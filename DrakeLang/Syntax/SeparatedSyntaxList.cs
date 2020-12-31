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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DrakeLang.Syntax
{
    public sealed class SeparatedSyntaxList<T> : IReadOnlyList<T>
        where T : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxNode> _nodesAndSeparators;

        public SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodesAndSeparators)
        {
            _nodesAndSeparators = nodesAndSeparators;
        }

        public int Count => (_nodesAndSeparators.Length + 1) / 2;

        public T this[int index] => (T)_nodesAndSeparators[index * 2];

        public TextSpan Span
        {
            get
            {
                var start = _nodesAndSeparators.Min(node => node.Span.Start);
                var end = _nodesAndSeparators.Max(node => node.Span.End);

                return TextSpan.FromBounds(start, end);
            }
        }

        public SyntaxToken GetSeparator(int index) => (SyntaxToken)_nodesAndSeparators[index * 2 + 1];

        public ImmutableArray<SyntaxNode> GetWithSeparators() => _nodesAndSeparators;

        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable<T>
    }
}