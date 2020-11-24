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
using VSharp.Symbols;

namespace VSharp.Binding
{
    internal abstract class BoundTreeRewriter
    {
        private readonly List<BoundStatement> _statementStack = new List<BoundStatement>();
        private readonly List<BoundExpression> _expressionStack = new List<BoundExpression>();

        public BoundTreeRewriter()
        {
        }

        #region Properties

        protected BoundStatement? RootStatement => _statementStack.FirstOrDefault();
        protected BoundStatement? CurrentStatement => _statementStack.LastOrDefault();

        protected BoundExpression? RootExpression => _expressionStack.FirstOrDefault();

        /// <summary>
        /// Keeps track of what expression uses what variables.
        /// </summary>
        protected Dictionary<VariableSymbol, HashSet<BoundExpression>> VariableUsage { get; } = new Dictionary<VariableSymbol, HashSet<BoundExpression>>();

        /// <summary>
        /// Keeps track of variables that are reassigned after initialization.
        /// </summary>
        protected HashSet<VariableSymbol> ReassignedVariables { get; } = new HashSet<VariableSymbol>();

        /// <summary>
        /// Mapping of updated variables.
        /// </summary>
        protected Dictionary<VariableSymbol, VariableSymbol> UpdatedVariables { get; } = new Dictionary<VariableSymbol, VariableSymbol>();

        #endregion Properties

        #region RewriteStatement

        protected ImmutableArray<BoundStatement>? RewriteStatements(IReadOnlyList<BoundStatement> statements)
        {
            // Rewrite statements.
            ImmutableArray<BoundStatement>.Builder? builder = null;
            for (int i = 0; i < statements.Count; i++)
            {
                var oldStatement = statements[i];
                var newStatement = RewriteStatement(oldStatement);

                if (builder is null && (newStatement != oldStatement))
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    builder = ImmutableArray.CreateBuilder<BoundStatement>(statements.Count);
                    for (int j = 0; j < i; j++)
                    {
                        builder.Add(statements[j]);
                    }
                }

                if (builder is not null)
                    builder.Add(newStatement);
            }

            return builder?.MoveToImmutable();
        }

        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            _statementStack.Add(node);
            try
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
                    BoundNodeKind.ReturnStatement => RewriteReturnStatement((BoundReturnStatement)node),
                    BoundNodeKind.ExpressionStatement => RewriteExpressionStatement((BoundExpressionStatement)node),
                    BoundNodeKind.NoOpStatement => node,

