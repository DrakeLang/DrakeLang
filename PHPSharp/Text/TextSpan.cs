//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
// Copyright (C) 2019  Niklas Gransjøen
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

namespace PHPSharp.Text
{
    /// <summary>
    /// Describes the position and length of a section of text.
    /// </summary>
    public struct TextSpan
    {
        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public override string ToString()
        {
            return $"{Start}..{End}";
        }

        /// <summary>
        /// Creates a text span from the given start and end values.
        /// </summary>
        public static TextSpan FromBounds(int start, int end)
        {
            int length = end - start;
            return new TextSpan(start, length);
        }
    }
}