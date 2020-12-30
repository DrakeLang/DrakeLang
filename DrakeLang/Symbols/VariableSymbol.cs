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

using System;

namespace DrakeLang.Symbols
{
    public class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type) : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public override SymbolKind Kind => SymbolKind.Variable;
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }

        public override string ToString()
        {
            return Type + " " + Name;
        }

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is not VariableSymbol other)
                return false;

            return Name == other.Name &&
                IsReadOnly == other.IsReadOnly &&
                Type == other.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, IsReadOnly, Type);
        }

        public static bool operator ==(VariableSymbol? left, VariableSymbol? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(VariableSymbol? left, VariableSymbol? right)
        {
            return !(left == right);
        }

        #endregion Operators
    }
}