﻿//------------------------------------------------------------------------------
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

using DrakeLang.Symbols;
using System.Collections.Immutable;

namespace DrakeLang.Binding
{
    internal abstract class BoundLoopStatement : BoundStatement
    {
        public BoundLoopStatement(ImmutableArray<BoundStatement> body, LabelSymbol continueLabel, LabelSymbol breakLabel)
        {
            Body = body;
            ContinueLabel = continueLabel;
            BreakLabel = breakLabel;
        }

        public ImmutableArray<BoundStatement> Body { get; }
        public LabelSymbol ContinueLabel { get; }
        public LabelSymbol BreakLabel { get; }
    }
}