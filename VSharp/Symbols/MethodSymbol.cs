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

using System.Collections.Immutable;
using System.Linq;

namespace VSharp.Symbols
{
    public sealed class MethodSymbol : Symbol
    {
        public MethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType) : base(name)
        {
            Parameters = parameters;
            ReturnType = returnType;
        }

        public MethodSymbol(NamespaceSymbol? @namespace, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType) : base(name)
        {
            Namespace = @namespace;
            Parameters = parameters;
            ReturnType = returnType;
        }

        #region Properties

        public override SymbolKind Kind => SymbolKind.Method;

        public NamespaceSymbol? Namespace { get; }
        public string FullName => Namespace is null ? Name : Namespace.Name + "." + Name;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }

        #endregion Properties

        public override string ToString() => ToString(showParamName: false);

        public string ToString(bool showParamName)
        {
            var paramExpression = showParamName
                ? Parameters.Select(p => p.ToString())
                : Parameters.Select(p => p.Type.ToString());

            return ReturnType + " " + FullName + "(" + string.Join(", ", paramExpression) + ")";
        }
    }
}