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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using VSharp.Binding.CFA;
using VSharp.Lowering;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Binding
{
    internal sealed class Binder
    {
        public const string GeneratedMainMethodName = "<Main>";
        public const string MainMethodName = "Main";

        private readonly LabelGenerator _labelGenerator;
        private readonly Stack<MethodSymbol> _callStack = new();
        private readonly Dictionary<MethodDeclarationSyntax, MethodSymbol> _methodSymbols = new();

        private BoundScope _scope;

        #region Constructors

        private Binder(LabelGenerator labelGenerator)
        {
            _labelGenerator = labelGenerator;

            var rootScope = GetRootScope();
            _scope = new BoundScope(rootScope, capturesVariables: false);
        }

        public static BindingResult Bind(CompilationUnitSyntax syntax, LabelGenerator labelGenerator)
        {
            var binder = new Binder(labelGenerator);

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

            var methodDeclarationsInfo = getMethodDeclarations(this, statements).ToHashSet();
            var declaredMethods = new HashSet<(MethodDeclarationSyntax methodDeclaration, NamespaceSymbol? namespaceSym)>();

            // Keep declaring methods in a loop until no new methods are delcared.
            while (methodDeclarationsInfo.Count > 0)
            {
                foreach (var info in methodDeclarationsInfo)
                {
                    var result = TryDeclareMethod(info.methodDeclaration, info.namespaceSym);
                    if (result is not MethodDeclarationResult.CannotInferReturnType)
                    {
                        declaredMethods.Add(info);
                    }
                }

                // No methods were declared in this loop. Escape.
                if (declaredMethods.Count == 0)
                    break;

                methodDeclarationsInfo.ExceptWith(declaredMethods);
                declaredMethods.Clear();
            }

            foreach (var (methodDeclaration, namespaceSym) in methodDeclarationsInfo)
            {
                TryDeclareMethod(methodDeclaration, namespaceSym, declareImplicitReturnTypesAsError: true);
                Diagnostics.ReportCannotInferReturnType(methodDeclaration.Identifier.Span, methodDeclaration.Identifier.Text);
            }

            static HashSet<(MethodDeclarationSyntax methodDeclaration, NamespaceSymbol? namespaceSym)> getMethodDeclarations(Binder @this, IEnumerable<StatementSyntax> statements)
            {
                var stack = new Stack<(StatementSyntax statement, NamespaceSymbol? namespaceSym)>(statements.Select(s => (s, default(NamespaceSymbol))).Reverse());

                var result = new HashSet<(MethodDeclarationSyntax, NamespaceSymbol?)>();
                while (stack.Count > 0)
                {
                    var (current, currentNamespace) = stack.Pop();
                    if (current is MethodDeclarationSyntax methodDeclaration)
                    {
                        result.Add((methodDeclaration, currentNamespace));
                    }
                    else if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                    {
                        if (namespaceDeclaration is SimpleNamespaceDeclarationStatementSyntax && currentNamespace is not null)
                            @this.Diagnostics.ReportIllegalSimpleNamespaceDeclaration(namespaceDeclaration.NamespaceToken.Span);

                        var names = namespaceDeclaration.Names.Select(name => name.Text ?? "");
                        var newNamespace = currentNamespace is null ? new NamespaceSymbol(names.ToImmutableArray()) : new NamespaceSymbol(currentNamespace, names);
                        foreach (var statement in namespaceDeclaration.Statements)
                        {
                            stack.Push((statement, newNamespace));
                        }
                    }
                    else
                    {
                        if (currentNamespace is not null)
                            @this.Diagnostics.ReportIllegalStatementPlacement(current.Span);
                    }
                }

                return result;
            }
        }

        #endregion Constructors

        #region Properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        /// <summary>
        /// The return value of the current method. Null if method is implicit.
        /// </summary>
        private MethodSymbol? CurrentMethod => _callStack.TryPeek(out var method) ? method : null;

        /// <summary>
        /// Used for binding statements for analysis, without commitment.
        /// </summary>
        private bool EnableAnalysisMode { get; set; }

        #endregion Properties

        #region BindStatement

        private BoundStatement BindStatement(StatementSyntax syntax)
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
                SyntaxKind.ContinueStatement => BindContinueStatement((ContinueStatementSyntax)syntax),
                SyntaxKind.BreakStatement => BindBreakStatement((BreakStatementSyntax)syntax),
                SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax),

                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
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
            if (CurrentMethod is not null)
                Diagnostics.ReportIllegalNamespaceDeclaration(syntax.NamespaceToken.Span);

            var statements = BindStatements(syntax.Statements);
            return new BoundBlockStatement(statements);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;
            var isReadOnly = syntax.Keyword.Kind is SyntaxKind.SetKeyword;

            var initializer = BindExpression(syntax.Initializer);

            // Only allow explicit type in combination with 'set'
            if (syntax.ExplicitType is not null && syntax.Keyword.Kind is not SyntaxKind.SetKeyword)
            {
                Diagnostics.ReportIllegalExplicitType(syntax.ExplicitType.Span);
            }

            // Don't allow void assignment
            if (initializer.Type == Types.Void && syntax.Keyword.Kind.IsImplicitTypeKeyword())
            {
                var span = TextSpan.FromBounds(syntax.Identifier.Span.Start, syntax.Initializer.Span.End);
                Diagnostics.ReportCannotAssignVoid(span);
            }

            var typeToken = syntax.ExplicitType ?? syntax.Keyword;
            var type = ResolveType(typeToken.Kind);
            if (type is null)
                type = initializer.Type;
            else
                initializer = BindConvertion(syntax.Initializer.Span, initializer, type);

            var variable = new VariableSymbol(name, isReadOnly, type);
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

            if (CurrentMethod is null)
                throw new Exception($"Unexpected return statement outside of method body.");

            if (CurrentMethod.ReturnType == Types.Void)
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

                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol targetType)
        {
            var expression = BindExpression(syntax);
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
            var type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? Types.Error;
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
            if (EnableAnalysisMode && !TryFindMethod(syntax.NamespaceNames, syntax.Identifier, out var method))
                return BoundErrorExpression.Instance;
            else if (!FindMethodOrReport(syntax.NamespaceNames, syntax.Identifier, out method))
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
            if (pipedParameter != null)
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

                if (argument.Type != parameter.Type && !argument.Type.IsError() && !parameter.Type.IsError())
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

            var type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? Types.Error;
            if (type == Types.Error)
                return BoundErrorExpression.Instance;

            var conversion = Conversion.Classify(expression.Type, type);
            if (conversion == Conversion.None)
            {
                Diagnostics.ReportNoExplicitConversion(syntax.Span, expression.Type, type);
                return BoundErrorExpression.Instance;
            }

            return new BoundExplicitCastExpression(type, expression);
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

        private MethodDeclarationResult TryDeclareMethod(MethodDeclarationSyntax syntax, NamespaceSymbol? namespaceSym, bool declareImplicitReturnTypesAsError = false)
        {
            if (_methodSymbols.ContainsKey(syntax))
                return MethodDeclarationResult.Success;

            if (syntax.Identifier.Text is null)
                return MethodDeclarationResult.Error;

            string name = syntax.Identifier.Text;
            var parameters = BindParameters(syntax.Parameters);

            var hasImplicitReturnType = syntax.TypeOrDefKeyword.Kind == SyntaxKind.DefKeyword;

            MethodSymbol? method;
            if (!hasImplicitReturnType)
            {
                var returnType = ResolveType(syntax.TypeOrDefKeyword.Kind) ?? Types.Error;
                method = new MethodSymbol(namespaceSym, name, parameters, returnType);
            }
            else if (declareImplicitReturnTypesAsError)
            {
                method = new MethodSymbol(namespaceSym, name, parameters, Types.Error);
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

                    method = new MethodSymbol(namespaceSym, name, parameters, returnType);
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

            static TypeSymbol? ResolveImplicitType(Binder @this, IEnumerable<BoundReturnStatement> returnStatements)
            {
                if (!returnStatements.Any())
                    return Types.Void;

                return returnStatements.FirstOrDefault(r => r.Expression is not null && r.Expression.Type != Types.Error)?.Expression?.Type;
            }
        }

        private ImmutableArray<BoundStatement> BindBody(MethodDeclarationSyntax syntax)
        {
            if (syntax.Declaration is ExpressionBodyStatementSyntax expressionBody)
            {
                var expression = BindExpression(syntax, expressionBody);
                var returnStatement = new BoundReturnStatement(expression);
                var body = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(returnStatement));

                return Lowerer.Lower(body, _labelGenerator);
            }
            else
            {
                var statements = BindStatements(syntax.Declaration.Statements);
                var body = new BoundBlockStatement(statements);

                return Lowerer.Lower(body, _labelGenerator);
            }
        }

        private BoundExpression BindExpression(MethodDeclarationSyntax syntax, ExpressionBodyStatementSyntax expressionBody)
        {
            if (expressionBody.Statement is ExpressionStatementSyntax expressionStatement)
            {
                return BindExpression(expressionStatement.Expression);
            }

            if (!EnableAnalysisMode)
            {
                Diagnostics.ReportMethodNotAllPathsReturnValue(syntax.Identifier.Span);
            }
            return BoundErrorExpression.Instance;
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
            var type = ResolveType(parameter.TypeToken.TypeIdentifier.Kind) ?? Types.Error;

            return new ParameterSymbol(name, type);
        }

        /// <summary>
        /// Reports a diagnostic if there's an invalid assignment.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="expression"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        private BoundExpression BindConvertion(TextSpan span, BoundExpression expression, TypeSymbol resultType)
        {
            var conversion = Conversion.Classify(expression.Type, resultType);
            if (conversion == Conversion.Identity || conversion == Conversion.Implicit)
            {
            }
            else if (conversion == Conversion.Explicit)
            {
                Diagnostics.ReportCannotImplicitlyConvert(span, expression.Type, resultType);
            }
            else
            {
                Diagnostics.ReportCannotConvert(span, expression.Type, resultType);
            }

            return expression;
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

        private bool TryFindMethod(SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            if (namespaceNames is null)
                return TryFindMethod((NamespaceSymbol?)null, identifierToken, out method);

            var @namespace = new NamespaceSymbol(namespaceNames.Select(name => name.Text ?? "").ToImmutableArray());
            return TryFindMethod(@namespace, identifierToken, out method);
        }

        private bool TryFindMethod(NamespaceSymbol? @namespace, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            string? name = @namespace is null ? identifierToken.Text : @namespace.Name + "." + identifierToken.Text;
            if (name is null)
            {
                method = null;
                return false;
            }

            return _scope.TryLookupMethod(name, out method);
        }

        private bool FindMethodOrReport(SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            if (TryFindMethod(namespaceNames, identifierToken, out method))
                return true;

            var methodName = identifierToken.Text;
            if (methodName is not null)
            {
                Diagnostics.ReportUndefinedSymbol(identifierToken.Span, methodName);

                if (namespaceNames is null)
                    _scope.TryDeclareMethod(new MethodSymbol(methodName, ImmutableArray<ParameterSymbol>.Empty, Types.Error));
                else
                {
                    var @namespace = new NamespaceSymbol(namespaceNames.Select(name => name.Text ?? "").ToImmutableArray());
                    _scope.TryDeclareMethod(new MethodSymbol(@namespace, methodName, ImmutableArray<ParameterSymbol>.Empty, Types.Error));
                }
            }

            return false;
        }

        #endregion TryFind

        #endregion Helpers

        #region Utilities

        private static TypeSymbol? ResolveType(SyntaxKind typeKeyword) => typeKeyword switch
        {
            SyntaxKind.BoolKeyword => Types.Boolean,
            SyntaxKind.IntKeyword => Types.Int,
            SyntaxKind.StringKeyword => Types.String,
            SyntaxKind.FloatKeyword => Types.Float,

            _ => null,
        };

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

        #endregion Utilities
    }
}