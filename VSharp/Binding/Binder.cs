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

using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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
            BoundScope parentScope = CreateParentScopes(previous);

            Binder binder = new Binder(parentScope);
            BoundStatement statement = binder.BindStatement(syntax.Statement);

            ImmutableArray<Diagnostic> diagnostics = previous is null ?
                binder.Diagnostics.ToImmutableArray() :
                previous.Diagnostics.AddRange(binder.Diagnostics);

            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();
            return new BoundGlobalScope(previous, diagnostics, variables, statement);
        }

        private static BoundScope CreateParentScopes(BoundGlobalScope? previous)
        {
            if (previous is null)
                return CreateRootScope();

            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = CreateRootScope();
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                    scope.TryDeclareVariable(v);

                parent = scope;
            }

            return parent;

            static BoundScope CreateRootScope()
            {
                BoundScope root = new BoundScope();

                foreach (var method in BuiltinMethods.GetAll())
                    root.TryDeclareMethod(method);

                return root;
            }
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

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();
            PushScope();

            foreach (StatementSyntax statementSyntax in syntax.Statements)
            {
                var statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }

            PopScope();
            return new BoundBlockStatement(statements.ToImmutable());
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? "?";
            bool declare = syntax.Identifier.Text != null;
            bool isReadOnly = false; // TODO: introduce readonly values and compile-time constants.

            BoundExpression initializer = BindExpression(syntax.Initializer);

            // Don't allow void assignment
            if (initializer.Type == TypeSymbol.Void && syntax.Keyword.Kind == SyntaxKind.VarKeyword)
            {
                TextSpan span = TextSpan.FromBounds(syntax.Identifier.Span.Start, syntax.Initializer.Span.End);
                Diagnostics.ReportCannotAssignVoid(span);
            }

            TypeSymbol? variableType = ResolveType(syntax.Keyword.Kind);
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

            VariableSymbol variable = new VariableSymbol(name, isReadOnly, variableType);
            if (declare && !_scope.TryDeclareVariable(variable))
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition.Expression, TypeSymbol.Boolean);
            BoundStatement thenStatement = BindStatement(syntax.ThenStatement);
            BoundStatement? elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition.Expression, TypeSymbol.Boolean);
            BoundStatement body = BindStatement(syntax.Body);

            return new BoundWhileStatement(condition, body);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            PushScope();

            BoundStatement initStatement = BindStatement(syntax.InitializationStatement);
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Boolean);
            BoundStatement updateStatement = BindStatement(syntax.UpdateStatement);
            BoundStatement body = BindStatement(syntax.Body);

            PopScope();
            return new BoundForStatement(initStatement, condition, updateStatement, body);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression expression = BindExpression(syntax.Expression);
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
            BoundExpression expression = BindExpression(syntax);

            if (expression.Type != targetType &&
                expression.Type != TypeSymbol.Error &&
                targetType != TypeSymbol.Error)
            {
                Diagnostics.ReportCannotConvert(syntax.Span, expression.Type, targetType);
            }

            return expression;
        }

        private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            object value = syntax.Value ?? 0;
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
            BoundExpression boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Type == TypeSymbol.Error)
                return BoundErrorExpression.Instace;

            BoundUnaryOperator? boundOp = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, syntax.UnaryType, boundOperand.Type);
            if (boundOp is null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return BoundErrorExpression.Instace;
            }

            return new BoundUnaryExpression(boundOp, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            BoundExpression boundLeft = BindExpression(syntax.Left);
            BoundExpression boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
                return BoundErrorExpression.Instace;

            BoundBinaryOperator? boundOp = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
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
            TypeSymbol type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? throw new Exception($"Failed to resolve type kind '{syntax.TypeExpression.TypeIdentifier}'.");
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
            if (!TryFindVariable(syntax.IdentifierToken, out VariableSymbol? variable) || variable.Type == TypeSymbol.Error)
                return BoundErrorExpression.Instace;

            if (variable.IsReadOnly)
                Diagnostics.ReportCannotAssignReadOnly(syntax.EqualsToken.Span, variable.Name);

            BoundExpression boundExpression = BindExpression(syntax.Expression);
            if (boundExpression.Type == TypeSymbol.Error)
                return BoundErrorExpression.Instace;

            if (syntax.EqualsToken.Kind != SyntaxKind.EqualsToken)
            {
                BoundVariableExpression boundVariable = new BoundVariableExpression(variable);
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

                BoundBinaryOperator? boundOp = BoundBinaryOperator.Bind(operatorKind, variable.Type, boundExpression.Type);
                if (boundOp is null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.EqualsToken.Span, syntax.EqualsToken.Text, variable.Type, boundExpression.Type);
                    return BoundErrorExpression.Instace;
                }

                boundExpression = new BoundBinaryExpression(boundVariable, boundOp, boundExpression);
                if (boundExpression.Type == TypeSymbol.Error)
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
                BoundExpression argument = boundArguments[i];
                ParameterSymbol parameter = method.Paramaters[i];

                if (argument.Type != parameter.Type)
                {
                    Diagnostics.ReportWrongArgumentType(syntax.Span, method.Name, parameter.Name, parameter.Type, argument.Type);
                    return BoundErrorExpression.Instace;
                }
            }

            return new BoundCallExpression(method, boundArguments.ToImmutableArray());
        }

        private BoundExplicitCastExpression BindExplicitCastExpression(ExplicitCastExpressionSyntax syntax)
        {
            TypeSymbol type = ResolveType(syntax.TypeExpression.TypeIdentifier.Kind) ?? throw new Exception($"Failed to resolve type kind '{syntax.TypeExpression.TypeIdentifier}'.");
            BoundExpression expression = BindExpression(syntax.Expression);

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