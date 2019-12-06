//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace PHPSharp.Binding
{
    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new Dictionary<string, VariableSymbol>();

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

        public bool TryDeclare(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name))
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string? name, [NotNullWhen(true)] out VariableSymbol? variable)
        {
            if (name is null)
            {
                variable = null;
                return false;
            }

            if (_variables.TryGetValue(name, out variable))
                return true;

            if (Parent is null)
                return false;

            return Parent.TryLookup(name, out variable);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
        {
            return _variables.Values.ToImmutableArray();
        }

        #endregion Methods
    }
}