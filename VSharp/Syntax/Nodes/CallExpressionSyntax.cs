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
    /**
     * Syntax:
     * - methodName();
     * - methodName(param1);
     * - methodName(param1, param2);
     */

    public sealed class CallExpressionSyntax : ExpressionSyntax
    {
        public CallExpressionSyntax(SyntaxToken identifier, SyntaxToken leftParenthesis, SeparatedSyntaxCollection<SyntaxNode> arguments, SyntaxToken rightParenthesis)
        {
            Identifier = identifier;
            LeftParenthesis = leftParenthesis;
            Arguments = arguments;
            RightParenthesis = rightParenthesis;
        }

        public override SyntaxKind Kind => SyntaxKind.CallExpression;

        public SyntaxToken Identifier { get; }
        public SyntaxToken LeftParenthesis { get; }
        public SeparatedSyntaxCollection<SyntaxNode> Arguments { get; }
        public SyntaxToken RightParenthesis { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return LeftParenthesis;

            foreach (var argument in Arguments.GetWithSeparators())
                yield return argument;

            yield return RightParenthesis;
        }
    }
}