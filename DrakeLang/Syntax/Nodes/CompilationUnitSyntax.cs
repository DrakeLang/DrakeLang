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

using System.Collections.Generic;
using System.Collections.Immutable;

namespace DrakeLang.Syntax
{
    /// <summary>
    /// Root object of a compilation.
    /// </summary>
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        internal CompilationUnitSyntax(ImmutableArray<StatementSyntax> statements, SyntaxToken endOfFileToken)
        {
            Statements = statements;
            EndOfFileToken = endOfFileToken;
        }

        public ImmutableArray<StatementSyntax> Statements { get; }
        public SyntaxToken EndOfFileToken { get; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var statement in Statements)
            {
                yield return statement;
            }
            yield return EndOfFileToken;
        }
    }
}