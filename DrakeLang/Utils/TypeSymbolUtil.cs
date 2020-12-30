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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DrakeLang.Symbols;
using static DrakeLang.Symbols.SystemSymbols;

namespace DrakeLang.Utils
{
    internal static class TypeSymbolUtil
    {
        public static TypeSymbol FromClrType(Type type)
        {
            if (type == typeof(bool))
                return Types.Boolean;
            else if (type == typeof(int))
                return Types.Int;
            else if (type == typeof(double))
                return Types.Float;
            else if (type == typeof(string))
                return Types.String;
            else if (type == typeof(char))
                return Types.Char;
            if (type == typeof(object))
                return Types.Object;

            throw new Exception($"Clr type '{type}' is illegal.");
        }

        public static TypeSymbol FromValue(object value) => value switch
        {
            bool => Types.Boolean,
            int => Types.Int,
            double => Types.Float,
            string => Types.String,
            char => Types.Char,

            _ => throw new Exception($"Value '{value}' of type '{value.GetType()}' is illegal."),
        };

        private static readonly ImmutableHashSet<TypeSymbol> _primitiveTypes = ImmutableHashSet.Create(new[]
        {
            Types.Boolean,
            Types.Int,
            Types.Float,
            Types.Char,
            Types.String,
        });

        public static bool IsPrimitive(TypeSymbol type) => _primitiveTypes.Contains(type);

        public static IEnumerable<MethodSymbol> FindMethods(this TypeSymbol type, string name) => type.Methods.Where(m => m.Name == name);

        public static IEnumerable<MethodSymbol> FindGetIndexers(this TypeSymbol type) => type.FindMethods(MethodSymbol.GetIndexerName);

        public static IEnumerable<MethodSymbol> FindSetIndexers(this TypeSymbol type) => type.FindMethods(MethodSymbol.SetIndexerName);
    }
}