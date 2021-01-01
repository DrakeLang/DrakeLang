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
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace DrakeLang.Tests
{
    /// <summary>
    /// Helper class for working out position of compiler errors.
    /// </summary>
    internal sealed class AnnotatedText
    {
        public AnnotatedText(string text, ImmutableArray<TextSpan> spans)
        {
            Text = text;
            Spans = spans;
        }

        #region Properties

        public string Text { get; }
        public ImmutableArray<TextSpan> Spans { get; }

        #endregion Properties

        #region Methods

        public static AnnotatedText Parse(string text)
        {
            text = Unindent(text);

            var textBuilder = new StringBuilder();
            var spanBuilder = ImmutableArray.CreateBuilder<TextSpan>();
            var startStack = new Stack<int>();

            int position = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '[')
                {
                    startStack.Push(position);
                }
                else if (c == ']')
                {
                    if (!startStack.TryPop(out int start))
                        throw new ArgumentException("Too many ']' in input text.", nameof(text));

                    int end = position;
                    TextSpan span = TextSpan.FromBounds(start, end);
                    spanBuilder.Add(span);
                }
                else
                {
                    if (c == '\\')
                    {
                        if (i < text.Length - 1 && text[i + 1] is '[' or ']')
                            i++;
                    }

                    position++;
                    textBuilder.Append(text[i]);
                }
            }

            if (startStack.Count != 0)
                throw new ArgumentException("Missing ']' in input text.", nameof(text));

            return new AnnotatedText(textBuilder.ToString(), spanBuilder.ToImmutable());
        }

        public static string[] UnintentLines(string text)
        {
            List<string> lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }

            int minIndentation = int.MaxValue;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.Trim().Length == 0)
                {
                    lines[i] = string.Empty;
                    continue;
                }

                int indentation = line.Length - line.TrimStart().Length;
                minIndentation = Math.Min(minIndentation, indentation);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length == 0)
                    continue;

                lines[i] = lines[i][minIndentation..];
            }

            while (lines.Count > 0 && lines[0].Length == 0)
                lines.RemoveAt(0);

            while (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines.ToArray();
        }

        #endregion Methods

        #region Private methods

        private static string Unindent(string text)
        {
            string[] lines = UnintentLines(text);
            return string.Join(Environment.NewLine, lines);
        }

        #endregion Private methods
    }
}