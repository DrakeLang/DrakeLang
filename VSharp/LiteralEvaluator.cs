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
using System.Globalization;
using VSharp.Binding;
using VSharp.Symbols;

namespace VSharp
{
    internal sealed class LiteralEvaluator
    {
        public static object EvaluateUnaryExpression(BoundUnaryOperator op, object operand)
        {
            switch (op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return operand;

                case BoundUnaryOperatorKind.Negation:
                    if (op.ResultType == TypeSymbol.Int)
                        return -(int)operand;
                    else
                        return -(double)operand;

                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operand;

                case BoundUnaryOperatorKind.OnesComplement:
                    return ~(int)operand;

                default:
                    throw new Exception($"Unexpected unary operator '{op.Kind}'.");
            }
        }

        public static object EvaluateBinaryExpression(BoundBinaryOperator op, object left, object right)
        {
            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (op.ResultType == TypeSymbol.Int)
                        return (int)left + (int)right;
                    if (op.ResultType == TypeSymbol.Float)
                        return (double)left + (double)right;
                    if (op.LeftType == TypeSymbol.String)
                        return (string)left + right;
                    else
                        return left + (string)right;

                case BoundBinaryOperatorKind.Subtraction:
                    if (op.ResultType == TypeSymbol.Int)
                        return (int)left - (int)right;
                    else
                        return (double)left - (double)right;

                case BoundBinaryOperatorKind.Multiplication:
                    if (op.ResultType == TypeSymbol.Int)
                        return (int)left * (int)right;
                    else
                        return (double)left * (double)right;

                case BoundBinaryOperatorKind.Division:
                    if (op.ResultType == TypeSymbol.Int)
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
                    if (op.ResultType == TypeSymbol.Int)
                        return (int)left & (int)right;
                    else
                        return (bool)left & (bool)right;

                case BoundBinaryOperatorKind.BitwiseOr:
                    if (op.ResultType == TypeSymbol.Int)
                        return (int)left | (int)right;
                    else
                        return (bool)left | (bool)right;

                case BoundBinaryOperatorKind.BitwiseXor:
                    if (op.ResultType == TypeSymbol.Int)
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
                    if (op.LeftType == TypeSymbol.Int)
                        return (int)left < (int)right;
                    else
                        return (double)left < (double)right;

                case BoundBinaryOperatorKind.LessThanOrEquals:
                    if (op.LeftType == TypeSymbol.Int)
                        return (int)left <= (int)right;
                    else
                        return (double)left <= (double)right;

                case BoundBinaryOperatorKind.GreaterThan:
                    if (op.LeftType == TypeSymbol.Int)
                        return (int)left > (int)right;
                    else
                        return (double)left > (double)right;

                case BoundBinaryOperatorKind.GreaterThanOrEquals:
                    if (op.LeftType == TypeSymbol.Int)
                        return (int)left >= (int)right;
                    else
                        return (double)left >= (double)right;

                default:
                    throw new Exception($"Unexpected binary operator '{op.Kind}'.");
            }
        }

        public static object EvaluateExplicitCastExpression(TypeSymbol type, object value)
        {
            if (type == TypeSymbol.Boolean)
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            else if (type == TypeSymbol.Float)
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            else if (type == TypeSymbol.Int)
            {
                return Convert.ToInt16(value, CultureInfo.InvariantCulture);
            }
            else if (type == TypeSymbol.String)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            else throw new Exception($"Unexpected type '{type}'.");
        }
    }
}