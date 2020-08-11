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
using System.Collections.Immutable;

namespace VSharp.Binding
{
    internal abstract class BoundTreeRewriter
    {
        #region RewriteStatement

        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            return node.Kind switch
            {
                BoundNodeKind.BlockStatement => RewriteBlockStatement((BoundBlockStatement)node),
                BoundNodeKind.VariableDeclarationStatement => RewriteVariableDeclarationStatement((BoundVariableDeclarationStatement)node),
                BoundNodeKind.MethodDeclarationStatement => RewriteMethodDeclarationStatement((BoundMethodDeclarationStatement)node),
                BoundNodeKind.IfStatement => RewriteIfStatement((BoundIfStatement)node),
                BoundNodeKind.WhileStatement => RewriteWhileStatement((BoundWhileStatement)node),
                BoundNodeKind.ForStatement => RewriteForStatement((BoundForStatement)node),
                BoundNodeKind.LabelStatement => RewriteLabelStatement((BoundLabelStatement)node),
                BoundNodeKind.GotoStatement => RewriteGotoStatement((BoundGotoStatement)node),
                BoundNodeKind.ConditionalGotoStatement => RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node),
                BoundNodeKind.ExpressionStatement => RewriteExpressionStatement((BoundExpressionStatement)node),
                BoundNodeKind.NoOpStatement => node,

                _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
            };
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            // Rewrite method declarations.
            ImmutableArray<BoundMethodDeclarationStatement>.Builder? methodDeclarationBuilder = null;
            for (int i = 0; i < node.MethodDeclarations.Length; i++)
            {
                var oldDeclaration = node.MethodDeclarations[i];
                var newDeclaration = RewriteMethodDeclarationStatement(oldDeclaration);

                if (methodDeclarationBuilder is null && newDeclaration != oldDeclaration)
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    methodDeclarationBuilder = ImmutableArray.CreateBuilder<BoundMethodDeclarationStatement>(node.MethodDeclarations.Length);
                    for (int j = 0; j < i; j++)
                    {
                        methodDeclarationBuilder.Add(node.MethodDeclarations[i]);
                    }
                }

