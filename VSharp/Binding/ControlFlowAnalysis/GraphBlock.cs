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

namespace VSharp.Binding.CFA
{
    internal sealed class GraphBlock
    {
        public GraphBlock()
        {
        }

        public GraphBlock(bool isStart)
        {
            IsStart = isStart;
        }

        #region Properties

        public bool? IsStart { get; }
        public bool? IsEnd => !IsStart;

        public HashSet<GraphBranch> Incoming { get; } = new HashSet<GraphBranch>();
        public HashSet<GraphBranch> Outgoing { get; } = new HashSet<GraphBranch>();

        public List<BoundStatement> Statements { get; } = new List<BoundStatement>();

        #endregion Properties

        #region Method

        public override string ToString()
        {
            if (IsStart is true)
                return "<start>";
            else if (IsEnd is true)
                return "<end>";
            else
                return Statements.Select(s => s.ToFriendlyString()).Aggregate((s1, s2) => s1 + "\\l" + s2) + "\\l";
        }

        #endregion Method
    }
}