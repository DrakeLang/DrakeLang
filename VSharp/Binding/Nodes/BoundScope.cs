//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Niklas Gransjøen
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

using VSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace VSharp.Binding
{
    internal sealed class BoundScope
    {
        private Dictionary<string, VariableSymbol>? _variables;
        private Dictionary<string, MethodSymbol>? _methods;

        #region Constructors

        public BoundScope() : this(null)
        {
        }

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        #endregion Constructors

        #region Properties

        public BoundScope? Parent { get; }

        #endregion Properties

        #region Methods

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            if (_variables is null)
            {
                _variables = new Dictionary<string, VariableSymbol>
                {
                    [variable.Name] = variable
                };

                return true;
            }

            if (_variables.ContainsKey(variable.Name))
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        /// <summary>
        /// Attempts to locate a variable with the given name in this scope, or one of its parents.
        /// </summary>
        public bool TryLookupVariable(string name, [NotNullWhen(true)] out VariableSymbol? variable)
        {
            if (_variables != null && _variables.TryGetValue(name, out variable))
                return true;

            if (Parent is null)
            {
                variable = null;
                return false;
            }

            return Parent.TryLookupVariable(name, out variable);
        }

        public bool TryDeclareMethod(MethodSymbol method)
        {
            if (_methods is null)
            {
                _methods = new Dictionary<string, MethodSymbol>
                {
                    [method.Name] = method
                };
                return true;
            }

            if (_methods.ContainsKey(method.Name))
                return false;

            _methods[method.Name] = method;
            return true;
        }

        /// <summary>
        /// Attempts to locate a variable with the given name in this scope, or one of its parents.
        /// </summary>
        public bool TryLookupMethod(string? name, [NotNullWhen(true)] out MethodSymbol? method)
        {
            if (name is null)
            {
                method = null;
                return false;
            }

            if (_methods != null && _methods.TryGetValue(name, out method))
                return true;

            if (Parent is null)
            {
                method = null;
                return false;
            }

            return Parent.TryLookupMethod(name, out method);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
        {
            if (_variables is null)
                return ImmutableArray<VariableSymbol>.Empty;

            return _variables.Values.ToImmutableArray();
        }

        public ImmutableArray<MethodSymbol> GetDeclaredMethods()
        {
            if (_methods is null)
                return ImmutableArray<MethodSymbol>.Empty;

            return _methods.Values.ToImmutableArray();
        }

        #endregion Methods
    }
}