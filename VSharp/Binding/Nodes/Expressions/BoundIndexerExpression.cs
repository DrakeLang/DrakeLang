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
using VSharp.Symbols;

namespace VSharp.Binding
{
    internal sealed class BoundIndexerExpression : BoundExpression
    {
        public BoundIndexerExpression(BoundExpression operand, BoundExpression parameter)
        {
            Operand = operand;
            if (operand.Type.Indexer is null)
                throw new ArgumentException($"Type of operand '{operand.Type}' does not have an indexer.");

            Parameter = parameter;
        }

        public override TypeSymbol Type => Indexer.ReturnType;
        public override BoundNodeKind Kind => BoundNodeKind.IndexerExpression;

        public BoundExpression Operand { get; }
        public BoundExpression Parameter { get; }

        public IndexerSymbol Indexer => Operand.Type.Indexer!;

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Operand;
            yield return Parameter;
        }
    }
}