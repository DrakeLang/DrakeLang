//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
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

using PHPSharp.Symbols;
using PHPSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace PHPSharp.Binding
{
    internal class Binder
    {
        private BoundScope _scope;

        public Binder(BoundScope? parent)
        {
            _scope = new BoundScope(parent);
        }

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

        private BoundVariableDeclarationStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            string name = syntax.Identifier.Text ?? throw new Exception("Variable name cannot be null");
            bool isReadOnly = false;

            BoundExpression initializer = BindExpression(syntax.Initializer);
            var variableType = syntax.Keyword.Kind switch
            {
                SyntaxKind.BoolKeyword => typeof(bool),
                SyntaxKind.IntKeyword => typeof(int),
                SyntaxKind.StringKeyword => typeof(string),
                _ => initializer.Type,
            };

            if (variableType != initializer.Type)
                Diagnostics.ReportCannotConvert(syntax.Initializer.Span, initializer.Type, variableType);

            VariableSymbol variable = new VariableSymbol(name, isReadOnly, variableType);

            if (!_scope.TryDeclare(variable))
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundIfStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition.Expression, typeof(bool));
            BoundStatement thenStatement = BindStatement(syntax.ThenStatement);
            BoundStatement? elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundWhileStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition.Expression, typeof(bool));
            BoundStatement body = BindStatement(syntax.Body);

            return new BoundWhileStatement(condition, body);
        }

        private BoundForStatement BindForStatement(ForStatementSyntax syntax)
        {
            PushScope();

            BoundStatement initStatement = BindStatement(syntax.InitializationStatement);
            BoundExpression condition = BindExpression(syntax.Condition, typeof(bool));
            BoundStatement updateStatement = BindStatement(syntax.UpdateStatement);
            BoundStatement body = BindStatement(syntax.Body);

            PopScope();
            return new BoundForStatement(initStatement, condition, updateStatement, body);
        }

        private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
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
                SyntaxKind.ParenthesizedExpression => BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax),
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
                SyntaxKind.NameExpression => BindNameExpression((NameExpressionSyntax)syntax),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)syntax),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax),

                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, Type targetType)
        {
            BoundExpression expression = BindExpression(syntax);
            if (expression.Type != targetType)
                Diagnostics.ReportCannotConvert(syntax.Span, expression.Type, targetType);

            return expression;
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            object value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            string? name = syntax.IdentifierToken.Text;
            if (string.IsNullOrEmpty(name))
                return new BoundLiteralExpression(0);

            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
                return new BoundLiteralExpression(0);
            }

            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            string? name = syntax.IdentifierToken.Text;
            BoundExpression boundExpression = BindExpression(syntax.Expression);

            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
                return boundExpression;
            }

            if (variable.IsReadOnly)
                Diagnostics.ReportCannotAssign(syntax.EqualsToken.Span, name);

            if (boundExpression.Type != variable.Type)
            {
                Diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
                return boundExpression;
            }

            if (syntax.EqualsToken.Kind != SyntaxKind.EqualsToken)
            {
                BoundBinaryOperatorKind operatorKind;
                switch (syntax.EqualsToken.Kind)
                {
                    case SyntaxKind.PlusEqualsToken:
                        operatorKind = BoundBinaryOperatorKind.Addition;
                        break;

                    case SyntaxKind.MinusEqualsToken:
                        operatorKind = BoundBinaryOperatorKind.Subtraction;
                        break;

                    case SyntaxKind.StarEqualsToken:
                        operatorKind = BoundBinaryOperatorKind.Multiplication;
                        break;

                    case SyntaxKind.SlashEqualsToken:
                        operatorKind = BoundBinaryOperatorKind.Division;
                        break;

                    case SyntaxKind.AmpersandEqualsToken:
                    case SyntaxKind.PipeEqualsToken:
                        Diagnostics.ReportUndefinedBinaryOperator(syntax.EqualsToken.Span, syntax.EqualsToken.Text, variable.Type, boundExpression.Type);
                        return boundExpression;

                    default:
                        throw new Exception($"Unexpected assignment kind {syntax.EqualsToken.Kind}");
                }

                BoundVariableExpression boundVariable = new BoundVariableExpression(variable);
                BoundBinaryOperator? boundOp = BoundBinaryOperator.Bind(operatorKind, variable.Type, boundExpression.Type);
                if (boundOp is null)
                {
                    throw new InvalidOperationException();
                }

                boundExpression = new BoundBinaryExpression(boundVariable, boundOp, boundExpression);
            }

            return new BoundAssignmentExpression(variable, boundExpression);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            BoundExpression boundOperand = BindExpression(syntax.Operand);
            BoundUnaryOperator? op = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, syntax.UnaryType, boundOperand.Type);

            if (op is null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return boundOperand;
            }

            return new BoundUnaryExpression(op, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            BoundExpression boundLeft = BindExpression(syntax.Left);
            BoundExpression boundRight = BindExpression(syntax.Right);
            BoundBinaryOperator? op = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (op is null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return boundLeft;
            }

            return new BoundBinaryExpression(boundLeft, op, boundRight);
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

        #endregion Helpers

        #region Statics

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            BoundScope? parentScope = CreateParentScopes(previous);

            Binder binder = new Binder(parentScope);
            BoundStatement statement = binder.BindStatement(syntax.Statement);
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();

            ImmutableArray<Diagnostic> diagnostics = previous == null ?
                binder.Diagnostics.ToImmutableArray() :
                previous.Diagnostics.AddRange(binder.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, variables, statement);
        }

        private static BoundScope? CreateParentScopes(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope? parent = null;
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                    scope.TryDeclare(v);

                parent = scope;
            }

            return parent;
        }

        #endregion Statics
    }
}