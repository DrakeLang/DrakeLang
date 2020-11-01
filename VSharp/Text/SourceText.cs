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

using System.Collections.Immutable;

namespace VSharp.Text
{
    public class SourceText
    {
        private readonly string _text;

        private SourceText(string text)
        {
            _text = text;
            Lines = ParseLines(this, text);
        }

        public static SourceText From(string text)
        {
            return new SourceText(text);
        }

        #region Properties

        public ImmutableArray<TextLine> Lines { get; }

        public char this[int index] => _text[index];

        public int Length => _text.Length;

        #endregion Properties

        #region Methods

        public int GetLineIndex(int position)
        {
            int lower = 0;
            int upper = Lines.Length - 1;

            while (lower <= upper)
            {
                int index = lower + (upper - lower) / 2;
                int start = Lines[index].Start;

                if (position == start)
                    return index;

                if (position < start)
                    upper = index - 1;
                else
                    lower = index + 1;
            }

            return lower - 1;
        }

        public override string ToString()
        {
            return _text;
        }

        public string ToString(int start, int length) => _text.Substring(start, length);

        public string ToString(TextSpan span)
        {
            int length = span.Length;
            if (length <= 0)
                return string.Empty;

            return _text.Substring(span.Start, length);
        }

        #endregion Methods

        #region Static methods

        private static ImmutableArray<TextLine> ParseLines(SourceText sourceText, string text)
        {
            var result = ImmutableArray.CreateBuilder<TextLine>();

            int position = 0;
            int lineStart = 0;

            while (position < text.Length)
            {
                int lineBreakWidth = GetLineBreakWidth(text, position);
                if (lineBreakWidth == 0)
                {
                    position++;
                }
                else
                {
                    AddLine(result, sourceText, position, lineStart, lineBreakWidth);

                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, sourceText, position, lineStart, 0);

            return result.ToImmutable();
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder result, SourceText sourceText, int position, int lineStart, int lineBreakWidth)
        {
            int lineLength = position - lineStart;
            int lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
            var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);

            result.Add(line);
        }

        private static int GetLineBreakWidth(string text, int position)
        {
            char currentChar = text[position];
            char nextChar = position + 1 >= text.Length ? '\0' : text[position + 1];

            if (currentChar == '\r' && nextChar == '\n')
                return 2;

            if (currentChar == '\r' || currentChar == '\n')
                return 1;

            return 0;
        }

        #endregion Static methods
    }
}