                    _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
                };
            }
            finally
            {
                _statementStack.RemoveAt(_statementStack.Count - 1);
            }
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            switch (node.Statements.Length)
            {
                case 0:
                    return BoundNoOpStatement.Instance;

                case 1:
                    return RewriteStatement(node.Statements[0]);

                default:
                    var statements = RewriteStatements(node.Statements);
                    return statements is null ? node : new BoundBlockStatement(statements.Value);
            }
        }

        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var initializer = RewriteExpression(node.Initializer);

            var variable = GetActiveVariable(node.Variable);
            if (variable.IsReadOnly && initializer is BoundLiteralExpression literalExpression)
            {
                var constant = new ConstantSymbol(variable.Name, literalExpression.Value);
                UpdateVariable(variable, constant);

                return BoundNoOpStatement.Instance;
            }

            if (initializer == node.Initializer &&
                variable == node.Variable)
            {
                return node;
            }

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        protected virtual BoundStatement RewriteMethodDeclarationStatement(BoundMethodDeclarationStatement node)
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
            var condition = RewriteExpression(node.Condition);
            var thenStatement = RewriteStatement(node.ThenStatement);
            var elseStatement = (node.ElseStatement == null) ? null : RewriteStatement(node.ElseStatement);

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
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);

            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body, node.ContinueLabel, node.BreakLabel);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var initializationStatement = node.InitializationStatement is null ? null : RewriteStatement(node.InitializationStatement);
            var condition = node.Condition is null ? null : RewriteExpression(node.Condition);
            var updateStatement = node.UpdateStatement is null ? null : RewriteStatement(node.UpdateStatement);
            var body = RewriteStatement(node.Body);

            if (initializationStatement == node.InitializationStatement &&
                condition == node.Condition &&
                updateStatement == node.UpdateStatement &&
                body == node.Body)
            {
                return node;
            }

            return new BoundForStatement(initializationStatement, condition, updateStatement, body, node.ContinueLabel, node.BreakLabel);
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
            var condition = RewriteExpression(node.Condition);
            if (condition is BoundLiteralExpression literalCondition)
            {
                if (node.JumpIfFalse.Equals(literalCondition.Value))
                    return BoundNoOpStatement.Instance;
                else
                {
                    var result = new BoundGotoStatement(node.Label);
                    return RewriteStatement(result);
                }
            }

            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfFalse);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            if (node.Expression is null)
                return node;

            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundReturnStatement(expression);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            // Remove expression statements with no side effects.
            switch (node.Expression)
            {
                case BoundLiteralExpression or
                    BoundVariableExpression or
                    BoundBinaryExpression or
                    BoundExplicitCastExpression:
                case BoundUnaryExpression unaryExpression when !unaryExpression.Op.Kind.IsIncrementOrDecrement():
                    {
                        // Removed expressions may affect variable usage.
                        foreach (var varUsage in VariableUsage.Values)
                            varUsage.Remove(node.Expression);

                        return BoundNoOpStatement.Instance;
                    }
            }

            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(expression);
        }

        #endregion RewriteStatement

        #region RewriteExpression

        protected ImmutableArray<BoundExpression> RewriteExpressions(ImmutableArray<BoundExpression> expressions)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;
            for (int i = 0; i < expressions.Length; i++)
            {
                var oldExpression = expressions[i];
                var newExpression = RewriteExpression(oldExpression);

                if (builder is null && newExpression != oldExpression)
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    builder = ImmutableArray.CreateBuilder<BoundExpression>(expressions.Length);
                    for (int j = 0; j < i; j++)
                        builder.Add(expressions[j]);
                }

                if (builder != null)
                    builder.Add(newExpression);
            }

            if (builder is null)
                return expressions;

            return builder.MoveToImmutable();
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            _expressionStack.Add(node);
            try
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
                    BoundNodeKind.ExplicitCastExpression => RewriteExplicitCastExpression((BoundExplicitCastExpression)node),

                    _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
                };
            }
            finally
            {
                _expressionStack.RemoveAt(_expressionStack.Count - 1);
            }
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            if (TryGetConstant(node.Variable, out var constant))
            {
                var result = new BoundLiteralExpression(constant);
                return RewriteExpression(result);
            }

            var variable = GetActiveVariable(node.Variable);
            VariableUsage[variable].Add(RootExpression ?? node);

            if (variable == node.Variable)
                return node;

            return new BoundVariableExpression(variable);
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var variable = GetActiveVariable(node.Variable);
            ReassignedVariables.Add(variable);

            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression &&
                variable == node.Variable)
            {
                return node;
            }

            return new BoundAssignmentExpression(variable, expression);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            if (node.Operand is BoundVariableExpression variableExpression &&
                node.Op.Kind.IsIncrementOrDecrement())
            {
                // Handle unary expressions that modify the value of the variable.
                var variable = GetActiveVariable(variableExpression.Variable);
                ReassignedVariables.Add(variable);

                // Convert to pre dec or -inc only if it's not part of a statement other than expression statement and not a part of a larger expression.
                if (CurrentStatement is BoundExpressionStatement && RootExpression == node)
                {
                    if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement)
                        node = new BoundUnaryExpression(BoundUnaryOperator.Operators[(BoundUnaryOperatorKind.PreDecrement, node.Op.OperandType)], node.Operand);
                    else if (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
                        node = new BoundUnaryExpression(BoundUnaryOperator.Operators[(BoundUnaryOperatorKind.PreIncrement, node.Op.OperandType)], node.Operand);
                }
            }

            var operand = RewriteExpression(node.Operand);
            if (operand is BoundLiteralExpression literalOperand)
            {
                var value = LiteralEvaluator.EvaluateUnaryExpression(node.Op, literalOperand.Value);
                if (value == literalOperand.Value)
                    return literalOperand;

                var result = new BoundLiteralExpression(value);
                return RewriteExpression(result);
            }

            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            if (left is BoundLiteralExpression literalLeft && right is BoundLiteralExpression literalRight)
            {
                var value = LiteralEvaluator.EvaluateBinaryExpression(node.Op, literalLeft.Value, literalRight.Value);

                BoundLiteralExpression result;
                if (value == literalLeft.Value)
                    result = literalLeft;
                else if (value == literalRight.Value)
                    result = literalRight;
                else
                    result = new BoundLiteralExpression(value);

                return RewriteExpression(result);
            }

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            var arguments = RewriteExpressions(node.Arguments);
            if (arguments == node.Arguments)
                return node;

            return new BoundCallExpression(node.Method, arguments);
        }

        protected virtual BoundExpression RewriteExplicitCastExpression(BoundExplicitCastExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression is BoundLiteralExpression literalExpression)
            {
                var value = LiteralEvaluator.EvaluateExplicitCastExpression(node.Type, literalExpression.Value);
                var result = new BoundLiteralExpression(value);

                return RewriteExpression(result);
            }

            if (expression == node.Expression)
                return node;

            return new BoundExplicitCastExpression(node.Type, expression);
        }

        #endregion RewriteExpression

        #region Helpers

        /// <summary>
        /// Returns the same variable OR the variable that has replaced the given one.
        /// </summary>
        protected VariableSymbol GetActiveVariable(VariableSymbol variable)
        {
            while (UpdatedVariables.TryGetValue(variable, out var updatedVariable))
                variable = updatedVariable;

            if (!VariableUsage.ContainsKey(variable))
                VariableUsage.Add(variable, new());

            return variable;
        }

        protected bool TryGetConstant(VariableSymbol variable, [NotNullWhen(true)] out ConstantSymbol? constant)
        {
            if (GetActiveVariable(variable) is ConstantSymbol result)
            {
                constant = result;
                return true;
            }
            else
            {
                constant = null;
                return false;
            }
        }

        protected void UpdateVariable(VariableSymbol oldVar, VariableSymbol newVar)
        {
            var activeOld = GetActiveVariable(oldVar);

            VariableUsage.Add(newVar, VariableUsage[activeOld]);
            VariableUsage.Remove(activeOld);

            if (ReassignedVariables.Remove(activeOld))
                ReassignedVariables.Add(newVar);

            UpdatedVariables.Add(activeOld, newVar);
        }

        #endregion Helpers
    }
}