                if (methodDeclarationBuilder != null)
                    methodDeclarationBuilder.Add(newDeclaration);
            }

            // Rewrite statements.
            ImmutableArray<BoundStatement>.Builder? statementBuilder = null;
            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement oldStatement = node.Statements[i];
                BoundStatement newStatement = RewriteStatement(oldStatement);

                if (statementBuilder is null && (newStatement != oldStatement || ignore(oldStatement)))
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    statementBuilder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);
                    for (int j = 0; j < i; j++)
                    {
                        tryAdd(node.Statements[j]);
                    }
                }

                if (statementBuilder != null)
                    tryAdd(newStatement);
            }

            if (methodDeclarationBuilder is null && statementBuilder is null)
                return node;

            var statements = statementBuilder?.ToImmutable() ?? node.Statements;
            var methodDeclarations = methodDeclarationBuilder?.MoveToImmutable() ?? node.MethodDeclarations;
            return new BoundBlockStatement(statements, methodDeclarations);

            void tryAdd(BoundStatement statement)
            {
                if (!ignore(statement))
                {
                    statementBuilder.Add(statement);
                }
            }

            static bool ignore(BoundStatement statement)
            {
                return statement.Kind == BoundNodeKind.NoOpStatement;
            }
        }

        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            BoundExpression initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
                return node;

            return new BoundVariableDeclarationStatement(node.Variable, initializer);
        }

        protected virtual BoundMethodDeclarationStatement RewriteMethodDeclarationStatement(BoundMethodDeclarationStatement node)
        {
            var declaration = RewriteBlockStatement(node.Declaration);
            if (declaration is BoundBlockStatement blockStatement)
            {
                if (declaration == node.Declaration)
                    return node;
                else
                    return new BoundMethodDeclarationStatement(node.Method, blockStatement);
            }

            return new BoundMethodDeclarationStatement(node.Method, new BoundBlockStatement(ImmutableArray.Create(declaration)));
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = (node.ElseStatement == null) ? null : RewriteStatement(node.ElseStatement);

            if (condition == node.Condition &&
                thenStatement == node.ThenStatement &&
                elseStatement == node.ElseStatement)
            {
                return node;
            }

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);

            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundStatement initializationStatement = RewriteStatement(node.InitializationStatement);
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement updateStatement = RewriteStatement(node.UpdateStatement);
            BoundStatement body = RewriteStatement(node.Body);

            if (initializationStatement == node.InitializationStatement &&
                condition == node.Condition &&
                updateStatement == node.UpdateStatement &&
                body == node.Body)
            {
                return node;
            }

            return new BoundForStatement(initializationStatement, condition, updateStatement, body);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            if (condition.Kind == BoundNodeKind.LiteralExpression)
            {
                BoundLiteralExpression literalCondition = (BoundLiteralExpression)condition;

                if (node.JumpIfFalse.Equals(literalCondition.Value))
                    return BoundNoOpStatement.Instance;
                else
                    return new BoundGotoStatement(node.Label);
            }

            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfFalse);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(expression);
        }

        #endregion RewriteStatement

        #region RewriteExpression

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            return node.Kind switch
            {
                BoundNodeKind.ErrorExpression => node,
                BoundNodeKind.LiteralExpression => RewriteLiteralExpression((BoundLiteralExpression)node),
                BoundNodeKind.VariableExpression => RewriteVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => RewriteAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => RewriteUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => RewriteBinaryExpression((BoundBinaryExpression)node),
                BoundNodeKind.CallExpression => RewriteCallExpression((BoundCallExpression)node),
                BoundNodeKind.ExplicitCastExpression => RewriteExplicitCaseExpression((BoundExplicitCastExpression)node),

                _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
            };
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Variable, expression);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression operand = RewriteExpression(node.Operand);
            if (operand.Kind == BoundNodeKind.LiteralExpression)
            {
                BoundLiteralExpression literalOperand = (BoundLiteralExpression)node.Operand;

                var value = LiteralEvaluator.EvaluateUnaryExpression(node.Op, literalOperand.Value);
                if (value == literalOperand.Value)
                    return literalOperand;

                return new BoundLiteralExpression(value);
            }

            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression left = RewriteExpression(node.Left);
            BoundExpression right = RewriteExpression(node.Right);

            if (left.Kind == BoundNodeKind.LiteralExpression && right.Kind == BoundNodeKind.LiteralExpression)
            {
                BoundLiteralExpression literalLeft = (BoundLiteralExpression)left;
                BoundLiteralExpression literalRight = (BoundLiteralExpression)right;

                var value = LiteralEvaluator.EvaluateBinaryExpression(node.Op, literalLeft.Value, literalRight.Value);
                if (value == literalLeft.Value)
                    return literalLeft;
                else if (value == literalRight.Value)
                    return literalRight;

                return new BoundLiteralExpression(value);
            }

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;
            for (int i = 0; i < node.Arguments.Length; i++)
            {
                BoundExpression oldArgument = node.Arguments[i];
                BoundExpression newArgument = RewriteExpression(oldArgument);

                if (builder is null && newArgument != oldArgument)
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);
                    for (int j = 0; j < i; j++)
                        builder.Add(node.Arguments[j]);
                }

                if (builder != null)
                    builder.Add(newArgument);
            }

            if (builder is null)
                return node;

            return new BoundCallExpression(node.Method, builder.MoveToImmutable());
        }

        protected virtual BoundExpression RewriteExplicitCaseExpression(BoundExplicitCastExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression.Kind == BoundNodeKind.LiteralExpression)
            {
                BoundLiteralExpression literalExpression = (BoundLiteralExpression)expression;
                var value = LiteralEvaluator.EvaluateExplicitCastExpression(node.Type, literalExpression.Value);

                return new BoundLiteralExpression(value);
            }

            if (expression == node.Expression)
                return node;

            return new BoundExplicitCastExpression(node.Type, expression);
        }

        #endregion RewriteExpression
    }
}