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

using System.Collections.Immutable;
using VSharp.Symbols;
using VSharp.Syntax;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Binding
{
    internal sealed class BoundUnaryOperator
    {
        private BoundUnaryOperator(SyntaxKind syntaxKind, UnaryType unaryType, BoundUnaryOperatorKind kind, TypeSymbol operandType)
            : this(syntaxKind, unaryType, kind, operandType, operandType)
        {
        }

        private BoundUnaryOperator(SyntaxKind syntaxKind, UnaryType unaryType, BoundUnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
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
        public TypeSymbol OperandType { get; }
        public TypeSymbol ResultType { get; }

        #endregion Properties

        #region Statics

        public static BoundUnaryOperator? Bind(SyntaxKind syntaxKind, UnaryType unaryType, TypeSymbol operandType)
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

            if (operandType != TypeSymbol.Object)
                return Bind(syntaxKind, unaryType, operandType.BaseType);

            return null;
        }

        private static readonly BoundUnaryOperator[] _operators =
        {
            // Bool
            new BoundUnaryOperator(SyntaxKind.BangToken, UnaryType.Pre, BoundUnaryOperatorKind.LogicalNegation, Types.Boolean),

            // Int
            new BoundUnaryOperator(SyntaxKind.PlusToken, UnaryType.Pre, BoundUnaryOperatorKind.Identity, Types.Int),
            new BoundUnaryOperator(SyntaxKind.MinusToken, UnaryType.Pre, BoundUnaryOperatorKind.Negation, Types.Int),

            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreIncrement, Types.Int),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreDecrement, Types.Int),
            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Post, BoundUnaryOperatorKind.PostIncrement, Types.Int),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Post, BoundUnaryOperatorKind.PostDecrement, Types.Int),

            new BoundUnaryOperator(SyntaxKind.TildeToken, UnaryType.Pre, BoundUnaryOperatorKind.OnesComplement, Types.Int),

            // Float
            new BoundUnaryOperator(SyntaxKind.PlusToken, UnaryType.Pre, BoundUnaryOperatorKind.Identity, Types.Float),
            new BoundUnaryOperator(SyntaxKind.MinusToken, UnaryType.Pre, BoundUnaryOperatorKind.Negation, Types.Float),

            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreIncrement, Types.Float),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Pre, BoundUnaryOperatorKind.PreDecrement, Types.Float),
            new BoundUnaryOperator(SyntaxKind.PlusPlusToken, UnaryType.Post, BoundUnaryOperatorKind.PostIncrement, Types.Float),
            new BoundUnaryOperator(SyntaxKind.MinusMinusToken, UnaryType.Post, BoundUnaryOperatorKind.PostDecrement, Types.Float),
        };

        public static ImmutableDictionary<(BoundUnaryOperatorKind, TypeSymbol), BoundUnaryOperator> Operators = _operators.ToImmutableDictionary(o => (o.Kind, o.OperandType));

        #endregion Statics
    }
}