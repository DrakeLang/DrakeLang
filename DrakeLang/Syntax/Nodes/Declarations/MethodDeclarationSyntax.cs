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

namespace DrakeLang.Syntax
{
    /**
     * Syntax:
     * def methodName() {}
     * int methodName() {}
     * def methodName(type param1) {}
     * def methodName(type param2, type param2) {}
     */

    public sealed class MethodDeclarationSyntax : DeclarationSyntax
    {
        internal MethodDeclarationSyntax(SyntaxToken typeOrDefKeyword, SyntaxToken identifier, SyntaxToken leftParenthesis, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken rightParenthesis, BodyStatementSyntax declaration)
        {
            TypeOrDefKeyword = typeOrDefKeyword;
            Identifier = identifier;
            LeftParenthesis = leftParenthesis;
            Parameters = parameters;
            RightParenthesis = rightParenthesis;
            Declaration = declaration;
        }

        public override SyntaxKind Kind => SyntaxKind.MethodDeclaration;

        public SyntaxToken TypeOrDefKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken LeftParenthesis { get; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
        public SyntaxToken RightParenthesis { get; }
        public BodyStatementSyntax Declaration { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeOrDefKeyword;
            yield return Identifier;
            yield return LeftParenthesis;
            foreach (var parameter in Parameters)
            {
                yield return parameter;
            }
            yield return RightParenthesis;
            yield return Declaration;
        }
    }
}