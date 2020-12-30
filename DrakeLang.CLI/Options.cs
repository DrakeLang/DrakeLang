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

using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrakeLangO
{
    [Flags]
    public enum DebugOutput
    {
        None = 0,
        ShowTree = 1,
        ShowProgram = 2,
        PrintControlFlowGraph = 4,
    }

    public sealed class Options
    {
        public sealed class Resources
        {
            public static string DebugHelpText => "Output various types of data. Values: " + string.Join(", ", Enum.GetValues<DebugOutput>()) + ".\n" +
                "Use separator '|' to provide multiple values.";
        }

        [Option('d', "debug", Separator = '|', HelpText = nameof(Resources.DebugHelpText), ResourceType = typeof(Resources))]
        public IEnumerable<DebugOutput> Debug { get; set; } = Enumerable.Empty<DebugOutput>();

        [Option('s', "source", HelpText = "The path to the source to compile", Required = true, Min = 1, Separator = ',')]
        public IEnumerable<string> Source { get; set; } = Enumerable.Empty<string>();

        public DebugOutput GetAggregatedDebugValues() => Debug.Aggregate(DebugOutput.None, (d1, d2) => d1 | d2);
    }
}