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
    public sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        internal LiteralExpressionSyntax(SyntaxToken literalToken) : this(literalToken, literalToken.Value)
        {
        }

        internal LiteralExpressionSyntax(SyntaxToken literalToken, object? value)
        {
            LiteralToken = literalToken;
            Value = value;
        }

        #region Properties

        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
        public SyntaxToken LiteralToken { get; }
        public object? Value { get; }

        #endregion Properties

        #region Methods

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }

        #endregion Methods
    }
}