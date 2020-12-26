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

namespace VSharp.Symbols
{
    public sealed class IndexerSymbol : MemberSymbol
    {
        internal IndexerSymbol(ParameterSymbol parameter, TypeSymbol returnType) : base("[]")
        {
            Parameter = parameter;
            ReturnType = returnType;
        }

        public override NamespaceSymbol? Namespace => null;
        public override SymbolKind Kind => SymbolKind.Property;
        public ParameterSymbol Parameter { get; }
        public TypeSymbol ReturnType { get; }

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is not IndexerSymbol other)
                return false;

            return Name == other.Name &&
                Namespace == other.Namespace &&
                Parameter == other.Parameter &&
                ReturnType == other.ReturnType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Namespace, Parameter, ReturnType);
        }

        public static bool operator ==(IndexerSymbol? left, IndexerSymbol? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(IndexerSymbol? left, IndexerSymbol? right)
        {
            return !(left == right);
        }

        #endregion Operators
    }
}