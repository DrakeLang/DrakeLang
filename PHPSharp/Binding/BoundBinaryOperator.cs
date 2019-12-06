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

using PHPSharp.Syntax;
using System;

namespace PHPSharp.Binding
{
    internal class BoundBinaryOperator
    {
        private BoundBinaryOperator(SyntaxKind syntaxKind, BoundBinaryOperatorKind kind, Type type)
            : this(syntaxKind, kind, type, type, type)
        {
        }

        private BoundBinaryOperator(SyntaxKind syntaxKind, BoundBinaryOperatorKind kind, Type inputType, Type resultType)
           : this(syntaxKind, kind, inputType, inputType, resultType)
        {
        }

        private BoundBinaryOperator(SyntaxKind syntaxKind, BoundBinaryOperatorKind kind, Type leftType, Type rightType, Type resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            LeftType = leftType;
            RightType = rightType;
            ResultType = resultType;
        }

        #region Properties

        public SyntaxKind SyntaxKind { get; }
        public BoundBinaryOperatorKind Kind { get; }
        public Type LeftType { get; }
        public Type RightType { get; }
        public Type ResultType { get; }

        #endregion Properties

        #region Public statics

        public static BoundBinaryOperator? Bind(SyntaxKind syntaxKind, Type leftType, Type rightType)
        {
            foreach (BoundBinaryOperator op in _operators)
            {
                if (op.SyntaxKind == syntaxKind && op.LeftType == leftType && op.RightType == rightType)
                    return op;
            }

            return null;
        }

        public static BoundBinaryOperator? Bind(BoundBinaryOperatorKind operatorKind, Type leftType, Type rightType)
        {
            foreach (BoundBinaryOperator op in _operators)
            {
                if (op.Kind == operatorKind && op.LeftType == leftType && op.RightType == rightType)
                    return op;
            }

            return null;
        }

        #endregion Public statics

        #region Private statics

        private static readonly BoundBinaryOperator[] _operators =
        {
            // Integer
            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, typeof(int)),

            new BoundBinaryOperator(SyntaxKind.AmpersandToken, BoundBinaryOperatorKind.BitwiseAnd, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.PipeToken, BoundBinaryOperatorKind.BitwiseOr, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.HatToken, BoundBinaryOperatorKind.BitwiseXor, typeof(int)),

            new BoundBinaryOperator(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equals, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.NotEquals , typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.LessThan, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessThanOrEquals, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.GreaterThan, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterThanOrEquals, typeof(int), typeof(bool)),

            // Boolean
            new BoundBinaryOperator(SyntaxKind.AmpersandToken, BoundBinaryOperatorKind.BitwiseAnd, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.PipeToken, BoundBinaryOperatorKind.BitwiseOr, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.HatToken, BoundBinaryOperatorKind.BitwiseXor, typeof(bool)),

            new BoundBinaryOperator(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equals, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.NotEquals, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.LessThan, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessThanOrEquals, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.GreaterThan, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterThanOrEquals, typeof(bool)),
        };

        #endregion Private statics
    }
}