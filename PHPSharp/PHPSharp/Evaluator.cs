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
// along with this program.  If not, see https://www.gnu.org/licenses/.
//------------------------------------------------------------------------------

using PHPSharp.Binding;
using System;

namespace PHPSharp
{
    public class Evaluator
    {
        private readonly BoundExpression _root;

        public Evaluator(BoundExpression root)
        {
            _root = root;
        }

        #region Methods

        public string Evaluate()
        {
            string result = Evaluate(_root);
            return string.Format("<?php {0} ?>", result);
        }

        private string Evaluate(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    return EvaluateLiteral((BoundLiteralExpression)node);

                case BoundNodeKind.BinaryExpression:
                    return EvaluateBinary((BoundBinaryExpression)node);

                case BoundNodeKind.UnaryExpression:
                    return EvaluateUnary((BoundUnaryExpression)node);

                default:
                    throw new Exception($"Unexpected node '{node.Kind}'.");
            }
        }

        #endregion Methods

        #region Private methods

        private string EvaluateLiteral(BoundLiteralExpression node)
        {
            if (node.Type != typeof(bool))
                return node.Value.ToString();
            else
            {
                bool boolVal = (bool)node.Value;
                return boolVal ? "true" : "false";
            }
        }

        private string EvaluateBinary(BoundBinaryExpression node)
        {
            string left = "(" + Evaluate(node.Left);
            string right = Evaluate(node.Right) + ")";

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

        private string EvaluateUnary(BoundUnaryExpression node)
        {
            switch (node.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return Evaluate(node.Operand);

                case BoundUnaryOperatorKind.Negation:
                    return "-" + Evaluate(node.Operand);

                case BoundUnaryOperatorKind.LogicalNegation:
                    return "!" + Evaluate(node.Operand);

                default:
                    throw new Exception($"Unexpected unary operator '{node.Op.Kind}'.");
            }
        }

        #endregion Private methods
    }
}