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
using System.Linq;
using VSharp.Syntax;

namespace VSharp.Symbols
{
    public sealed class NamespaceSymbol : Symbol
    {
        internal NamespaceSymbol(ImmutableArray<string> names) : base(string.Join('.', names))
        {
            Names = names;
        }

        internal NamespaceSymbol(SeparatedSyntaxList<SyntaxToken> names) : this(ToEnumerable(names))
        {
        }

        internal NamespaceSymbol(IEnumerable<string> names) : this(names.ToImmutableArray())
        {
        }

        internal NamespaceSymbol(NamespaceSymbol? parent, IEnumerable<string> extraNames) : this(parent is null ? extraNames.ToImmutableArray() : parent.Names.AddRange(extraNames))
        {
        }

        internal NamespaceSymbol(NamespaceSymbol? parent, SeparatedSyntaxList<SyntaxToken> extraNames) : this(parent, ToEnumerable(extraNames))
        {
        }

        public override SymbolKind Kind => SymbolKind.Namespace;

        public ImmutableArray<string> Names { get; }

        #region Operators

        public override bool Equals(object? obj)
        {
            return obj is NamespaceSymbol other
                && Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Name);

        public static bool operator ==(NamespaceSymbol? left, NamespaceSymbol? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(NamespaceSymbol? left, NamespaceSymbol? right)
        {
            return !(left == right);
        }

        #endregion Operators

        #region Utilities

        private static IEnumerable<string> ToEnumerable(SeparatedSyntaxList<SyntaxToken> names) => names.Select(name => name.Text ?? "");

        #endregion Utilities
    }
}