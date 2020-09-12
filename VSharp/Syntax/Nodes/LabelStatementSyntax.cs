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

namespace VSharp.Syntax
{
    public sealed class LabelStatementSyntax : StatementSyntax
    {
        public LabelStatementSyntax(SyntaxToken identifier, SyntaxToken colonToken)
        {
            Identifier = identifier;
            ColonToken = colonToken;
        }

        public override SyntaxKind Kind => SyntaxKind.LabelStatement;

        public SyntaxToken Identifier { get; }
        public SyntaxToken ColonToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return ColonToken;
        }
    }

    public sealed class GoToStatementSyntax : StatementSyntax
    {
        public GoToStatementSyntax(SyntaxToken goToKeyword, SyntaxToken label, SyntaxToken semicolon)
        {
            GoToKeyword = goToKeyword;
            Label = label;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.GoToStatement;

        public SyntaxToken GoToKeyword { get; }
        public SyntaxToken Label { get; }
        public SyntaxToken Semicolon { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return GoToKeyword;
            yield return Label;
            yield return Semicolon;
        }
    }
}