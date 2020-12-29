//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Vivian Vea
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using VSharp.Binding.CFA;
using VSharp.Lowering;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using VSharp.Utils;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Binding
{
    internal sealed class Binder
    {
        private const string GeneratedMethodSeparator = "$$";
        public const string GeneratedMainMethodName = "<Main>";
        public const string MainMethodName = "Main";

        private readonly LabelGenerator _labelGenerator = new();
        private readonly Stack<MethodSymbol> _callStack = new();
        private readonly Stack<StatementSyntax> _statementStacktrace = new();
        private readonly Stack<NamespaceSymbol> _namespaceStack = new();
        private readonly HashSet<NamespaceSymbol> _includedNamespaces = new();
        private readonly Dictionary<MethodDeclarationSyntax, MethodSymbol> _methodSymbols = new();

        private BoundScope _scope;

        #region Constructors

        private Binder()
        {
            var rootScope = GetRootScope();
            _scope = new BoundScope(rootScope, capturesVariables: false);
        }

        public static BindingResult Bind(CompilationUnitSyntax syntax)
        {
            var binder = new Binder();

            var statements = binder.BindTopLevelStatements(syntax.Statements);
            var diagnostics = binder.Diagnostics.ToImmutableArray();

            return new BindingResult(statements, diagnostics);
        }

        private ImmutableArray<BoundMethodDeclaration> BindTopLevelStatements(IEnumerable<StatementSyntax> statements)
        {
            var boundStatements = BindStatements(statements);
            var loweredStatements = Lowerer.Lower(boundStatements, _labelGenerator);

            var methods = loweredStatements.OfType<BoundMethodDeclaration>();
            var topLevelStatements = loweredStatements.Except(methods).ToImmutableArray();

            if (topLevelStatements.Length > 0)
            {
                // Implicitly create main method.

                var mainMethodSymbol = new MethodSymbol(GeneratedMainMethodName, ImmutableArray<ParameterSymbol>.Empty, Types.Void);
                _scope.TryDeclareMethod(mainMethodSymbol);

                var mainMethod = new BoundMethodDeclaration(mainMethodSymbol, topLevelStatements);

                methods = methods.Prepend(mainMethod);
            }

            return methods.ToImmutableArray();
        }

        private ImmutableArray<BoundStatement> BindStatements(IEnumerable<StatementSyntax> statements)
        {
            DeclareMethods(statements);

            // Declare labels.
            foreach (var statement in statements.OfType<LabelStatementSyntax>())
            {
                DeclareLabel(statement);
            }

            // Bind remaining statements.
            var statementBuilder = ImmutableArray.CreateBuilder<BoundStatement>();
            foreach (var statement in statements)
            {
                var boundStatement = BindStatement(statement);
                statementBuilder.Add(boundStatement);
            }

            return statementBuilder.ToImmutable();
        }

        private void DeclareMethods(IEnumerable<StatementSyntax> statements)
        {
            if (EnableAnalysisMode)
                return;

            bool runDeclaredMethods;
            do
            {
                // Keep declaring methods in a loop until no new methods are delcared.
                runDeclaredMethods = false;

                foreach (var declaration in enumerateDeclarations(statements))
                {
                    var result = TryDeclareMethod(declaration);
                    if (result is not MethodDeclarationResult.CannotInferReturnType)
                    {
                        runDeclaredMethods = true;
                    }
                }
            } while (runDeclaredMethods);

            foreach (var declaration in enumerateDeclarations(statements))
            {
                TryDeclareMethod(declaration, declareImplicitReturnTypesAsError: true);
                Diagnostics.ReportCannotInferReturnType(declaration.Identifier.Span, declaration.Identifier.Text);
            }

            IEnumerable<MethodDeclarationSyntax> enumerateDeclarations(IEnumerable<StatementSyntax> statements)
            {
                foreach (var statement in statements)
                {
                    _statementStacktrace.Push(statement);
                    switch (statement)
                    {
                        case MethodDeclarationSyntax methodDeclaration:
                            if (!_methodSymbols.ContainsKey(methodDeclaration))
                                yield return methodDeclaration;

                            var localMethods = methodDeclaration.Declaration.Statements.OfType<MethodDeclarationSyntax>();
                            foreach (var declaration in enumerateDeclarations(localMethods))
                            {
                                yield return declaration;
                            }
                            break;

                        case NamespaceDeclarationSyntax namespaceDeclaration:
                            if (namespaceDeclaration is SimpleNamespaceDeclarationStatementSyntax && CurrentNamespace is not null)
                                Diagnostics.ReportIllegalSimpleNamespaceDeclaration(namespaceDeclaration.NamespaceToken.Span);

                            var namespaceSym = new NamespaceSymbol(CurrentNamespace, namespaceDeclaration.Names);
                            _namespaceStack.Push(namespaceSym);
                            try
                            {
                                foreach (var declaration in enumerateDeclarations(namespaceDeclaration.Statements))
                                {
                                    yield return declaration;
                                }
                            }
                            finally
                            {
                                _namespaceStack.Pop();
                            }
                            break;

                        case WithNamespaceStatementSyntax withNamespaceStatement:
                            namespaceSym = new NamespaceSymbol(withNamespaceStatement.Names);
                            bool wasIncluded = _includedNamespaces.Add(namespaceSym);
                            try
                            {
                                foreach (var declaration in enumerateDeclarations(withNamespaceStatement.Statements))
                                {
                                    yield return declaration;
                                }
                            }
                            finally
                            {
                                if (wasIncluded)
                                    _includedNamespaces.Remove(namespaceSym);
                            }
                            break;

                        default:
                            if (CurrentNamespace is not null && CurrentMethod is null)
                                Diagnostics.ReportIllegalStatementPlacement(statement.Span);
                            break;
                    }
                    _statementStacktrace.Pop();
                }
            }
        }

        #endregion Constructors

        #region Properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        /// <summary>
        /// The return value of the current method. Null if method is implicit.
        /// </summary>
        private MethodSymbol? CurrentMethod => _callStack.TryPeek(out var method) ? method : null;

        private NamespaceSymbol? CurrentNamespace => _namespaceStack.TryPeek(out var namespaceSym) ? namespaceSym : null;

        /// <summary>
        /// Used for binding statements for analysis, without commitment.
        /// </summary>
        private bool EnableAnalysisMode { get; set; }

        #endregion Properties

        #region BindStatement

        private BoundStatement BindStatement(StatementSyntax syntax)
        {
            _statementStacktrace.Push(syntax);
            try
            {
                return syntax.Kind switch
                {
                    SyntaxKind.MethodDeclaration => BindMethodDeclarationStatement((MethodDeclarationSyntax)syntax),
                    SyntaxKind.NamespaceDeclaration => BindNamespaceDeclarationStatement((NamespaceDeclarationSyntax)syntax),

                    SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax),
                    SyntaxKind.VariableDeclarationStatement => BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax),
                    SyntaxKind.IfStatement => BindIfStatement((IfStatementSyntax)syntax),
                    SyntaxKind.WhileStatement => BindLoopStatement((WhileStatementSyntax)syntax),
                    SyntaxKind.ForStatement => BindLoopStatement((ForStatementSyntax)syntax),
                    SyntaxKind.GoToStatement => BindGoToStatement((GoToStatementSyntax)syntax),
                    SyntaxKind.ReturnStatement => BindReturnStatement((ReturnStatementSyntax)syntax),
                    SyntaxKind.LabelStatement => BindLabelStatement((LabelStatementSyntax)syntax),
                    SyntaxKind.WithNamespaceStatement => BindWithNamespaceStatement((WithNamespaceStatementSyntax)syntax),
                    SyntaxKind.WithAliasStatement => BindWithAliasStatement((WithAliasStatementSyntax)syntax),
                    SyntaxKind.ContinueStatement => BindContinueStatement((ContinueStatementSyntax)syntax),
                    SyntaxKind.BreakStatement => BindBreakStatement((BreakStatementSyntax)syntax),
                    SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax),

                    _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
                };
            }
            finally
            {
                _statementStacktrace.Pop();
            }
        }

        private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            PushScope();
            try
            {
                var statements = BindStatements(syntax.Statements);
                return new BoundBlockStatement(statements);
            }
            finally
            {
                PopScope();
            }
        }

        private BoundStatement BindNamespaceDeclarationStatement(NamespaceDeclarationSyntax syntax)
        {
            if (!EnableAnalysisMode && CurrentMethod is not null)
                Diagnostics.ReportIllegalNamespaceDeclaration(syntax.NamespaceToken.Span);

            var namespaceSym = new NamespaceSymbol(CurrentNamespace, syntax.Names);
            _namespaceStack.Push(namespaceSym);
            try
            {
                var statements = BindStatements(syntax.Statements);
                return new BoundBlockStatement(statements);
            }
            finally
            {
                _namespaceStack.Pop();
            }
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;
            var isReadOnly = syntax.VarOrSetKeyword?.Kind is SyntaxKind.SetKeyword;

            var initializer = BindExpression(syntax.Initializer);

            // Only allow explicit type in combination with 'set'
            if (syntax.ExplicitType is not null && syntax.VarOrSetKeyword is not null and { Kind: not SyntaxKind.SetKeyword })
            {
                Diagnostics.ReportIllegalExplicitType(syntax.ExplicitType.Span);
            }

            // Don't allow void assignment
            if (initializer.Type == Types.Void && syntax.ExplicitType is null)
            {
                var span = TextSpan.FromBounds(syntax.Identifier.Span.Start, syntax.Initializer.Span.End);
                Diagnostics.ReportCannotAssignVoid(span);
            }

            TypeSymbol? type;
            if (syntax.ExplicitType is not null)
            {
                type = ResolveType(syntax.ExplicitType) ?? Types.Error;
                initializer = BindConvertion(syntax.Initializer.Span, initializer, type);
            }
            else
                type = initializer.Type;

            VariableSymbol variable;
            if (isReadOnly && ExpressionIsConstant(initializer, out var value) && TypeSymbolUtil.IsPrimitive(type))
            {
                variable = new ConstantSymbol(name, value);
            }
            else
            {
                variable = new VariableSymbol(name, isReadOnly, type);
            }

            if (declare && !_scope.TryDeclareVariable(variable))
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement BindMethodDeclarationStatement(MethodDeclarationSyntax syntax)
        {
            if (!_methodSymbols.TryGetValue(syntax, out var method))
                return BoundNoOpStatement.Instance;

            PushMethodScope(method);
            try
            {
                foreach (var parameter in method.Parameters)
                {
                    _scope.TryDeclareVariable(parameter);
                }

                var boundDeclaration = BindBody(syntax);
                if (method.ReturnType != Types.Void && !new ControlFlowGraph(boundDeclaration).AllPathsReturn())
                    Diagnostics.ReportMethodNotAllPathsReturnValue(syntax.Identifier.Span);

                return new BoundMethodDeclaration(method, boundDeclaration);
            }
            finally
            {
                PopMethodScope();
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition.Expression, Types.Boolean);
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindLoopStatement(LoopStatementSyntax syntax)
        {
            var continueLabel = _labelGenerator.GenerateLabel(LabelCategory.Continue);
            var breakLabel = _labelGenerator.GenerateLabel(LabelCategory.Break);

            PushScope(continueLabel, breakLabel);
            try
            {
                return syntax.Kind switch
                {
                    SyntaxKind.WhileStatement => BindWhileStatement((WhileStatementSyntax)syntax, continueLabel, breakLabel),
                    SyntaxKind.ForStatement => BindForStatement((ForStatementSyntax)syntax, continueLabel, breakLabel),

                    _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
                };
            }
            finally
            {
                PopScope();
            }
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax, LabelSymbol continueLabel, LabelSymbol breakLabel)
        {
            var condition = BindExpression(syntax.Condition.Expression, Types.Boolean);
            var body = BindStatement(syntax.Body);

            return new BoundWhileStatement(condition, body, continueLabel, breakLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax, LabelSymbol continueLabel, LabelSymbol breakLabel)
        {
            var initStatement = syntax.InitializationStatement is null ? null : BindStatement(syntax.InitializationStatement);
            var condition = syntax.Condition is null ? null : BindExpression(syntax.Condition, Types.Boolean);
            var updateStatement = syntax.UpdateStatement is null ? null : BindStatement(syntax.UpdateStatement);
            var body = BindStatement(syntax.Body);

            return new BoundForStatement(initStatement, condition, updateStatement, body, continueLabel, breakLabel);
        }

        private BoundStatement BindGoToStatement(GoToStatementSyntax syntax)
        {
            if (!TryFindLabel(syntax.Label, out var label))
                return BoundNoOpStatement.Instance;

            return new BoundGotoStatement(label);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            if (EnableAnalysisMode)
            {
                if (syntax.Expression is null)
                    return new BoundReturnStatement();

                var expression = BindExpression(syntax.Expression);
                return new BoundReturnStatement(expression);
            }

            if (CurrentMethod is null || CurrentMethod.ReturnType == Types.Void)
            {
                if (syntax.Expression is not null)
                    Diagnostics.ReportInvalidReturnInVoidMethod(syntax.Expression.Span);

                return new BoundReturnStatement();
            }
            else
            {
                if (syntax.Expression is null)
                {
                    Diagnostics.ReportMissingReturnExpression(syntax.Span);
                    return BoundNoOpStatement.Instance;
                }

                var expression = BindExpression(syntax.Expression, CurrentMethod.ReturnType);
                return new BoundReturnStatement(expression);
            }
        }

        private BoundStatement BindLabelStatement(LabelStatementSyntax syntax)
        {
            if (!_scope.TryLookupLabel(syntax.Identifier.Text, out var label))
                return BoundNoOpStatement.Instance;

            return new BoundLabelStatement(label);
        }

        private BoundStatement BindWithNamespaceStatement(WithNamespaceStatementSyntax syntax)
        {
            var namespaceSym = new NamespaceSymbol(syntax.Names);

            bool wasIncluded = _includedNamespaces.Add(namespaceSym);
            try
            {
                var statements = BindStatements(syntax.Statements);
                return new BoundBlockStatement(statements);
            }
            finally
            {
                if (wasIncluded)
                    _includedNamespaces.Remove(namespaceSym);
            }
        }

        private BoundStatement BindWithAliasStatement(WithAliasStatementSyntax syntax)
        {
            if (syntax is WithMethodAliasStatementSyntax methodAliasSyntax)
            {
                if (!TryFindMethod(methodAliasSyntax.NamespaceNames, methodAliasSyntax.Identifier, out var method))
                    return BoundNoOpStatement.Instance;

                PushScope();
                try
                {
                    _scope.TryDeclareMethodAlias(method, syntax.Alias.Text);

                    var statements = BindStatements(syntax.Statements);
                    return new BoundBlockStatement(statements);
                }
                finally
                {
                    PopScope();
                }
            }
            else throw new Exception($"Unsupported case '{syntax.GetType()}'.");
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (!_scope.TryGetContinueLabel(out var continueLabel))
            {
                Diagnostics.ReportUnexpectedBreakOrContinue(syntax.Span);
                return BoundNoOpStatement.Instance;
            }

            return new BoundGotoStatement(continueLabel);
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (!_scope.TryGetBreakLabel(out var breakLabel))
            {
                Diagnostics.ReportUnexpectedBreakOrContinue(syntax.Span);
                return BoundNoOpStatement.Instance;
            }

            return new BoundGotoStatement(breakLabel);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression);
            return new BoundExpressionStatement(expression);
        }

        #endregion BindStatement

        #region BindExpression

        private BoundExpression BindExpression(ExpressionSyntax syntax)
        {
            return syntax.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
                SyntaxKind.NameExpression => BindNameExpression((NameExpressionSyntax)syntax),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax),
                SyntaxKind.ParenthesizedExpression => BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax),
                SyntaxKind.TypeofExpression => BindTypeofExpression((TypeofExpressionSyntax)syntax),
                SyntaxKind.NameofExpression => BindNameofExpression((NameofExpressionSyntax)syntax),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)syntax),
                SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)syntax),
                SyntaxKind.ExplicitCastExpression => BindExplicitCastExpression((ExplicitCastExpressionSyntax)syntax),
                SyntaxKind.IndexerExpression => BindIndexerExpression((IndexerExpressionSyntax)syntax),
                SyntaxKind.ArrayInitializationExpression => BindArrayInitializationExpression((ArrayInitializationExpressionSyntax)syntax),

                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol? targetType)
        {
            var expression = BindExpression(syntax);
            if (targetType is null)
                return expression;

            if (!expression.Type.IsError() && !targetType.IsError())
                return BindConvertion(syntax.Span, expression, targetType);

            return expression;
        }

        private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable))
                return BoundErrorExpression.Instance;

            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);
            if (boundOperand.Type.IsError())
                return BoundErrorExpression.Instance;

            var boundOp = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, syntax.UnaryType, boundOperand.Type);
            if (boundOp is null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return BoundErrorExpression.Instance;
            }

            if (boundOp.Kind.IsIncrementOrDecrement())
            {
                if (boundOperand is not BoundVariableExpression variableExpression)
                {
                    Diagnostics.ReportIncrementOperandMustBeVariable(syntax.Span);
                    return BoundErrorExpression.Instance;
                }
                else if (variableExpression.Variable.IsReadOnly)
                {
                    Diagnostics.ReportCannotAssignReadOnly(syntax.Span, variableExpression.Variable.Name);
                    return BoundErrorExpression.Instance;
                }
            }

            return new BoundUnaryExpression(boundOp, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            if (syntax.OperatorToken.Kind == SyntaxKind.PipeGreaterToken)
            {
                // Piped argument.
                if (syntax.Right is not CallExpressionSyntax callExpression)
                {
                    Diagnostics.ReportCanOnlyPipeToMethods(syntax.OperatorToken.Span);
                    return BoundErrorExpression.Instance;
                }

                return BindCallExpression(callExpression, syntax.Left);
            }

            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type.IsError() || boundRight.Type.IsError())
                return BoundErrorExpression.Instance;

            var boundOp = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOp is null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return BoundErrorExpression.Instance;
            }

            return new BoundBinaryExpression(boundLeft, boundOp, boundRight);
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private static BoundExpression BindTypeofExpression(TypeofExpressionSyntax syntax)
        {
            var type = ResolveType(syntax.TypeExpression) ?? Types.Error;
            return new BoundLiteralExpression(type.Name);
        }

        private BoundExpression BindNameofExpression(NameofExpressionSyntax syntax)
        {
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable))
                return BoundErrorExpression.Instance;

            return new BoundLiteralExpression(variable.Name);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable) || variable.Type.IsError())
                return BoundErrorExpression.Instance;

            if (variable.IsReadOnly)
                Diagnostics.ReportCannotAssignReadOnly(syntax.EqualsToken.Span, variable.Name);

            var boundExpression = BindExpression(syntax.Expression);
            if (boundExpression.Type.IsError())
                return BoundErrorExpression.Instance;

            if (syntax.EqualsToken.Kind != SyntaxKind.EqualsToken)
            {
                var boundVariable = new BoundVariableExpression(variable);
                var operatorKind = syntax.EqualsToken.Kind switch
                {
                    SyntaxKind.PlusEqualsToken => BoundBinaryOperatorKind.Addition,
                    SyntaxKind.MinusEqualsToken => BoundBinaryOperatorKind.Subtraction,
                    SyntaxKind.StarEqualsToken => BoundBinaryOperatorKind.Multiplication,
                    SyntaxKind.SlashEqualsToken => BoundBinaryOperatorKind.Division,
                    SyntaxKind.AmpersandEqualsToken => BoundBinaryOperatorKind.BitwiseAnd,
                    SyntaxKind.PipeEqualsToken => BoundBinaryOperatorKind.BitwiseOr,

                    _ => throw new Exception($"Unexpected assignment kind {syntax.EqualsToken.Kind}"),
                };

                var boundOp = BoundBinaryOperator.Bind(operatorKind, variable.Type, boundExpression.Type);
                if (boundOp is null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.EqualsToken.Span, syntax.EqualsToken.Text, variable.Type, boundExpression.Type);
                    return BoundErrorExpression.Instance;
                }

                boundExpression = new BoundBinaryExpression(boundVariable, boundOp, boundExpression);
                if (boundExpression.Type.IsError())
                    return BoundErrorExpression.Instance;
            }

            boundExpression = BindConvertion(syntax.Expression.Span, boundExpression, variable.Type);
            return new BoundAssignmentExpression(variable, boundExpression);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, ExpressionSyntax? pipedParameter = null)
        {
            if (syntax.Identifier.Text is null)
                return BoundErrorExpression.Instance;

            // Locate method.
            if (!TryFindMethod(syntax.NamespaceNames, syntax.Identifier, out var method))
                return BoundErrorExpression.Instance;

            if (method.ReturnType.IsError())
                return BoundErrorExpression.Instance;

            var parameterCount = syntax.Arguments.Count + (pipedParameter is null ? 0 : 1);

            // Bind arguments.
            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();
            foreach (var argument in syntax.Arguments)
            {
                if (argument is ExpressionSyntax expressionArg)
                {
                    var boundArgument = BindExpression(expressionArg);
                    boundArguments.Add(boundArgument);
                }
                else if (pipedParameter != null)
                {
                    var boundArgument = BindExpression(pipedParameter);
                    boundArguments.Add(boundArgument);
                    pipedParameter = null;
                }
                else
                {
                    Diagnostics.ReportUnexpectedPipedArgument(argument.Span);
                }
            }

            // If piped argument was not consumed, append to arguemnts.
            if (pipedParameter is not null)
            {
                var boundArgument = BindExpression(pipedParameter);
                boundArguments.Add(boundArgument);
                pipedParameter = null;
            }

            // Validate argument count.
            if (boundArguments.Count != method.Parameters.Length)
            {
                Diagnostics.ReportWrongArgumentCount(syntax.Span, method.Name, method.Parameters.Length, parameterCount);
                return BoundErrorExpression.Instance;
            }

            // Validate argument types.
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var argument = boundArguments[i];
                var parameter = method.Parameters[i];

                if (argument.Type.IsError())
                    continue;

                boundArguments[i] = BindConvertion(syntax.Span, argument, parameter.Type);

                if (boundArguments[i] is BoundErrorExpression)
                {
                    Diagnostics.ReportWrongArgumentType(syntax.Span, method.Name, parameter.Name, parameter.Type, argument.Type);
                    return BoundErrorExpression.Instance;
                }
            }

            return new BoundCallExpression(method, boundArguments.ToImmutableArray());
        }

        private BoundExpression BindExplicitCastExpression(ExplicitCastExpressionSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression);
            if (expression.Type == Types.Error)
                return BoundErrorExpression.Instance;

            var type = ResolveType(syntax.TypeExpression) ?? Types.Error;
            if (type == Types.Error)
                return BoundErrorExpression.Instance;

            var conversion = Conversion.Classify(expression.Type, type);
            if (!conversion.Exists)
            {
                Diagnostics.ReportNoExplicitConversion(syntax.Span, expression.Type, type);
                return BoundErrorExpression.Instance;
            }

            return new BoundExplicitCastExpression(type, expression);
        }

        private BoundExpression BindIndexerExpression(IndexerExpressionSyntax syntax)
        {
            var operand = BindExpression(syntax.Operand);

            var indexer = operand.Type.FindGetIndexers().FirstOrDefault();
            if (indexer is null)
            {
                Diagnostics.ReportTypeDoesNotHaveIndexer(syntax.Span, operand.Type.Name);
                return BoundErrorExpression.Instance;
            }

            if (syntax.Parameters.Count != indexer.Parameters.Length)
            {
                Diagnostics.ReportWrongArgumentCount(syntax.Span, operand.Type + "[]", indexer.Parameters.Length, syntax.Parameters.Count);
                return BoundErrorExpression.Instance;
            }

            var boundParameters = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Parameters.Count);

            for (int i = 0; i < syntax.Parameters.Count; i++)
            {
                var parameter = syntax.Parameters[i];
                var expectedType = indexer.Parameters[i].Type;

                var boundParameter = BindExpression(parameter, expectedType);
                boundParameters.Add(boundParameter);
            }

            return new BoundCallExpression(operand, indexer, boundParameters.MoveToImmutable());
        }

        private BoundExpression BindArrayInitializationExpression(ArrayInitializationExpressionSyntax syntax)
        {
            TypeSymbol? itemType = null;
            if (syntax.TypeToken is not null)
                itemType = ResolveType(syntax.TypeToken) ?? Types.Error;

            var boundInitializer = ImmutableArray.CreateBuilder<BoundExpression>();
            var sizeExpression = syntax.SizeExpression is not null ? BindExpression(syntax.SizeExpression, Types.Int) : null;

            if (syntax is LambdaArrayInitializerExpressionSyntax lambdaInitialization &&
                lambdaInitialization.Initializer.Count == 1)
            {
                boundInitializer.Add(BindExpression(lambdaInitialization.Initializer[0], itemType));
                itemType ??= boundInitializer[0].Type;
            }
            else
            {
                BindArrayInitializer(syntax.Initializer);
            }

            var arrayType = Types.Array.MakeConcreteType(itemType ?? Types.Object);
            sizeExpression ??= new BoundLiteralExpression(boundInitializer.Count);
            return new BoundArrayInitializationExpression(arrayType, sizeExpression, boundInitializer.ToImmutable());

            void BindArrayInitializer(SeparatedSyntaxList<ExpressionSyntax> initializer)
            {
                if (sizeExpression is null)
                {
                    BindArrayInitializer(initializer.Count);
                }
                else if (ExpressionIsConstant(sizeExpression, out var value))
                {
                    BindArrayInitializer(value);
                }
                else
                {
                    TextSpan span;
                    if (syntax.SizeExpression is not null)
                        span = syntax.SizeExpression.Span;
                    else
                    {
                        SyntaxNode startNode = (SyntaxNode?)syntax.TypeToken ?? syntax.OpenBracketToken;
                        span = TextSpan.FromBounds(startNode.Span.Start, syntax.CloseBracketToken.Span.End);
                    }

                    Diagnostics.ReportSizeMustBeConstantWithInitializer(span);
                    itemType ??= Types.Error;
                }

                void BindArrayInitializer(object size)
                {
                    if (!size.Equals(initializer.Count))
                    {
                        Diagnostics.ReportArraySizeMismatch(initializer.Span);
                    }

                    foreach (var expression in initializer)
                    {
                        boundInitializer.Add(BindExpression(expression, itemType));
                    }

                    foreach (var expression in boundInitializer)
                    {
                        if (itemType is null)
                            itemType = expression.Type;
                        else if (itemType != Types.Object)
                            itemType = itemType.FindCommonAncestor(expression.Type);
                    }
                }
            }
        }

        #endregion BindExpression

        #region Helpers

        /// <summary>
        /// Moves into a new scope.
        /// </summary>
        private void PushScope()
        {
            _scope = new BoundScope(_scope, capturesVariables: true);
        }

        /// <summary>
        /// Moves into a new scope.
        /// </summary>
        private void PushMethodScope(MethodSymbol method)
        {
            _scope = new BoundScope(_scope, capturesVariables: false);
            _callStack.Push(method);
        }

        /// <summary>
        /// Moves into a new scope.
        /// </summary>
        private void PushScope(LabelSymbol continueLabel, LabelSymbol breakLabel)
        {
            _scope = new BoundScope(_scope, continueLabel, breakLabel);

            _scope.TryDeclareLabel(continueLabel);
            _scope.TryDeclareLabel(breakLabel);
        }

        /// <summary>
        /// Drops the current scope, and moves up to its parent.
        /// </summary>
        private void PopScope()
        {
            _scope = _scope.Parent ?? throw new InvalidOperationException("Scope's parent was null");
        }

        private void PopMethodScope()
        {
            PopScope();
            _callStack.Pop();
        }

        #region DeclareMethod

        private enum MethodDeclarationResult { Success, CannotInferReturnType, Error }

        private MethodDeclarationResult TryDeclareMethod(MethodDeclarationSyntax syntax, bool declareImplicitReturnTypesAsError = false)
        {
            if (_methodSymbols.ContainsKey(syntax))
                return MethodDeclarationResult.Success;

            if (syntax.Identifier.Text is null)
                return MethodDeclarationResult.Error;

            string name = getOrGenerateMethodName(syntax.Identifier.Text);
            var parameters = BindParameters(syntax.Parameters);

            var hasImplicitReturnType = syntax.TypeOrDefKeyword.Kind == SyntaxKind.DefKeyword;

            MethodSymbol? method;
            if (!hasImplicitReturnType)
            {
                var returnType = ResolveType(syntax.TypeOrDefKeyword.Kind) ?? Types.Error;
                method = new MethodSymbol(CurrentNamespace, name, parameters, returnType);
            }
            else if (declareImplicitReturnTypesAsError)
            {
                method = new MethodSymbol(CurrentNamespace, name, parameters, Types.Error);
            }
            else
            {
                PushScope();
                EnableAnalysisMode = true;
                try
                {
                    // Resolve implicit return type
                    foreach (var parameter in parameters)
                    {
                        _scope.TryDeclareVariable(parameter);
                    }

                    // Bind a primitive body for analyzis to infer the return type.
                    var boundDeclaration = BindBody(syntax);

                    var returnStatements = boundDeclaration.OfType<BoundReturnStatement>();
                    var returnType = ResolveImplicitType(this, returnStatements);

                    if (returnType is null)
                        return MethodDeclarationResult.CannotInferReturnType;

                    method = new MethodSymbol(CurrentNamespace, name, parameters, returnType);
                }
                finally
                {
                    EnableAnalysisMode = false;
                    PopScope();
                }
            }

            if (!_scope.TryDeclareMethod(method))
            {
                Diagnostics.ReportMethodAlreadyDeclared(syntax.Identifier.Span, name);
                return MethodDeclarationResult.Error;
            }

            _methodSymbols.Add(syntax, method);
            return MethodDeclarationResult.Success;

            string getOrGenerateMethodName(string methodName)
            {
                return string.Join(GeneratedMethodSeparator,
                    _statementStacktrace
                        .OfType<MethodDeclarationSyntax>()
                        .Select(dec => dec.Identifier.Text)
                        .Reverse());
            }

            static TypeSymbol? ResolveImplicitType(Binder @this, IEnumerable<BoundReturnStatement> returnStatements)
            {
                if (returnStatements.All(r => r.Expression is null))
                    return Types.Void;

                if (returnStatements.All(r => r.Expression is not null && r.Expression.Type == Types.Error))
                    return null;

                return returnStatements.FirstOrDefault(r => r.Expression is not null)?.Expression!.Type;
            }
        }

        private ImmutableArray<BoundStatement> BindBody(MethodDeclarationSyntax syntax)
        {
            // Include namespace of method and all parent namespaces.
            Span<bool> includedNamespaces = CurrentNamespace is not null ? stackalloc bool[CurrentNamespace.Names.Length] : default;
            for (int i = 0; i < includedNamespaces.Length; i++)
            {
                Debug.Assert(CurrentNamespace is not null);
                includedNamespaces[i] = _includedNamespaces.Add(new NamespaceSymbol(CurrentNamespace.Names.Take(i + 1)));
            }

            try
            {
                if (syntax.Declaration is ExpressionBodyStatementSyntax expressionBody)
                {
                    var expression = BindExpression(expressionBody.Statement.Expression);
                    if (expression.Type == Types.Void)
                    {
                        var expressionStatement = new BoundExpressionStatement(expression);
                        return Lowerer.Lower(expressionStatement, _labelGenerator);
                    }
                    else
                    {
                        var returnStatement = new BoundReturnStatement(expression);
                        return Lowerer.Lower(returnStatement, _labelGenerator);
                    }
                }
                else
                {
                    var statements = BindStatements(syntax.Declaration.Statements);

                    return Lowerer.Lower(statements, _labelGenerator);
                }
            }
            finally
            {
                for (int i = 0; i < includedNamespaces.Length; i++)
                {
                    Debug.Assert(CurrentNamespace is not null);
                    _includedNamespaces.Remove(new NamespaceSymbol(CurrentNamespace.Names.Take(i + 1)));
                }
            }
        }

        #endregion DeclareMethod

        private void DeclareLabel(LabelStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;

            if (!declare)
                return;

            var label = new LabelSymbol(name);
            if (!_scope.TryDeclareLabel(label))
                Diagnostics.ReportLabelAlreadyDeclared(syntax.Identifier.Span, name);
        }

        private ImmutableArray<ParameterSymbol> BindParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(parameters.Count);
            var parameterNames = new HashSet<string>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = BindParameter(parameters[i]);
                if (!parameterNames.Add(parameter.Name))
                    Diagnostics.ReportDuplicateParameterName(parameters[i].Identifier.Span, parameter.Name);

                builder.Add(parameter);
            }

            return builder.MoveToImmutable();
        }

        private static ParameterSymbol BindParameter(ParameterSyntax parameter)
        {
            var name = parameter.Identifier.Text ?? "?";
            var type = ResolveType(parameter.TypeToken) ?? Types.Error;

            return new ParameterSymbol(name, type);
        }

        /// <summary>
        /// Reports a diagnostic if there's an invalid assignment.
        /// </summary>
        private BoundExpression BindConvertion(TextSpan span, BoundExpression expression, TypeSymbol resultType)
        {
            var conversion = Conversion.Classify(expression.Type, resultType);
            if (conversion.IsIdentity || conversion.IsImplicit)
            {
                return expression;
            }
            else if (conversion == Conversion.Explicit)
            {
                Diagnostics.ReportCannotImplicitlyConvert(span, expression.Type, resultType);
                return BoundErrorExpression.Instance;
            }
            else
            {
                Diagnostics.ReportCannotConvert(span, expression.Type, resultType);
                return BoundErrorExpression.Instance;
            }
        }

        #region TryFind

        private bool TryFindVariable(SyntaxToken identifierToken, [NotNullWhen(true)] out VariableSymbol? variable)
        {
            string? name = identifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                variable = null;
                return false;
            }

            if (!_scope.TryLookupVariable(name, out variable))
            {
                Diagnostics.ReportUndefinedSymbol(identifierToken.Span, name);

                _scope.TryDeclareVariable(new VariableSymbol(name, isReadOnly: false, Types.Error));
                return false;
            }

            return true;
        }

        private bool TryFindLabel(SyntaxToken identifierToken, [NotNullWhen(true)] out LabelSymbol? label)
        {
            string? name = identifierToken.Text;
            if (string.IsNullOrEmpty(name))
            {
                label = null;
                return false;
            }

            if (!_scope.TryLookupLabel(name, out label))
            {
                Diagnostics.ReportUndefinedSymbol(identifierToken.Span, name);

                _scope.TryDeclareLabel(new LabelSymbol(name));
                return false;
            }

            return true;
        }

        private bool TryFindMethod(NamespaceSymbol? namespaceSym, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            var methodName = identifierToken.Text;
            if (methodName is null)
            {
                method = null;
                return false;
            }

            // Try resolving local method first.
            if (namespaceSym is null)
            {
                foreach (var parentStatement in _statementStacktrace)
                {
                    if (!TryGetChildren<MethodDeclarationSyntax>(parentStatement, out var localDeclarations))
                        continue;

                    var localDeclaration = localDeclarations.FirstOrDefault(dec => dec.Identifier.Text == methodName);
                    if (localDeclaration is not null && _methodSymbols.TryGetValue(localDeclaration, out method))
                    {
                        return true;
                    }
                }
            }

            // Method must be global.
            var fullMethodName = namespaceSym is null ? methodName : namespaceSym.Name + "." + methodName;
            var resolvedMethods = new List<MethodSymbol>();

            if (_scope.TryLookupMethod(fullMethodName, out var resolvedMethod))
                resolvedMethods.Add(resolvedMethod);

            foreach (var includedNamespace in _includedNamespaces)
            {
                var tmpName = includedNamespace.Name + "." + fullMethodName;
                if (_scope.TryLookupMethod(tmpName, out resolvedMethod))
                    resolvedMethods.Add(resolvedMethod);
            }

            if (resolvedMethods.Count > 1)
            {
                if (!EnableAnalysisMode)
                    Diagnostics.ReportAmbigousSymbolReference(identifierToken.Span, resolvedMethods);

                method = null;
                return false;
            }

            if (resolvedMethods.Count == 1)
            {
                method = resolvedMethods[0];
                return true;
            }
            else
            {
                method = null;
            }

            if (EnableAnalysisMode)
                return false;

            Diagnostics.ReportUndefinedSymbol(identifierToken.Span, methodName);
            _scope.TryDeclareMethod(new MethodSymbol(namespaceSym, methodName, ImmutableArray<ParameterSymbol>.Empty, Types.Error));

            return false;
        }

        private bool TryFindMethod(SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            var namespaceSym = namespaceNames is not null ? new NamespaceSymbol(namespaceNames) : null;
            return TryFindMethod(namespaceSym, identifierToken, out method);
        }

        #endregion TryFind

        private static bool ExpressionIsConstant(BoundExpression expression, [NotNullWhen(true)] out object? value)
        {
            switch (expression)
            {
                case BoundLiteralExpression literalExpression:
                    value = literalExpression.Value;
                    return true;

                case BoundVariableExpression variableExpression when variableExpression.Variable is ConstantSymbol constant:
                    value = constant.Value;
                    return true;

                default:
                    value = null;
                    return false;
            }
        }

        #endregion Helpers

        #region Utilities

        private static TypeSymbol? ResolveType(TypeExpressionSyntax typeExpression)
        {
            var type = ResolveType(typeExpression.TypeIdentifiers[0].Kind) ?? Types.Error;
            if (typeExpression.IsArray)
                return Types.Array.MakeConcreteType(type);
            else
                return type;
        }

        private static TypeSymbol? ResolveType(SyntaxKind typeKeyword)
        {
            return typeKeyword switch
            {
                SyntaxKind.ObjectKeyword => Types.Object,
                SyntaxKind.BoolKeyword => Types.Boolean,
                SyntaxKind.IntKeyword => Types.Int,
                SyntaxKind.FloatKeyword => Types.Float,
                SyntaxKind.StringKeyword => Types.String,
                SyntaxKind.CharKeyword => Types.Char,

                _ => null,
            };
        }

        private static BoundScope? _rootScope;

        private static BoundScope GetRootScope()
        {
            if (_rootScope is null)
            {
                var rootScope = new BoundScope();
                foreach (var method in SystemSymbols.Methods.GetAll())
                    rootScope.TryDeclareMethod(method);

                Interlocked.CompareExchange(ref _rootScope, rootScope, null);
            }

            return _rootScope;
        }

        /// <summary>
        /// Returns all the children of the given statement of the specified type.
        /// </summary>
        private static bool TryGetChildren<T>(StatementSyntax statement, [NotNullWhen(true)] out IEnumerable<T>? result)
            where T : StatementSyntax
        {
            IEnumerable<StatementSyntax>? r = statement switch
            {
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.Declaration.Statements,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Statements,
                WithNamespaceStatementSyntax withNamespaceStatement => withNamespaceStatement.Statements,
                BlockStatementSyntax blockStatement => blockStatement.Statements,
                _ => null,
            };

            result = r?.OfType<T>();
            return result is not null;
        }

        #endregion Utilities
    }
}