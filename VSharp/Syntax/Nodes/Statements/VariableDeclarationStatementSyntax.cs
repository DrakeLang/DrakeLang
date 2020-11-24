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
    public sealed class VariableDeclarationStatementSyntax : StatementSyntax
    {
        internal VariableDeclarationStatementSyntax(SyntaxToken keyword, SyntaxToken? explicitType, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax initializer, SyntaxToken? semicolonToken)
        {
            Keyword = keyword;
            ExplicitType = explicitType;
            Identifier = identifier;
            EqualsToken = equalsToken;
            Initializer = initializer;
            SemicolonToken = semicolonToken;
        }

        #region Properties

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public SyntaxToken Keyword { get; }
        public SyntaxToken? ExplicitType { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Initializer { get; }
        public SyntaxToken? SemicolonToken { get; }

        #endregion Properties

        #region Methods

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            if (ExplicitType is not null)
                yield return ExplicitType;

            yield return Identifier;
            yield return EqualsToken;
            yield return Initializer;

            if (SemicolonToken is not null)
                yield return SemicolonToken;
        }

        #endregion Methods
    }
}