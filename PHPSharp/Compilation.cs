﻿//------------------------------------------------------------------------------
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

using PHPSharp.Binding;
using PHPSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace PHPSharp
{
    public class Compilation
    {
        public Compilation(SyntaxTree syntaxTree)
        {
            Syntax = syntaxTree;
        }

        public SyntaxTree Syntax { get; }

        #region Methods

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            Binder binder = new Binder(variables);
            BoundExpression boundExpression = binder.BindExpression(Syntax.Root);

            IEnumerable<Diagnostic> diagnostics = Syntax.Diagnostics.Concat(binder.Diagnostics);
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            Evaluator evaluator = new Evaluator(boundExpression, variables);
            string value = evaluator.Evaluate();

            return new EvaluationResult(Enumerable.Empty<Diagnostic>(), value);
        }

        #endregion Methods
    }
}