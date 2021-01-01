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

using DrakeLang.Binding;
using DrakeLang.Symbols;
using DrakeLang.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DrakeLang
{
    public sealed record CompilationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating if the compiled program should be optimized.
        /// </summary>
        /// <value>Default is true.</value>
        public bool Optimize { get; init; } = true;
    }

    public sealed class Compilation
    {
        private BindingResult? _bindingResult;

        public Compilation(SyntaxTree syntaxTree) : this(syntaxTree, new())
        { }

        public Compilation(SyntaxTree syntaxTree, CompilationOptions options)
        {
            SyntaxTree = syntaxTree;
            Options = options;
        }

        public SyntaxTree SyntaxTree { get; }
        public CompilationOptions Options { get; }

        public BindingResult BindingResult
        {
            get
            {
                if (_bindingResult is null)
                {
                    var result = Binder.Bind(SyntaxTree.CompilationUnits, Options);
                    result = new BindingResult(result.Methods, result.Diagnostics);

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

            evaluator.Evaluate(BindingResult.Methods, variables);
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty);
        }

        #endregion Methods
    }
}