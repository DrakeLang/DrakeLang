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

        private object? _lastValue;

        public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        #region Methods

        public object? Evaluate()
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

                case BoundNodeKind.VariableDeclarationStatement:
                    EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)node);
                    break;

                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement)node);
                    break;

                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)node);
                    break;

                case BoundNodeKind.ForStatement:
                    EvaluateForStatement((BoundForStatement)node);
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

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            object value = EvaluateExpression(node.Initializer);
            _variables[node.Variable] = value;

            _lastValue = value;
        }

        private void EvaluateIfStatement(BoundIfStatement node)
        {
            bool condition = (bool)EvaluateExpression(node.Condition);
            if (condition)
                EvaluateStatement(node.ThenStatement);
            else if (node.ElseStatement != null)
                EvaluateStatement(node.ElseStatement);
        }

        private void EvaluateWhileStatement(BoundWhileStatement node)
        {
            bool condition() => (bool)EvaluateExpression(node.Condition);
            while (condition())
                EvaluateStatement(node.Body);
        }

        private void EvaluateForStatement(BoundForStatement node)
        {
            EvaluateStatement(node.InitializationStatement);

            bool condition() => (bool)EvaluateExpression(node.Condition);
            while (condition())
            {
                EvaluateStatement(node.Body);
                EvaluateStatement(node.UpdateStatement);
            }
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            _lastValue = EvaluateExpression(node.Expression);
        }

        #endregion EvaluateStatement

        #region EvaluateExpression

        private object EvaluateExpression(BoundExpression node)
        {
            return node.Kind switch
            {
                BoundNodeKind.LiteralExpression => EvaluateLiteralExpression((BoundLiteralExpression)node),
                BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node),

                _ => throw new Exception($"Unexpected node '{node.Kind}'."),
            };
        }

        private static object EvaluateLiteralExpression(BoundLiteralExpression node)
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
            if (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement || node.Op.Kind == BoundUnaryOperatorKind.PreIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;
                _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);
                return _variables[variableExpression.Variable];
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;
                int value = (int)_variables[variableExpression.Variable];

                _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                return value;
            }

            object operant = EvaluateExpression(node.Operand);
            return node.Op.Kind switch
            {
                BoundUnaryOperatorKind.Identity => (int)operant,
                BoundUnaryOperatorKind.Negation => -(int)operant,
                BoundUnaryOperatorKind.LogicalNegation => !(bool)operant,
                BoundUnaryOperatorKind.OnesComplement => ~(int)operant,

                _ => throw new Exception($"Unexpected unary operator '{node.Op.Kind}'."),
            };
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
                    if ((int)right == 0)
                        return "ERR: Can't divide by zero";

                    return (int)left / (int)right;

                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (node.Type == typeof(int))
                        return (int)left & (int)right;
                    else
                        return (bool)left & (bool)right;

                case BoundBinaryOperatorKind.BitwiseOr:
                    if (node.Type == typeof(int))
                        return (int)left | (int)right;
                    else
                        return (bool)left | (bool)right;

                case BoundBinaryOperatorKind.BitwiseXor:
                    if (node.Type == typeof(int))
                        return (int)left ^ (int)right;
                    else
                        return (bool)left ^ (bool)right;

                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)left && (bool)right;

                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool)left || (bool)right;

                case BoundBinaryOperatorKind.Equals:
                    return Equals(left, right);

                case BoundBinaryOperatorKind.NotEquals:
                    return !Equals(left, right);

                case BoundBinaryOperatorKind.LessThan:
                    return (int)left < (int)right;

                case BoundBinaryOperatorKind.LessThanOrEquals:
                    return (int)left <= (int)right;

                case BoundBinaryOperatorKind.GreaterThan:
                    return (int)left > (int)right;

                case BoundBinaryOperatorKind.GreaterThanOrEquals:
                    return (int)left >= (int)right;

                default:
                    throw new Exception($"Unexpected binary operator '{node.Op.Kind}'.");
            }
        }

        #endregion EvaluateExpression
    }
}