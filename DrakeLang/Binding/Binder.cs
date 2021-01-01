//------------------------------------------------------------------------------
// DrakeLang - Viv's C#-esque sandbox.
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

using DrakeLang.Binding.CFA;
using DrakeLang.Lowering;
using DrakeLang.Symbols;
using DrakeLang.Syntax;
using DrakeLang.Text;
using DrakeLang.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using static DrakeLang.Symbols.SystemSymbols;

namespace DrakeLang.Binding
{
    internal sealed class Binder
    {
        private const string GeneratedMethodSeparator = "$$";
        public const string MainMethodName = "Main";

        private readonly LabelGenerator _labelGenerator = new();
        private readonly Stack<MethodSymbol> _callStack = new();
        private readonly Stack<StatementSyntax> _statementStacktrace = new();
        private readonly Stack<NamespaceSymbol> _namespaceStack = new();
        private readonly HashSet<NamespaceSymbol> _includedNamespaces = new();
        private readonly Dictionary<MethodDeclarationSyntax, MethodSymbol> _methodSymbols = new();
        private readonly Dictionary<MethodSymbol, BoundMethodDeclaration> _methodDeclarations = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<BoundNode, SyntaxNode> _boundNodeToSyntaxMap = new();
        private readonly List<BoundMethodDeclaration> _generatedMainMethods = new();

        private readonly CompilationOptions _options;
        private BoundScope _scope;

        #region Constructors

        private Binder(CompilationOptions options)
        {
            _options = options;

            var rootScope = GetRootScope();
            _scope = new BoundScope(rootScope, capturesVariables: false);
        }

        public static BindingResult Bind(ImmutableArray<CompilationUnitSyntax> units, CompilationOptions options)
        {
            var statements = units.SelectMany(u => u.Statements);

            var binder = new Binder(options);

            binder.DeclareMethods(statements);

            foreach (var unit in units)
            {
                binder.PushScope();
                try
                {
                    binder._currentText = unit.Text;
                    binder.BindTopLevelStatements(unit.Statements);
                }
                finally
                {
                    binder.PopScope();
                }
            }

            binder.ValidateMainMethod();

            var methods = binder._methodDeclarations.Values.ToImmutableArray();
            methods = Lowerer.Lower(methods, binder._labelGenerator, new() { Optimize = options.Optimize });

            var diagnostics = binder.Diagnostics.ToImmutableArray();

            return new BindingResult(methods, diagnostics);
        }

        private void BindTopLevelStatements(IEnumerable<StatementSyntax> statements)
        {
            var topLevelStatements = BindStatements(statements);

            if (topLevelStatements.Any())
            {
                // Implicitly create main method.
                var generatedMainMethodSymbol = new MethodSymbol(MainMethodName, ImmutableArray<ParameterSymbol>.Empty, Types.Void);
                var generatedMainMethod = new BoundMethodDeclaration(generatedMainMethodSymbol, topLevelStatements);

                _scope.TryDeclareMethod(generatedMainMethodSymbol);
                _generatedMainMethods.Add(generatedMainMethod);
                _methodDeclarations.Add(generatedMainMethodSymbol, generatedMainMethod);
            }
        }

        private ImmutableArray<BoundStatement> BindStatements(IEnumerable<StatementSyntax> statements)
        {
            // Declare labels.
            foreach (var statement in statements.OfType<LabelStatementSyntax>())
            {
                DeclareLabel(statement);
            }

            // Bind statements.
            return statements.SelectMany(s => BindStatement(s))
                             .ToImmutableArray();
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
                    _currentText = declaration.Text;
                    var result = TryDeclareMethod(declaration);
                    if (result is MethodDeclarationResult.Success)
                    {
                        runDeclaredMethods = true;
                    }
                }
            } while (runDeclaredMethods);

            foreach (var declaration in enumerateDeclarations(statements))
            {
                _currentText = declaration.Text;
                TryDeclareMethod(declaration, declareImplicitReturnTypesAsError: true);

                DiagnosticsBuilder.ReportCannotInferReturnType(declaration.Identifier.Span, declaration.Identifier.TokenText);
            }

