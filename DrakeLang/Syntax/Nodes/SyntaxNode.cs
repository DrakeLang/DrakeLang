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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DrakeLang.Syntax
{
    public abstract class SyntaxNode
    {
        private readonly Lazy<SourceText> _text;

        internal SyntaxNode()
        {
            _text = new(() => GetChildren().First().Text);
        }

        public abstract SyntaxKind Kind { get; }

        public virtual SourceText Text => _text.Value;

        public virtual TextSpan Span
        {
            get
            {
                TextSpan first = GetChildren().First().Span;
                TextSpan last = GetChildren().Last().Span;

                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        #region Methods

        public abstract IEnumerable<SyntaxNode> GetChildren();

        public override string ToString()
        {
            using StringWriter writer = new StringWriter();

            this.WriteTo(writer);
            return writer.ToString();
        }

        #endregion Methods
    }
}