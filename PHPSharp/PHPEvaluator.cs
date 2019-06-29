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
    internal class PHPEvaluator
    {
        private readonly BoundExpression _root;
        private readonly Dictionary<VariableSymbol, object> _variables;

        public PHPEvaluator(BoundExpression root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        #region Methods

        public string Evaluate()
        {
            string result = EvaluateExpression(_root);
            return string.Format("<?php {0} ?>", result);
        }

        private string EvaluateExpression(BoundExpression node)
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

        #endregion Methods

        #region Private methods

        private string EvaluateLiteralExpression(BoundLiteralExpression node)
        {
            if (node.Type != typeof(bool))
                return node.Value.ToString();
            else
            {
                bool boolVal = (bool)node.Value;
                return boolVal ? "true" : "false";
            }
        }

        private string EvaluateVariableExpression(BoundVariableExpression node)
        {
            return "$" + node.Variable.Name;
        }

        private string EvaluateAssignmentExpression(BoundAssignmentExpression node)
        {
            return "$" + node.Variable.Name + "=" + EvaluateExpression(node.Expression);
        }

        private string EvaluateUnaryExpression(BoundUnaryExpression node)
        {
            switch (node.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return EvaluateExpression(node.Operand);

                case BoundUnaryOperatorKind.Negation:
                    return "-" + EvaluateExpression(node.Operand);

                case BoundUnaryOperatorKind.LogicalNegation:
                    return "!" + EvaluateExpression(node.Operand);

                default:
                    throw new Exception($"Unexpected unary operator '{node.Op.Kind}'.");
            }
        }

        private string EvaluateBinaryExpression(BoundBinaryExpression node)
        {
            string left = "(" + EvaluateExpression(node.Left);
            string right = EvaluateExpression(node.Right) + ")";

            switch (node.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return left + "+" + right;

                case BoundBinaryOperatorKind.Subtraction:
                    return left + "-" + right;

                case BoundBinaryOperatorKind.Multiplication:
                    return left + "*" + right;

                case BoundBinaryOperatorKind.Division:
                    return left + "/" + right;

                case BoundBinaryOperatorKind.LogicalAnd:
                    return left + "&&" + right;

                case BoundBinaryOperatorKind.LogicalOr:
                    return left + "||" + right;

                case BoundBinaryOperatorKind.Equals:
                    return left + "==" + right;

                case BoundBinaryOperatorKind.NotEquals:
                    return left + "!=" + right;

                default:
                    throw new Exception($"Unexpected binary operator '{node.Op.Kind}'.");
            }
        }

        #endregion Private methods
    }
}