            IEnumerable<MethodDeclarationSyntax> enumerateDeclarations(IEnumerable<StatementSyntax> statements, bool isMethodBody = false)
            {
                foreach (var statement in statements)
                {
                    _currentText = statement.Text;
                    _statementStacktrace.Push(statement);
                    switch (statement)
                    {
                        case MethodDeclarationSyntax methodDeclaration:
                            if (!_methodSymbols.ContainsKey(methodDeclaration))
                                yield return methodDeclaration;

                            foreach (var declaration in enumerateDeclarations(methodDeclaration.Declaration.Statements, isMethodBody: true))
                            {
                                yield return declaration;
                            }
                            break;

                        case NamespaceDeclarationSyntax namespaceDeclaration:
                            if (namespaceDeclaration is SimpleNamespaceDeclarationStatementSyntax && CurrentNamespace is not null)
                                GetDiagnosticBuilder(namespaceDeclaration.Text).ReportIllegalSimpleNamespaceDeclaration(namespaceDeclaration.NamespaceToken.Span);

                            var namespaceSym = new NamespaceSymbol(CurrentNamespace, namespaceDeclaration.Names);
                            _namespaceStack.Push(namespaceSym);
                            try
                            {
                                foreach (var declaration in enumerateDeclarations(namespaceDeclaration.Statements, isMethodBody))
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
                                foreach (var declaration in enumerateDeclarations(withNamespaceStatement.Statements, isMethodBody))
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

                        case WithMethodAliasStatementSyntax withMethodAliasStatement:

                            PushScope();
                            try
                            {
                                if (TryFindMethod(withMethodAliasStatement.NamespaceNames, withMethodAliasStatement.Identifier, out var method))
                                    _scope.TryDeclareMethodAlias(method, withMethodAliasStatement.Alias.TokenText);

                                foreach (var declaration in enumerateDeclarations(withMethodAliasStatement.Statements, isMethodBody))
                                {
                                    yield return declaration;
                                }
                            }
                            finally
                            {
                                PopScope();
                            }
                            break;

                        default:
                            if (!isMethodBody && CurrentNamespace is not null)
                                DiagnosticsBuilder.ReportIllegalStatementPlacement(statement.Span);
                            break;
                    }
                    _statementStacktrace.Pop();
                }
            }
        }

        private void ValidateMainMethod()
        {
            var mainMethods = _methodDeclarations.Values.Where(m => m.Method.Name == MainMethodName).ToArray();
            if (mainMethods.Length == 0)
                Diagnostics.ReportNoMainMethodFound();
            else if (_generatedMainMethods.Count > 1)
            {
                foreach (var generatedMethod in _generatedMainMethods)
                {
                    var syntax = getFirstSyntaxToken(generatedMethod);
                    GetDiagnosticBuilder(syntax.Text).ReportMultipleSourceTextsWithTopLevelStatements(syntax.Span);
                }
            }
            else if (mainMethods.Length > 1 && _generatedMainMethods.Any())
            {
                var generatedMethod = _generatedMainMethods.Single();
                var syntax = getFirstSyntaxToken(generatedMethod);
                GetDiagnosticBuilder(syntax.Text).ReportTopLevelStatementCannotBeCombinedWithExplicitMainMethod(syntax.Span);
            }
            else if (mainMethods.Length > 1)
            {
                foreach (var method in mainMethods)
                {
                    var declarationSyntax = _methodSymbols.Single(pair => pair.Value == method.Method).Key;
                    GetDiagnosticBuilder(declarationSyntax.Text).ReportMultipleMainMethods(declarationSyntax.Identifier.Span);
                }
            }

            SyntaxToken getFirstSyntaxToken(BoundMethodDeclaration methodDeclaration)
            {
                var node = _boundNodeToSyntaxMap[methodDeclaration.Declaration.First()];

                while (node is not SyntaxToken token)
                {
                    node = node.GetChildren().First();
                }

                return (SyntaxToken)node;
            }
        }

        #endregion Constructors

        #region Properties

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

        #region Diagnostics

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();
        private readonly Dictionary<SourceText, DiagnosticsBuilder> _diagnosticsBuilders = new();
        private SourceText? _currentText;

        private DiagnosticsBuilder DiagnosticsBuilder
        {
            get
            {
                if (_currentText is null)
                    throw new InvalidOperationException($"Current Text is not set.");

                return GetDiagnosticBuilder(_currentText);
            }
        }

