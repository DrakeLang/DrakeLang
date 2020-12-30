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
using System.Collections.Immutable;
using System.IO;
using DrakeLang.Binding.CFA;

namespace DrakeLang.Binding
{
    public sealed class BindingResult
    {
        internal BindingResult(ImmutableArray<BoundMethodDeclaration> methods, ImmutableArray<Diagnostic> diagnostics)
        {
            Methods = methods;
            Diagnostics = diagnostics;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        internal ImmutableArray<BoundMethodDeclaration> Methods { get; }

        #region Methods

        public void PrintProgram(TextWriter writer)
        {
            foreach (var method in Methods)
            {
                method.WriteTo(writer);
            }
        }

        public delegate TextWriter ControlFlowGraphWriterFactory(string methodName);

        public void GenerateControlFlowGraphs(ControlFlowGraphWriterFactory writerFactory, Action<TextWriter>? cleanup = null)
        {
            if (writerFactory is null)
                throw new ArgumentNullException(nameof(writerFactory));

            foreach (var method in Methods)
            {
                var writer = writerFactory(method.Method.Name);
                if (writer is null)
                    throw new ArgumentException("Factory returned null", nameof(writerFactory));

                new ControlFlowGraph(method.Declaration).WriteTo(writer);

                cleanup?.Invoke(writer);
            }
        }

        #endregion Methods
    }
}