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
using VSharp.Symbols;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Utils
{
    internal static class TypeSymbolUtil
    {
        public static TypeSymbol FromClrType(Type type)
        {
            if (type == typeof(bool))
                return Types.Boolean;
            else if (type == typeof(int))
                return Types.Int;
            else if (type == typeof(string))
                return Types.String;
            else if (type == typeof(double))
                return Types.Float;

            throw new Exception($"Clr type '{type}' is illegal.");
        }

        public static TypeSymbol FromValue(object value)
        {
            return value switch
            {
                bool _ => Types.Boolean,
                int _ => Types.Int,
                string _ => Types.String,
                double _ => Types.Float,

                _ => throw new Exception($"Value '{value}' of type '{value.GetType()}' is illegal."),
            };
        }
    }
}