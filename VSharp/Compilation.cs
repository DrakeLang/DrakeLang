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

using VSharp.Binding;
using VSharp.Lowering;
using VSharp.Symbols;
using VSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO; 
using System.Linq;
using System.Threading;

namespace VSharp
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;

        public Compilation(SyntaxTree syntaxTree)
            : this(null, syntaxTree)
        {
        }

        private Compilation(Compilation? previous, SyntaxTree syntaxTree)
        {
            Previous = previous;
            SyntaxTree = syntaxTree;
        }

        public Compilation? Previous { get; }
        public SyntaxTree SyntaxTree { get; }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTree.Root);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        #region Methods

        public Compilation ContinueWith(SyntaxTree syntaxTree)
        {
            return new Compilation(this, syntaxTree);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            IEnumerable<Diagnostic> diagnostics = SyntaxTree.Diagnostics.Concat(GlobalScope.Diagnostics);
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics.ToImmutableArray());

            BoundBlockStatement statement = GetStatement();
            IEvaluator evaluator = new Evaluator();

            evaluator.Evaluate(statement, variables);
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty);
        }

        public void PrintProgram(TextWriter writer)
        {
            BoundBlockStatement statement = GetStatement();
            statement.WriteTo(writer);
        }

        private BoundBlockStatement GetStatement()
        {
            return Lowerer.Lower(GlobalScope.Statement);
        }

        #endregion Methods
    }
}