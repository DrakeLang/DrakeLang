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

#pragma warning disable CA1724 // on't have type named Compilation due to conflict with 'System.Web.Compilation'

using System.Collections.Generic;
using VSharp.Symbols;

namespace VSharp
{
    internal sealed class LabelGenerator
    {
        private readonly Dictionary<LabelCategory, int> _labelCounters = new Dictionary<LabelCategory, int>();

        public LabelGenerator()
        {
        }

        public LabelSymbol GenerateLabel(LabelCategory category)
        {
            _labelCounters.TryGetValue(category, out int count);

            string name = $"{category}_{count}";

            count++;

            _labelCounters[category] = count;
            return new LabelSymbol(name);
        }
    }
}