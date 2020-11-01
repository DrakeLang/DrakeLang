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
    internal sealed class TypeofExpressionSyntax : ExpressionSyntax
    {
        public TypeofExpressionSyntax(
            SyntaxToken typeofKeyword,
            SyntaxToken leftParenthesis,
            TypeExpressionSyntax typeExpression,
            SyntaxToken rightParenthesis)
        {
            TypeofKeyword = typeofKeyword;
            LeftParenthesis = leftParenthesis;
            TypeExpression = typeExpression;
            RightParenthesis = rightParenthesis;
        }

        #region Properties

        public override SyntaxKind Kind => SyntaxKind.TypeofExpression;

        public SyntaxToken TypeofKeyword { get; }
        public SyntaxToken LeftParenthesis { get; }
        public TypeExpressionSyntax TypeExpression { get; }
        public SyntaxToken RightParenthesis { get; }

        #endregion Properties

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeofKeyword;
            yield return LeftParenthesis;
            yield return TypeExpression;
            yield return RightParenthesis;
        }
    }
}