//------------------------------------------------------------------------------
// DrakeLang - Viv's C#-esque sandbox.
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

using System.Collections.Generic;
using DrakeLang.Symbols;

namespace DrakeLang.Binding
{
    internal sealed class BoundExplicitCastExpression : BoundExpression
    {
        public BoundExplicitCastExpression(TypeSymbol type, BoundExpression expression)
        {
            Type = type;
            Expression = expression;
        }

        public override TypeSymbol Type { get; }
        public BoundExpression Expression { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ExplicitCastExpression;

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Expression;
        }
    }
}