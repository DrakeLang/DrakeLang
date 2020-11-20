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

using System.Collections.Generic;
using System.Linq;
using VSharp.Symbols;
using VSharp.Utils;

namespace VSharp.Binding
{
    internal class BoundLiteralExpression : BoundExpression
    {
        public static readonly BoundLiteralExpression True = new BoundLiteralExpression(true);
        public static readonly BoundLiteralExpression False = new BoundLiteralExpression(false);

        public BoundLiteralExpression(object value)
        {
            Value = value;
            Type = TypeSymbolUtil.FromValue(value);
        }

        public BoundLiteralExpression(ConstantSymbol constant)
        {
            Value = constant.Value;
            Type = constant.Type;
        }

        #region Properties

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public object Value { get; }

        #endregion Properties

        public override IEnumerable<BoundNode> GetChildren()
        {
            return Enumerable.Empty<BoundNode>();
        }
    }
}