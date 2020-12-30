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

using DrakeLang.Utils;

namespace DrakeLang.Symbols
{
    public sealed class ConstantSymbol : VariableSymbol
    {
        internal ConstantSymbol(string name, object value) : base(name, isReadOnly: true, TypeSymbolUtil.FromValue(value))
        {
            Value = value;
        }

        public object Value { get; }

        public override string ToString()
        {
            return "const " + base.ToString();
        }
    }
}