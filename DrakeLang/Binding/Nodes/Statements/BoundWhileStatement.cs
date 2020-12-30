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
    internal sealed class BoundWhileStatement : BoundLoopStatement
    {
        public BoundWhileStatement(BoundExpression condition,
                                   BoundStatement body,
                                   LabelSymbol continueLabel,
                                   LabelSymbol breakLabel)
            : base(body, continueLabel, breakLabel)
        {
            Condition = condition;
        }

        #region Properties

        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

        public BoundExpression Condition { get; }

        #endregion Properties

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
        }
    }
}