        private DiagnosticsBuilder GetDiagnosticBuilder(SourceText text)
        {
            if (!_diagnosticsBuilders.TryGetValue(text, out var current))
            {
                current = new(text, Diagnostics);
                _diagnosticsBuilders.Add(text, current);
            }

            return current;
        }

        #endregion Diagnostics

        #region BindStatement

        private ImmutableArray<BoundStatement> BindStatement(StatementSyntax? syntax)
        {
            if (syntax is null)
                return ImmutableArray<BoundStatement>.Empty;

            _statementStacktrace.Push(syntax);
            try
            {
                // Handle cases that result in ImmutableArray first.
                ImmutableArray<BoundStatement>? statements = syntax.Kind switch
                {
                    SyntaxKind.NamespaceDeclaration => BindNamespaceDeclarationStatement((NamespaceDeclarationSyntax)syntax),

                    SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax),
                    SyntaxKind.WithNamespaceStatement => BindWithNamespaceStatement((WithNamespaceStatementSyntax)syntax),
                    SyntaxKind.WithAliasStatement => BindWithAliasStatement((WithAliasStatementSyntax)syntax),
                    _ => null,
                };

                // If none matched on that, handle single-statement methods.
                if (statements is null)
                {
                    var statement = syntax.Kind switch
                    {
                        SyntaxKind.MethodDeclaration => BindMethodDeclarationStatement((MethodDeclarationSyntax)syntax),

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
                    statements = statement is not null ? ImmutableArray.Create(statement) : ImmutableArray<BoundStatement>.Empty;
                }

                foreach (var statement in statements.Value)
                {
                    _boundNodeToSyntaxMap[statement] = syntax;
                }

                return statements.Value;
            }
            finally
            {
                _statementStacktrace.Pop();
            }
        }

        private ImmutableArray<BoundStatement> BindBlockStatement(BlockStatementSyntax syntax)
        {
            PushScope();
            try
            {
                return BindStatements(syntax.Statements);
            }
            finally
            {
                PopScope();
            }
        }

