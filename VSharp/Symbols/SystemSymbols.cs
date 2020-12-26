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
using System.Linq;

namespace VSharp.Symbols
{
    public static class SystemSymbols
    {
        public static class Namespaces
        {
            public static readonly NamespaceSymbol Sys = new(new[] { "Sys" });

            public static readonly NamespaceSymbol Sys_Console = new(Sys, new[] { "Console" });
            public static readonly NamespaceSymbol Sys_IO = new(Sys, new[] { "IO" });
            public static readonly NamespaceSymbol Sys_String = new(Sys, new[] { "String" });

            public static readonly NamespaceSymbol Sys_IO_File = new(Sys_IO, new[] { "File" });
        }

        public static class Methods
        {
            #region Sys.Console

            public static readonly MethodSymbol Sys_Console_Write = new(Namespaces.Sys_Console, "Write", ImmutableArray.Create(new ParameterSymbol("value", Types.Object)), Types.Void);
            public static readonly MethodSymbol Sys_Console_WriteLine = new(Namespaces.Sys_Console, "WriteLine", ImmutableArray.Create(new ParameterSymbol("value", Types.Object)), Types.Void);
            public static readonly MethodSymbol Sys_Console_ReadLine = new(Namespaces.Sys_Console, "ReadLine", ImmutableArray<ParameterSymbol>.Empty, Types.String);

            #endregion Sys.Console

            #region Sys.IO.File

            public static readonly MethodSymbol Sys_IO_File_ReadAllText = new(Namespaces.Sys_IO_File, "ReadAllText", ImmutableArray.Create(new ParameterSymbol("path", Types.String)), Types.String);

            #endregion Sys.IO.File

            #region Sys.String

            public static readonly MethodSymbol Sys_String_Length = new(Namespaces.Sys_String, "Length", ImmutableArray.Create(new ParameterSymbol("str", Types.String)), Types.Int);

            public static readonly MethodSymbol Sys_String_CharAt = new(Namespaces.Sys_String, "GetChar",
                ImmutableArray.Create(
                    new ParameterSymbol("pos", Types.Int),
                    new ParameterSymbol("str", Types.String)
                ), Types.Char);

            #endregion Sys.String

            public static IEnumerable<MethodSymbol> GetAll()
            {
                return typeof(Methods).GetFields()
                                      .Where(f => f.FieldType == typeof(MethodSymbol))
                                      .Select(f => f.GetValue(null))
                                      .Cast<MethodSymbol>();
            }
        }

        public static class Types
        {
            public static readonly TypeSymbol Object = TypeSymbol.Object;

            public static readonly TypeSymbol Error = new("?");
            public static readonly TypeSymbol Void = new("void");

            public static readonly TypeSymbol Boolean = new("bool");
            public static readonly TypeSymbol Int = new("int");
            public static readonly TypeSymbol Float = new("float");
            public static readonly TypeSymbol Char = new("char");

            #region String

            public static readonly TypeSymbol String = new TypeSymbolBuilder
            {
                Name = "string",
                Methods = ImmutableArray.Create(new MethodSymbol[]
                {
                    new(MethodSymbol.GetIndexerName, ImmutableArray.Create(new ParameterSymbol("index", Int)), Char),
                    new("Length", ImmutableArray<ParameterSymbol>.Empty, Int),
                }),
            }.Build();

            #endregion String

            #region Array

            public static readonly TypeSymbol Array = new TypeSymbolBuilder
            {
                Name = "Array",
                GenericTypeArguments = ImmutableArray.Create(
                    new TypeSymbol(new GenericArgumentSymbolBuilder { Name = "T" })
                ),
                Methods = ImmutableArray.Create(new MethodSymbol[]
                {
                    new(MethodSymbol.GetIndexerName, 
                        parameters: ImmutableArray.Create(new ParameterSymbol("index", Int)), 
                        returnType: new TypeSymbol(new GenericArgumentSymbolBuilder { Name = "T" }))
                }),
            }.Build();

            #endregion Array
        }
    }
}