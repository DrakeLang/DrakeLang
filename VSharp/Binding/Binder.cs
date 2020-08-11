//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Niklas Gransjøen
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
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;

namespace VSharp.Binding
{
    internal sealed class Binder
    {
        private BoundScope _scope;

        #region Constructors

        private Binder(BoundScope parent)
        {
            _scope = new BoundScope(parent);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            var parentScope = CreateParentScopes(previous);

            var binder = new Binder(parentScope);

            var statements = binder.BindStatements(syntax.Statements);
            var diagnostics = previous is null ?
                binder.Diagnostics.ToImmutableArray() :
                previous.Diagnostics.AddRange(binder.Diagnostics);

            var variables = binder._scope.GetDeclaredVariables();
            return new BoundGlobalScope(previous, diagnostics, variables, statements);
        }

        private static BoundScope CreateParentScopes(BoundGlobalScope? previous)
        {
            if (previous is null)
                return CreateRootScope();

            var stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var parent = CreateRootScope();
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                    scope.TryDeclareVariable(v);

                parent = scope;
            }

            return parent;

            static BoundScope CreateRootScope()
            {
                var root = new BoundScope();
                foreach (var method in BuiltinMethods.GetAll())
                    root.TryDeclareMethod(method);

                return root;
            }
        }

        private BoundBlockStatement BindStatements(ImmutableArray<StatementSyntax> statements)
        {
            // Declare all methods in scope first.
            int methodDeclarations = 0;
            foreach (var statement in statements.Where(s => s.Kind == SyntaxKind.MethodDeclarationStatement))
            {
                DeclareMethod((MethodDeclarationStatementSyntax)statement);
                methodDeclarations++;
            }

            var methodDeclarationBuilder = ImmutableArray.CreateBuilder<BoundMethodDeclarationStatement>(methodDeclarations);
            var statementBuilder = ImmutableArray.CreateBuilder<BoundStatement>();
            foreach (var statement in statements)
            {
                if (statement.Kind == SyntaxKind.MethodDeclarationStatement)
                {
                    var methodDeclaration = BindMethodDeclarationStatement((MethodDeclarationStatementSyntax)statement);
                    methodDeclarationBuilder.Add(methodDeclaration);
                }
                else
                {
                    var boundStatement = BindStatement(statement);
                    statementBuilder.Add(boundStatement);
                }
            }

            return new BoundBlockStatement(statementBuilder.ToImmutable(), methodDeclarationBuilder.MoveToImmutable());
        }

        #endregion Constructors

        #region Properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        #endregion Properties

        #region BindStatement