        private ImmutableArray<BoundStatement> BindNamespaceDeclarationStatement(NamespaceDeclarationSyntax syntax)
        {
            if (!EnableAnalysisMode && CurrentMethod is not null)
                DiagnosticsBuilder.ReportIllegalNamespaceDeclaration(syntax.NamespaceToken.Span);

            var namespaceSym = new NamespaceSymbol(CurrentNamespace, syntax.Names);
            _namespaceStack.Push(namespaceSym);
            try
            {
                return BindStatements(syntax.Statements);
            }
            finally
            {
                _namespaceStack.Pop();
            }
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.TokenText;
            var isReadOnly = syntax.VarOrSetKeyword?.Kind is SyntaxKind.SetKeyword;

            var initializer = BindExpression(syntax.Initializer);

            // Only allow explicit type in combination with 'set'
            if (syntax.ExplicitType is not null && syntax.VarOrSetKeyword is not null and { Kind: not SyntaxKind.SetKeyword })
            {
                DiagnosticsBuilder.ReportIllegalExplicitType(syntax.ExplicitType.Span);
            }

            // Don't allow void assignment
            if (initializer.Type == Types.Void && syntax.ExplicitType is null)
            {
                var span = TextSpan.FromBounds(syntax.Identifier.Span.Start, syntax.Initializer.Span.End);
                DiagnosticsBuilder.ReportCannotAssignVoid(span);
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

            if (!_scope.TryDeclareVariable(variable))
                DiagnosticsBuilder.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement? BindMethodDeclarationStatement(MethodDeclarationSyntax syntax)
        {
            if (!_methodSymbols.TryGetValue(syntax, out var method))
                return null;

            PushMethodScope(method);
            try
            {
                foreach (var parameter in method.Parameters)
                {
                    _scope.TryDeclareVariable(parameter);
                }

                var boundDeclaration = BindBody(syntax);
                if (method.ReturnType != Types.Void && method.ReturnType != Types.Error
                    && !new ControlFlowGraph(boundDeclaration).AllPathsReturn())
                {
                    DiagnosticsBuilder.ReportMethodNotAllPathsReturnValue(syntax.Identifier.Span);
                }

                var boundMethodDeclaration = new BoundMethodDeclaration(method, boundDeclaration);
                _methodDeclarations[method] = boundMethodDeclaration;

                return null;
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
            var elseStatement = BindStatement(syntax.ElseClause?.ElseStatement);

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
            var initStatement = BindStatement(syntax.InitializationStatement);
            var condition = syntax.Condition is null ? null : BindExpression(syntax.Condition, Types.Boolean);
            var updateStatement = BindStatement(syntax.UpdateStatement);
            var body = BindStatement(syntax.Body);

            return new BoundForStatement(initStatement, condition, updateStatement, body, continueLabel, breakLabel);
        }

        private BoundStatement? BindGoToStatement(GoToStatementSyntax syntax)
        {
            if (!TryFindLabel(syntax.Label, out var label))
                return null;

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
                    DiagnosticsBuilder.ReportInvalidReturnInVoidMethod(syntax.Expression.Span);

                return new BoundReturnStatement();
            }
            else
            {
                if (syntax.Expression is null)
                {
                    DiagnosticsBuilder.ReportMissingReturnExpression(syntax.Span);
                    return new BoundReturnStatement();
                }

                var expression = BindExpression(syntax.Expression, CurrentMethod.ReturnType);
                return new BoundReturnStatement(expression);
            }
        }

        private BoundStatement? BindLabelStatement(LabelStatementSyntax syntax)
        {
            if (!_scope.TryLookupLabel(syntax.Identifier.TokenText, out var label))
                return null;

            return new BoundLabelStatement(label);
        }

        private ImmutableArray<BoundStatement> BindWithNamespaceStatement(WithNamespaceStatementSyntax syntax)
        {
            var namespaceSym = new NamespaceSymbol(syntax.Names);

            bool wasIncluded = _includedNamespaces.Add(namespaceSym);
            try
            {
                return BindStatements(syntax.Statements);
            }
            finally
            {
                if (wasIncluded)
                    _includedNamespaces.Remove(namespaceSym);
            }
        }

        private ImmutableArray<BoundStatement> BindWithAliasStatement(WithAliasStatementSyntax syntax)
        {
            if (syntax is WithMethodAliasStatementSyntax methodAliasSyntax)
            {
                if (!TryFindMethod(methodAliasSyntax.NamespaceNames, methodAliasSyntax.Identifier, out var method))
                    return ImmutableArray<BoundStatement>.Empty;

                PushScope();
                try
                {
                    _scope.TryDeclareMethodAlias(method, syntax.Alias.TokenText);

                    return BindStatements(syntax.Statements);
                }
                finally
                {
                    PopScope();
                }
            }
            else throw new Exception($"Unsupported case '{syntax.GetType()}'.");
        }

        private BoundStatement? BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (!_scope.TryGetContinueLabel(out var continueLabel))
            {
                DiagnosticsBuilder.ReportUnexpectedBreakOrContinue(syntax.Span);
                return null;
            }

            return new BoundGotoStatement(continueLabel);
        }

        private BoundStatement? BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (!_scope.TryGetBreakLabel(out var breakLabel))
            {
                DiagnosticsBuilder.ReportUnexpectedBreakOrContinue(syntax.Span);
                return null;
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
                DiagnosticsBuilder.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.TokenText, boundOperand.Type);
                return BoundErrorExpression.Instance;
            }

