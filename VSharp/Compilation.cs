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

#pragma warning disable CA1724 // don't have type named Compilation due to conflict with 'System.Web.Compilation'

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using VSharp.Binding;
using VSharp.Lowering;
using VSharp.Symbols;
using VSharp.Syntax;

namespace VSharp
{
    public sealed class Compilation
    {
        private readonly LabelGenerator _labelGenerator = new LabelGenerator();
        private BindingResult? _bindingResult;

        public Compilation(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public SyntaxTree SyntaxTree { get; }

        internal BindingResult BindingResult
        {
            get
            {
                if (_bindingResult is null)
                {
                    var result = Binder.Bind(SyntaxTree.Root, _labelGenerator);
                    if (result.Diagnostics.Length == 0)
                    {
                        var loweredResult = Lowerer.Lower(result.Statement, _labelGenerator);
                        result = new BindingResult(ImmutableArray<Diagnostic>.Empty, loweredResult);
                    }

                    Interlocked.CompareExchange(ref _bindingResult, result, null);
                }

                return _bindingResult;
            }
        }

        #region Methods

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var diagnostics = SyntaxTree.Diagnostics.Concat(BindingResult.Diagnostics);
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics.ToImmutableArray());

            var evaluator = new Evaluator();

            evaluator.Evaluate(BindingResult.Statement, variables);
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty);
        }

        public void PrintProgram(TextWriter writer)
        {
            BindingResult.Statement.WriteTo(writer);
        }

        #endregion Methods
    }
}