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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VSharp.Symbols;

namespace VSharp.Binding
{
    internal sealed class BoundArrayInitializationExpression : BoundExpression
    {
        public BoundArrayInitializationExpression(TypeSymbol type, BoundExpression sizeExpression, ImmutableArray<BoundExpression> initializer)
        {
            if (type.GetGenericTypeDefinition() != SystemSymbols.Types.Array)
                throw new ArgumentException($"Array type must be of type '{SystemSymbols.Types.Array}'.");

            Type = type;
            SizeExpression = sizeExpression;
            Initializer = initializer;
        }

        public override TypeSymbol Type { get; }
        public BoundExpression SizeExpression { get; }
        public ImmutableArray<BoundExpression> Initializer { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayInitializationExpression;

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return SizeExpression;
            foreach (var item in Initializer)
            {
                yield return item;
            }
        }
    }
}