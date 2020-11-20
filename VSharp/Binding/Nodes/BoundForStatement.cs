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
using VSharp.Symbols;

namespace VSharp.Binding
{
    internal sealed class BoundForStatement : BoundLoopStatement
    {
        public BoundForStatement(BoundStatement? initializationStatement,
                                 BoundExpression? condition,
                                 BoundStatement? updateStatement,
                                 BoundStatement body,
                                 LabelSymbol continueLabel,
                                 LabelSymbol breakLabel)
            : base(body, continueLabel, breakLabel)
        {
            InitializationStatement = initializationStatement;
            Condition = condition;
            UpdateStatement = updateStatement;
        }

        #region Properties

        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

        public BoundStatement? InitializationStatement { get; }
        public BoundExpression? Condition { get; }
        public BoundStatement? UpdateStatement { get; }

        #endregion Properties

        public override IEnumerable<BoundNode> GetChildren()
        {
            if (InitializationStatement is not null) yield return InitializationStatement;
            if (Condition is not null) yield return Condition;
            if (UpdateStatement is not null) yield return UpdateStatement;
            yield return Body;
        }
    }
}