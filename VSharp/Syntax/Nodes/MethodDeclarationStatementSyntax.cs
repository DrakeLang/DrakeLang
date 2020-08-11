//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
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

using System.Collections.Generic;

namespace VSharp.Syntax
{
    /**
     * Syntax:
     * def methodName() {}
     */

    public sealed class MethodDeclarationStatementSyntax : StatementSyntax
    {
        public MethodDeclarationStatementSyntax(SyntaxToken defKeyword, SyntaxToken identifier, SyntaxToken leftParenthesis, SyntaxToken rightParenthesis, BlockStatementSyntax declaration)
        {
            DefKeyword = defKeyword;
            Identifier = identifier;
            LeftParenthesis = leftParenthesis;
            RightParenthesis = rightParenthesis;
            Declaration = declaration;
        }

        public override SyntaxKind Kind => SyntaxKind.MethodDeclarationStatement;

        public SyntaxToken DefKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken LeftParenthesis { get; }
        public SyntaxToken RightParenthesis { get; }
        public BlockStatementSyntax Declaration { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DefKeyword;
            yield return Identifier;
            yield return LeftParenthesis;
            yield return RightParenthesis;
            yield return Declaration;
        }
    }
}