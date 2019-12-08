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
using PHPSharp.Symbols;
using System;
using System.Collections.Generic;

namespace PHPSharp
{
    internal sealed class Evaluator : IEvaluator
    {
        private readonly BoundBlockStatement _root;
        private readonly Dictionary<VariableSymbol, object> _variables;

        private object? _lastValue;

        public Evaluator(BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        #region IEvaluator

        public object? Evaluate()
        {
            // Create label-index mapping for goto statements.
            Dictionary<LabelSymbol, int> labelToIndex = new Dictionary<LabelSymbol, int>();
            for (int i = 0; i < _root.Statements.Length; i++)
            {
                if (_root.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            // Evaluate program.
            int index = 0;
            while (index < _root.Statements.Length)
            {
                BoundStatement s = _root.Statements[index];
                switch (s.Kind)
                {
                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)s);
                        index++;
                        break;

                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement)s);
                        index++;
                        break;

                    case BoundNodeKind.LabelStatement:
                        // do nothing.
                        index++;
                        break;

                    case BoundNodeKind.GotoStatement:
                        BoundGotoStatement gotoStatement = (BoundGotoStatement)s;
                        index = labelToIndex[gotoStatement.Label];
                        break;

                    case BoundNodeKind.ConditionalGotoStatement:
                        BoundConditionalGotoStatement conGotoStatement = (BoundConditionalGotoStatement)s;
                        if (EvaluateConditionalGotoStatement(conGotoStatement))
                            index = labelToIndex[conGotoStatement.Label];
                        else
                            index++;
                        break;

                    default:
                        throw new Exception($"Unexpected node '{s.Kind}'.");
                }
            }

            return _lastValue;
        }

        #endregion IEvaluator

        #region EvaluateStatement

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            object value = EvaluateExpression(node.Initializer);
            _variables[node.Variable] = value;

            _lastValue = value;
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            _lastValue = EvaluateExpression(node.Expression);
        }

        private bool EvaluateConditionalGotoStatement(BoundConditionalGotoStatement s)
        {
            bool condition = (bool)EvaluateExpression(s.Condition);

            return condition ^ s.JumpIfFalse;
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
                BoundNodeKind.CallExpression => EvaluateCallExpression((BoundCallExpression)node),

                _ => throw new Exception($"Unexpected node '{node.Kind}'."),
            };
        }

        private static object EvaluateLiteralExpression(BoundLiteralExpression node)
        {
            return node.Value;
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
            // Pre- and post increment/decrement.
            if (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement || node.Op.Kind == BoundUnaryOperatorKind.PreIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;

                if (node.Type == TypeSymbol.Int)
                    _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);
                else
                    _variables[variableExpression.Variable] = (double)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);

                return _variables[variableExpression.Variable];
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;

                object value = _variables[variableExpression.Variable];
                if (node.Type == TypeSymbol.Int)
                {
                    _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                }
                else
                {
                    _variables[variableExpression.Variable] = (double)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                }

                return value;
            }

            // Other unary operations.
            object operant = EvaluateExpression(node.Operand);
            switch (node.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return operant;

                case BoundUnaryOperatorKind.Negation:
                    if (node.Type == TypeSymbol.Int)
                        return -(int)operant;
                    else
                        return -(double)operant;

                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operant;

                case BoundUnaryOperatorKind.OnesComplement:
                    return ~(int)operant;

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
                    if (node.Type == TypeSymbol.Int)
                        return (int)left + (int)right;
                    if (node.Type == TypeSymbol.Float)
                        return (double)left + (double)right;
                    if (node.Left.Type == TypeSymbol.String)
                        return (string)left + right;
                    else
                        return left + (string)right;

                case BoundBinaryOperatorKind.Subtraction:
                    if (node.Type == TypeSymbol.Int)
                        return (int)left - (int)right;
                    else
                        return (double)left - (double)right;

                case BoundBinaryOperatorKind.Multiplication:
                    if (node.Type == TypeSymbol.Int)
                        return (int)left * (int)right;
                    else
                        return (double)left * (double)right;

                case BoundBinaryOperatorKind.Division:
                    if (node.Type == TypeSymbol.Int)
                    {
                        if ((int)right == 0) return "ERR: Can't divide by zero";
                        return (int)left / (int)right;
                    }
                    else
                    {
                        if ((double)right == 0) return "ERR: Can't divide by zero";
                        return (double)left / (double)right;
                    }

                case BoundBinaryOperatorKind.Modulo:
                    return (int)left % (int)right;

                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (node.Type == TypeSymbol.Int)
                        return (int)left & (int)right;
                    else
                        return (bool)left & (bool)right;

                case BoundBinaryOperatorKind.BitwiseOr:
                    if (node.Type == TypeSymbol.Int)
                        return (int)left | (int)right;
                    else
                        return (bool)left | (bool)right;

                case BoundBinaryOperatorKind.BitwiseXor:
                    if (node.Type == TypeSymbol.Int)
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
                    if (node.Left.Type == TypeSymbol.Int)
                        return (int)left < (int)right;
                    else
                        return (double)left < (double)right;

                case BoundBinaryOperatorKind.LessThanOrEquals:
                    if (node.Left.Type == TypeSymbol.Int)
                        return (int)left <= (int)right;
                    else
                        return (double)left <= (double)right;

                case BoundBinaryOperatorKind.GreaterThan:
                    if (node.Left.Type == TypeSymbol.Int)
                        return (int)left > (int)right;
                    else
                        return (double)left > (double)right;

                case BoundBinaryOperatorKind.GreaterThanOrEquals:
                    if (node.Left.Type == TypeSymbol.Int)
                        return (int)left >= (int)right;
                    else
                        return (double)left >= (double)right;

                default:
                    throw new Exception($"Unexpected binary operator '{node.Op.Kind}'.");
            }
        }

        private object EvaluateCallExpression(BoundCallExpression node)
        {
            if (node.Method == BuiltinMethods.Input)
            {
                return Console.ReadLine();
            }
            else if (node.Method == BuiltinMethods.Print)
            {
                string message = (string)EvaluateExpression(node.Arguments[0]);
                Console.WriteLine(message);
                return 0; // cannot return null due to nullable reference types being enabled.
            }
            else throw new Exception($"Unexpected method '{node.Method}'.");
        }

        #endregion EvaluateExpression
    }
}