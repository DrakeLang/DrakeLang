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

using System.Collections.Generic;
using System.Collections.Immutable;

namespace VSharp.Symbols
{
    public static class SystemSymbols
    {
        public static class Namespaces
        {
            public static readonly NamespaceSymbol Sys = new(ImmutableArray.Create("Sys"));
        }

        public static class Methods
        {
            public static readonly MethodSymbol Print = new(Namespaces.Sys, "Print", ImmutableArray.Create(new ParameterSymbol("text", Types.String)), Types.Void);
            public static readonly MethodSymbol Input = new(Namespaces.Sys, "Input", ImmutableArray<ParameterSymbol>.Empty, Types.String);

            public static IEnumerable<MethodSymbol> GetAll()
            {
                yield return Print;
                yield return Input;
            }
        }

        public static class Types
        {
            public static readonly TypeSymbol Error = new("?");
            public static readonly TypeSymbol Void = new("void");

            public static readonly TypeSymbol Boolean = new("bool");
            public static readonly TypeSymbol Int = new("int");
            public static readonly TypeSymbol Float = new("float");
            public static readonly TypeSymbol String = new("string");
            public static readonly TypeSymbol Char = new("char");
        }
    }
}