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

using PHPSharp.Binding;
using System;
using System.Collections.Generic;

namespace PHPSharp
{
    internal class Evaluator
    {
        private readonly BoundStatement _root;
        private readonly Dictionary<VariableSymbol, object> _variables;

        private object _lastValue;

        public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        #region Methods

        public object Evaluate()
        {
            EvaluateStatement(_root);
            return _lastValue;
        }

        #endregion Methods

        #region EvaluateStatement

        private void EvaluateStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement)node);
                    break;

                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)node);
                    break;

                default:
                    throw new Exception($"Unexpected node '{node.Kind}'.");
            }
        }

        private void EvaluateBlockStatement(BoundBlockStatement node)
        {
            foreach (BoundStatement statement in node.Statements)
                EvaluateStatement(statement);
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            _lastValue = EvaluateExpression(node.Expression);
        }

        #endregion EvaluateStatement

        #region EvaluateExpression

        private object EvaluateExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    return EvaluateLiteralExpression((BoundLiteralExpression)node);

                case BoundNodeKind.VariableExpression:
                    return EvaluateVariableExpression((BoundVariableExpression)node);

                case BoundNodeKind.AssignmentExpression:
                    return EvaluateAssignmentExpression((BoundAssignmentExpression)node);

                case BoundNodeKind.UnaryExpression:
                    return EvaluateUnaryExpression((BoundUnaryExpression)node);

                case BoundNodeKind.BinaryExpression:
                    return EvaluateBinaryExpression((BoundBinaryExpression)node);

                default:
                    throw new Exception($"Unexpected node '{node.Kind}'.");
            }
        }

        private object EvaluateLiteralExpression(BoundLiteralExpression node)
        {
            if (node.Type != typeof(bool))
                return node.Value;
            else
            {
                return (bool)node.Value;
            }
        }

        private object EvaluateVariableExpression(BoundVariableExpression node)
        {
            return _variables[node.Variable];
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression node)
        {
            object value = EvaluateExpression(node.Expression);
            _variables[node.Variable] = value;

            return value;
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression node)
        {
            object operant = EvaluateExpression(node.Operand);
            switch (node.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return (int)operant;

                case BoundUnaryOperatorKind.Negation:
                    return -(int)operant;

                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operant;

                default:
                    throw new Exception($"Unexpected unary operator '{node.Op.Kind}'.");
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression node)
        {
            object left = EvaluateExpression(node.Left);
            object right = EvaluateExpression(node.Right);

            switch (node.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return (int)left + (int)right;

                case BoundBinaryOperatorKind.Subtraction:
                    return (int)left - (int)right;

                case BoundBinaryOperatorKind.Multiplication:
                    return (int)left * (int)right;

                case BoundBinaryOperatorKind.Division:
                    return (int)left / (int)right;

                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)left && (bool)right;

                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool)left || (bool)right;

                case BoundBinaryOperatorKind.Equals:
                    return Equals(left, right);

                case BoundBinaryOperatorKind.NotEquals:
                    return !Equals(left, right);

                default:
                    throw new Exception($"Unexpected binary operator '{node.Op.Kind}'.");
            }
        }

        #endregion EvaluateExpression
    }
}