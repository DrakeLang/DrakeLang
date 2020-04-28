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

using PHPSharp.Symbols;
using System;
using System.Collections.Generic;

namespace PHPSharp.Binding
{
    internal class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(BoundUnaryOperator op, BoundExpression operand)
        {
            Op = op;
            Operand = operand;
        }

        #region Properties

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Op.ResultType;
        public BoundUnaryOperator Op { get; }
        public BoundExpression Operand { get; }

        #endregion Properties

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Operand;
        }
    }
}