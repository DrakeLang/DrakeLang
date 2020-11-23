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

using System;

namespace VSharp.Text
{
    /// <summary>
    /// Describes the position and length of a section of text.
    /// </summary>
    public readonly struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
    {
        #region Constructors

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Creates a text span from the given start and end values.
        /// </summary>
        public static TextSpan FromBounds(int start, int end)
        {
            int length = end - start;
            return new TextSpan(start, length);
        }

        #endregion Constructors

        #region Properties

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        #endregion Properties

        public override string ToString()
        {
            return $"{Start}..{End}";
        }

        #region Operators

        public override bool Equals(object? obj)
        {
            return obj is TextSpan span && Equals(span);
        }

        public bool Equals(TextSpan other)
        {
            return Start == other.Start &&
                   Length == other.Length;
        }

        public override int GetHashCode() => HashCode.Combine(Start, Length);

        public int CompareTo(TextSpan other)
        {
            return Start - other.Start;
        }

        public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);

        public static bool operator !=(TextSpan left, TextSpan right) => !(left == right);

        public static bool operator >(TextSpan left, TextSpan right) => left.CompareTo(right) > 0;

        public static bool operator <(TextSpan left, TextSpan right) => left.CompareTo(right) < 0;

        public static bool operator >=(TextSpan left, TextSpan right) => left.CompareTo(right) >= 0;

        public static bool operator <=(TextSpan left, TextSpan right) => left.CompareTo(right) <= 0;

        #endregion Operators
    }
}