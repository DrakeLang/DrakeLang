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

using DrakeLang.Text;
using System.Collections.Generic;
using System.Linq;

namespace DrakeLang.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        internal SyntaxToken(SourceText text, SyntaxKind kind, int position, string tokenText, object? value)
        {
            Text = text;
            Kind = kind;
            Position = position;
            TokenText = tokenText;
            Value = value;
        }

        #region Properties

        public override SourceText Text { get; }
        public override SyntaxKind Kind { get; }

        public int Position { get; }

        /// <summary>
        /// The text of this token.
        /// </summary>
        public string TokenText { get; }

        public object? Value { get; }
        public override TextSpan Span => new TextSpan(Position, TokenText.Length);

        #endregion Properties

        #region Methods

        public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

        #endregion Methods
    }
}