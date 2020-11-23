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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace VSharp.Symbols
{
    public sealed class NamespaceSymbol : Symbol
    {
        internal NamespaceSymbol(ImmutableArray<string> names) : base(string.Join('.', names))
        {
            Names = names;
        }

        internal NamespaceSymbol(NamespaceSymbol parent, IEnumerable<string> extraNames) : this(parent.Names.AddRange(extraNames))
        {
        }

        public override SymbolKind Kind => SymbolKind.Namespace;

        public ImmutableArray<string> Names { get; }

        public override bool Equals(object? obj)
        {
            return obj is NamespaceSymbol other
                && Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Name);
    }
}