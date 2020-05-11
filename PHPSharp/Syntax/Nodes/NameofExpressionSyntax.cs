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

using System.Collections.Generic;

namespace PHPSharp.Syntax
{
    internal sealed class NameofExpressionSyntax : ExpressionSyntax
    {
        public NameofExpressionSyntax(
           SyntaxToken typeofKeyword,
           SyntaxToken leftParenthesis,
           SyntaxToken identifierToken,
           SyntaxToken rightParenthesis)
        {
            NameofKeyword = typeofKeyword;
            LeftParenthesis = leftParenthesis;
            IdentifierToken = identifierToken;
            RightParenthesis = rightParenthesis;
        }

        #region Properties

        public override SyntaxKind Kind => SyntaxKind.NameofExpression;

        public SyntaxToken NameofKeyword { get; }
        public SyntaxToken LeftParenthesis { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken RightParenthesis { get; }

        #endregion Properties

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NameofKeyword;
            yield return LeftParenthesis;
            yield return IdentifierToken;
            yield return RightParenthesis;
        }
    }
}