        private BoundStatement BindStatement(StatementSyntax syntax)
        {
            return syntax.Kind switch
            {
                SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax),
                SyntaxKind.VariableDeclarationStatement => BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax),
                SyntaxKind.IfStatement => BindIfStatement((IfStatementSyntax)syntax),
                SyntaxKind.WhileStatement => BindWhileStatement((WhileStatementSyntax)syntax),
                SyntaxKind.ForStatement => BindForStatement((ForStatementSyntax)syntax),
                SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax),

                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax)
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

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;
            bool isReadOnly = false; // TODO: introduce readonly values and compile-time constants.

            var initializer = BindExpression(syntax.Initializer);

            // Don't allow void assignment
            if (initializer.Type == TypeSymbol.Void && syntax.Keyword.Kind == SyntaxKind.VarKeyword)
            {
                TextSpan span = TextSpan.FromBounds(syntax.Identifier.Span.Start, syntax.Initializer.Span.End);
                Diagnostics.ReportCannotAssignVoid(span);
            }

            var variableType = ResolveType(syntax.Keyword.Kind);
            if (variableType is null)
                variableType = initializer.Type;

            var conversion = Conversion.Classify(variableType, initializer.Type);
            if (conversion == Conversion.Explicit)
            {
                Diagnostics.ReportCannotImplicitlyConvert(syntax.Initializer.Span, initializer.Type, variableType);
            }
            else if (conversion == Conversion.None)
            {
                Diagnostics.ReportCannotConvert(syntax.Initializer.Span, initializer.Type, variableType);
            }

            var variable = new VariableSymbol(name, isReadOnly, variableType);
            if (declare && !_scope.TryDeclareVariable(variable))
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundMethodDeclarationStatement BindMethodDeclarationStatement(MethodDeclarationStatementSyntax syntax)
        {
            if (!_scope.TryLookupMethod(syntax.Identifier.Text, out var method))
                throw new Exception($"Failed to resolve symbol for method '{syntax.Identifier.Text}'.");

            PushScope();
            try
            {
                var parameterBuilder = ImmutableArray.CreateBuilder<VariableSymbol>(method.Paramaters.Length);
                foreach (var parameter in method.Paramaters)
                {
                    var variable = new VariableSymbol(parameter.Name, isReadOnly: true, parameter.Type);
                    parameterBuilder.Add(variable);
                    _scope.TryDeclareVariable(variable);
                }

                var boundDeclaration = BindBlockStatement(syntax.Declaration);
                return new BoundMethodDeclarationStatement(method, parameterBuilder.MoveToImmutable(), boundDeclaration);
            }
            finally
            {
                PopScope();
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition.Expression, TypeSymbol.Boolean);
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition.Expression, TypeSymbol.Boolean);
            var body = BindStatement(syntax.Body);

            return new BoundWhileStatement(condition, body);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            PushScope();

            var initStatement = BindStatement(syntax.InitializationStatement);
            var condition = BindExpression(syntax.Condition, TypeSymbol.Boolean);
            var updateStatement = BindStatement(syntax.UpdateStatement);
            var body = BindStatement(syntax.Body);

            PopScope();
            return new BoundForStatement(initStatement, condition, updateStatement, body);
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

            if (expression.Type != targetType &&
                !expression.Type.IsError() &&
                !targetType.IsError())
            {
                Diagnostics.ReportCannotConvert(syntax.Span, expression.Type, targetType);
            }

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
                return BoundErrorExpression.Instace;

            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Type.IsError())
                return BoundErrorExpression.Instace;

            var boundOp = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, syntax.UnaryType, boundOperand.Type);
            if (boundOp is null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return BoundErrorExpression.Instace;
            }

            return new BoundUnaryExpression(boundOp, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type.IsError() || boundRight.Type.IsError())
                return BoundErrorExpression.Instace;

            var boundOp = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOp is null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return BoundErrorExpression.Instace;
            }

            return new BoundBinaryExpression(boundLeft, boundOp, boundRight);
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private static BoundExpression BindTypeofExpression(TypeofExpressionSyntax syntax)
        {
            var type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? TypeSymbol.Error;
            return new BoundLiteralExpression(type.Name);
        }

        private BoundExpression BindNameofExpression(NameofExpressionSyntax syntax)
        {
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable))
                return BoundErrorExpression.Instace;

            return new BoundLiteralExpression(variable.Name);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable) || variable.Type.IsError())
                return BoundErrorExpression.Instace;

            if (variable.IsReadOnly)
                Diagnostics.ReportCannotAssignReadOnly(syntax.EqualsToken.Span, variable.Name);

            var boundExpression = BindExpression(syntax.Expression);
            if (boundExpression.Type.IsError())
                return BoundErrorExpression.Instace;

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
                    return BoundErrorExpression.Instace;
                }

                boundExpression = new BoundBinaryExpression(boundVariable, boundOp, boundExpression);
                if (boundExpression.Type.IsError())
                    return BoundErrorExpression.Instace;
            }

            var conversion = Conversion.Classify(boundExpression.Type, variable.Type);
            if (conversion == Conversion.Identity || conversion == Conversion.Implicit)
            {
                return new BoundAssignmentExpression(variable, boundExpression);
            }
            else if (conversion == Conversion.Explicit)
            {
                Diagnostics.ReportCannotImplicitlyConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
                return boundExpression;
            }
            else
            {
                Diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
                return boundExpression;
            }
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            // Locate method.
            if (!_scope.TryLookupMethod(syntax.Identifier.Text, out MethodSymbol? method))
            {
                Diagnostics.ReportUndefinedMethod(syntax.Identifier.Span, syntax.Identifier.Text);
                return BoundErrorExpression.Instace;
            }

            // Validate argument count.
            if (syntax.Arguments.Count != method.Paramaters.Length)
            {
                Diagnostics.ReportWrongArgumentCount(syntax.Span, method.Name, method.Paramaters.Length, syntax.Arguments.Count);
                return BoundErrorExpression.Instace;
            }

            // Bind arguments.
            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();
            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArguments.Add(boundArgument);
            }

            // Validate argument types.
            for (int i = 0; i < method.Paramaters.Length; i++)
            {
                var argument = boundArguments[i];
                var parameter = method.Paramaters[i];

                if (argument.Type != parameter.Type && !argument.Type.IsError() && !parameter.Type.IsError())
                {
                    Diagnostics.ReportWrongArgumentType(syntax.Span, method.Name, parameter.Name, parameter.Type, argument.Type);
                    return BoundErrorExpression.Instace;
                }
            }

            return new BoundCallExpression(method, boundArguments.ToImmutableArray());
        }

        private BoundExplicitCastExpression BindExplicitCastExpression(ExplicitCastExpressionSyntax syntax)
        {
            var type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? TypeSymbol.Error;
            var expression = BindExpression(syntax.Expression);

            return new BoundExplicitCastExpression(type, expression);
        }

        #endregion BindExpression

        #region Helpers

        /// <summary>
        /// Moves into a new scope.
        /// </summary>
        private void PushScope()
        {
            _scope = new BoundScope(_scope);
        }

        /// <summary>
        /// Drops the current scope, and moves up to its parent.
        /// </summary>
        private void PopScope()
        {
            _scope = _scope.Parent ?? throw new InvalidOperationException("Scope's parent was null");
        }

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
                Diagnostics.ReportUndefinedName(identifierToken.Span, name);

                _scope.TryDeclareVariable(new VariableSymbol(name, isReadOnly: true, TypeSymbol.Error));
                return false;
            }

            return true;
        }

        private void DeclareMethod(MethodDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;
            if (!declare)
                return;

            var parameters = BindParameters(syntax.Parameters);
            var method = new MethodSymbol(name, parameters, TypeSymbol.Void);

            if (!_scope.TryDeclareMethod(method))
                Diagnostics.ReportMethodAlreadyDeclared(syntax.Identifier.Span, name);
        }

        private ImmutableArray<ParameterSymbol> BindParameters(SeparatedSyntaxCollection<ParameterSyntax> parameters)
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
            var type = ResolveType(parameter.TypeToken.TypeIdentifier.Kind) ?? TypeSymbol.Error;

            return new ParameterSymbol(name, type);
        }

        #endregion Helpers

        #region Utilities

        private static TypeSymbol? ResolveType(SyntaxKind typeKeyword) => typeKeyword switch
        {
            SyntaxKind.BoolKeyword => TypeSymbol.Boolean,
            SyntaxKind.IntKeyword => TypeSymbol.Int,
            SyntaxKind.StringKeyword => TypeSymbol.String,
            SyntaxKind.FloatKeyword => TypeSymbol.Float,
            SyntaxKind.VarKeyword => null,

            _ => throw new Exception($"Unexpected keyword '{typeKeyword}'."),
        };

        #endregion Utilities
    }
}