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

namespace DrakeLang
{
    public sealed record Diagnostic(SourceText? Text, TextSpan Span, string Message)
    {
        public Diagnostic(string Message) : this(null, default, Message)
        {
        }

        /// <summary>
        /// Returns a string representation of this diagnostic.
        /// </summary>
        public override string ToString()
        {
            if (Text is null)
                return Message;

            int lineIndex = Text.GetLineIndex(Span.Start);
            var line = Text.Lines[lineIndex];
            int lineNumer = lineIndex + 1;
            int character = Span.Start - line.Start + 1;

            if (Text.SourceFile is null)
                return $"{Text.SourceFile} ({lineNumer}, {character}): {Message}";
            else
                return $"({lineNumer}, {character}): {Message}";
        }
    }
}