            if (boundOp.Kind.IsIncrementOrDecrement())
            {
                if (boundOperand is not BoundVariableExpression variableExpression)
                {
                    DiagnosticsBuilder.ReportIncrementOperandMustBeVariable(syntax.Span);
                    return BoundErrorExpression.Instance;
                }
                else if (variableExpression.Variable.IsReadOnly)
                {
                    DiagnosticsBuilder.ReportCannotAssignReadOnly(syntax.Span, variableExpression.Variable.Name);
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
                    DiagnosticsBuilder.ReportCanOnlyPipeToMethods(syntax.OperatorToken.Span);
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
                DiagnosticsBuilder.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.TokenText, boundLeft.Type, boundRight.Type);
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
                DiagnosticsBuilder.ReportCannotAssignReadOnly(syntax.EqualsToken.Span, variable.Name);

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
                    DiagnosticsBuilder.ReportUndefinedBinaryOperator(syntax.EqualsToken.Span, syntax.EqualsToken.TokenText[0..1], variable.Type, boundExpression.Type);
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
            if (string.IsNullOrEmpty(syntax.Identifier.TokenText))
                return BoundErrorExpression.Instance;

            // Locate method.
            if (!TryFindMethod(syntax.NamespaceNames, syntax.Identifier, out var method))
                return BoundErrorExpression.Instance;

            if (method.ReturnType.IsError())
                return BoundErrorExpression.Instance;

            // Bind arguments.

            bool unexpectedPipedArg = false;
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
                    DiagnosticsBuilder.ReportUnexpectedPipedArgument(argument.Span);
                    unexpectedPipedArg = true;

                    boundArguments.Add(BoundErrorExpression.Instance);
                }
            }

            // If piped argument was not consumed, append to arguemnts.
            if (pipedParameter is not null)
            {
                var boundArgument = BindExpression(pipedParameter);
                boundArguments.Add(boundArgument);
                //pipedParameter = null;
            }

            // Validate argument count.
            if (!unexpectedPipedArg && boundArguments.Count != method.Parameters.Length)
            {
                DiagnosticsBuilder.ReportWrongArgumentCount(syntax.Span, method.Name, method.Parameters.Length, boundArguments.Count);
                return BoundErrorExpression.Instance;
            }

            // Validate argument types.
            for (int i = 0; i < boundArguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = method.Parameters[i];

                if (argument.Type.IsError())
                    continue;

                if (!Conversion.Classify(argument.Type, parameter.Type).IsImplicit)
                {
                    DiagnosticsBuilder.ReportWrongArgumentType(syntax.Span, method.Name, parameter.Name, parameter.Type, argument.Type);
                    boundArguments[i] = BoundErrorExpression.Instance;
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
                DiagnosticsBuilder.ReportNoExplicitConversion(syntax.Span, expression.Type, type);
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
                DiagnosticsBuilder.ReportTypeDoesNotHaveIndexer(syntax.Span, operand.Type.Name);
                return BoundErrorExpression.Instance;
            }

            if (syntax.Parameters.Count != indexer.Parameters.Length)
            {
                DiagnosticsBuilder.ReportWrongArgumentCount(syntax.Span,
                                                            "[]",
                                                            indexer.Parameters.Length,
                                                            syntax.Parameters.Count);
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
                    var span = syntax.SizeExpression!.Span;
                    DiagnosticsBuilder.ReportSizeMustBeConstantWithInitializer(span);
                    itemType ??= Types.Error;
                }

                void BindArrayInitializer(object size)
                {
                    if (!size.Equals(initializer.Count))
                    {
                        DiagnosticsBuilder.ReportArraySizeMismatch(initializer.Span);
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

        private enum MethodDeclarationResult { Success, CannotInferReturnType }

        private static readonly MethodSymbol _mockMethod = new("mock", ImmutableArray<ParameterSymbol>.Empty, Types.Error);

        private MethodDeclarationResult TryDeclareMethod(MethodDeclarationSyntax syntax, bool declareImplicitReturnTypesAsError = false)
        {
            if (_methodSymbols.ContainsKey(syntax))
                return MethodDeclarationResult.Success;

            string name = getOrGenerateMethodName(syntax.Identifier.TokenText);
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
                PushMethodScope(_mockMethod);
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

                    // To easily find any return-statements, we lower this primitive body.
                    boundDeclaration = Lowerer.Lower(boundDeclaration, _labelGenerator);

                    var returnStatements = boundDeclaration.OfType<BoundReturnStatement>();
                    var returnType = ResolveImplicitType(this, returnStatements);

                    if (returnType is null)
                        return MethodDeclarationResult.CannotInferReturnType;

                    method = new MethodSymbol(CurrentNamespace, name, parameters, returnType);
                }
                finally
                {
                    EnableAnalysisMode = false;
                    PopMethodScope();
                }
            }

            if (!_scope.TryDeclareMethod(method))
            {
                DiagnosticsBuilder.ReportMethodAlreadyDeclared(syntax.Identifier.Span, name);
            }

            _methodSymbols.Add(syntax, method);
            return MethodDeclarationResult.Success;

            string getOrGenerateMethodName(string methodName)
            {
                return string.Join(GeneratedMethodSeparator,
                    _statementStacktrace
                        .OfType<MethodDeclarationSyntax>()
                        .Select(dec => dec.Identifier.TokenText)
                        .Reverse());
            }

            static TypeSymbol? ResolveImplicitType(Binder @this, IEnumerable<BoundReturnStatement> returnStatements)
            {
                if (!returnStatements.Any() || returnStatements.All(r => r.Expression is null))
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
                        return ImmutableArray.Create<BoundStatement>(expressionStatement);
                    }
                    else
                    {
                        var returnStatement = new BoundReturnStatement(expression);
                        return ImmutableArray.Create<BoundStatement>(returnStatement);
                    }
                }
                else
                {
                    return BindStatements(syntax.Declaration.Statements);
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
            string name = syntax.Identifier.TokenText;

            var label = new LabelSymbol(name);
            if (!_scope.TryDeclareLabel(label))
                DiagnosticsBuilder.ReportLabelAlreadyDeclared(syntax.Identifier.Span, name);
        }

        private ImmutableArray<ParameterSymbol> BindParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(parameters.Count);
            var parameterNames = new HashSet<string>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = BindParameter(parameters[i]);
                if (!parameterNames.Add(parameter.Name))
                    DiagnosticsBuilder.ReportDuplicateParameterName(parameters[i].Identifier.Span, parameter.Name);

                builder.Add(parameter);
            }

            return builder.MoveToImmutable();
        }

        private static ParameterSymbol BindParameter(ParameterSyntax parameter)
        {
            var name = parameter.Identifier.TokenText;
            var type = ResolveType(parameter.TypeToken) ?? Types.Error;

            return new ParameterSymbol(name, type);
        }

        /// <summary>
        /// Reports a diagnostic if there's an invalid assignment.
        /// </summary>
        private BoundExpression BindConvertion(TextSpan span, BoundExpression expression, TypeSymbol resultType)
        {
            var conversion = Conversion.Classify(expression.Type, resultType);
            if (conversion.IsImplicit)
            {
                return expression;
            }
            else if (conversion == Conversion.Explicit)
            {
                DiagnosticsBuilder.ReportCannotImplicitlyConvert(span, expression.Type, resultType);
                return BoundErrorExpression.Instance;
            }
            else
            {
                DiagnosticsBuilder.ReportCannotConvert(span, expression.Type, resultType);
                return BoundErrorExpression.Instance;
            }
        }

        #region TryFind

        private bool TryFindVariable(SyntaxToken identifierToken, [NotNullWhen(true)] out VariableSymbol? variable)
        {
            string name = identifierToken.TokenText;
            if (string.IsNullOrEmpty(name))
            {
                variable = null;
                return false;
            }

            if (!_scope.TryLookupVariable(name, out variable))
            {
                DiagnosticsBuilder.ReportUndefinedSymbol(identifierToken.Span, name);

                _scope.TryDeclareVariable(new VariableSymbol(name, isReadOnly: false, Types.Error));
                return false;
            }

            return true;
        }

        private bool TryFindLabel(SyntaxToken identifierToken, [NotNullWhen(true)] out LabelSymbol? label)
        {
            string name = identifierToken.TokenText;
            if (string.IsNullOrEmpty(name))
            {
                label = null;
                return false;
            }

            if (!_scope.TryLookupLabel(name, out label))
            {
                DiagnosticsBuilder.ReportUndefinedSymbol(identifierToken.Span, name);

                _scope.TryDeclareLabel(new LabelSymbol(name));
                return false;
            }

            return true;
        }

        private bool TryFindMethod(NamespaceSymbol? namespaceSym, SyntaxToken identifierToken, [NotNullWhen(true)] out MethodSymbol? method)
        {
            var methodName = identifierToken.TokenText;
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

                    var localDeclaration = localDeclarations.FirstOrDefault(dec => dec.Identifier.TokenText == methodName);
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
                    DiagnosticsBuilder.ReportAmbigousSymbolReference(identifierToken.Span, resolvedMethods);

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

            DiagnosticsBuilder.ReportUndefinedSymbol(identifierToken.Span, methodName);
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

        private static TypeSymbol ResolveType(TypeExpressionSyntax typeExpression)
        {
            if (typeExpression.IsArray)
            {
                var typeArgumentExpression = typeExpression.GetArrayTypeArgument();
                var typeArgument = ResolveType(typeArgumentExpression);
                return Types.Array.MakeConcreteType(typeArgument);
            }
            else
                return ResolveType(typeExpression.TypeToken.Kind) ?? Types.Error;
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
                foreach (var method in Methods.GetAll())
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