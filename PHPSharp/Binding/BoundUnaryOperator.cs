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
    internal class BoundUnaryOperator
    {
        private BoundUnaryOperator(SyntaxKind syntaxKind, UnaryType unaryType, BoundUnaryOperatorKind kind, Type operandType)
            : this(syntaxKind, unaryType, kind, operandType, operandType)
        {
        }

        private BoundUnaryOperator(SyntaxKind syntaxKind, UnaryType unaryType, BoundUnaryOperatorKind kind, Type operandType, Type resultType)
        {
            SyntaxKind = syntaxKind;
            UnaryType = unaryType;
            Kind = kind;
            OperandType = operandType;
            ResultType = resultType;
        }

        #region Properties

        public SyntaxKind SyntaxKind { get; }
        public UnaryType UnaryType { get; }
        public BoundUnaryOperatorKind Kind { get; }
        public Type OperandType { get; }
        public Type ResultType { get; }

        #endregion Properties

        #region Public statics

        public static BoundUnaryOperator? Bind(SyntaxKind syntaxKind, UnaryType unaryType, Type operandType)
        {
            foreach (BoundUnaryOperator op in _operators)
            {
                if (op.SyntaxKind == syntaxKind &&
                    op.UnaryType == unaryType &&
                    op.OperandType == operandType)
                {
                    return op;
                }
            }

            return null;
        }

        #endregion Public statics

        #region Private statics

        private static readonly BoundUnaryOperator[] _operators =
        {
            new BoundUnaryOperator(SyntaxKind.BangToken, UnaryType.Pre, BoundUnaryOperatorKind.LogicalNegation, typeof(bool)),

            new BoundUnaryOperator(SyntaxKind.PlusToken, UnaryType.Pre, BoundUnaryOperatorKind.Identity, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.MinusToken, UnaryType.Pre, BoundUnaryOperatorKind.Negation, typeof(int)),

            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreIncrement, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreDecrement, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Post, BoundUnaryOperatorKind.PostIncrement, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Post, BoundUnaryOperatorKind.PostDecrement, typeof(int)),

            new BoundUnaryOperator(SyntaxKind.TildeToken, UnaryType.Pre, BoundUnaryOperatorKind.OnesComplement, typeof(int)),
        };

        #endregion Private statics
    }
}