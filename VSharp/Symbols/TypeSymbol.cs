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

namespace VSharp.Symbols
{
    public sealed class TypeSymbol : MemberSymbol
    {
        /// <summary>
        /// The base type of all types.
        /// </summary>
        public static readonly TypeSymbol Object = new TypeSymbol("object");

        private readonly TypeSymbol? _baseType;

        internal TypeSymbol(string name) : base(name)
        {
        }

        internal TypeSymbol(string name, NamespaceSymbol? namespaceSym = null, TypeSymbol? baseType = null) : this(name)
        {
            Namespace = namespaceSym;
            _baseType = baseType;
        }

        public override SymbolKind Kind => SymbolKind.Type;
        public override NamespaceSymbol? Namespace { get; }
        public TypeSymbol BaseType => _baseType ?? Object;
    }
}