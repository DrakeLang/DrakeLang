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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Statements = System.Collections.Immutable.ImmutableArray<DrakeLang.Binding.BoundStatement>;

namespace DrakeLang.Lowering
{
    internal record LowererOptions
    {
        public bool Optimize { get; init; } = true;
    }

    internal sealed class Lowerer : BoundTreeRewriter
    {
        private readonly LowererOptions _options;
        private readonly LabelGenerator _labelGenerator;

        private Lowerer(LabelGenerator labelGenerator, LowererOptions? options = null)
        {
            _options = options ?? new();
            _labelGenerator = labelGenerator;
        }

        #region Methods

        public static ImmutableArray<BoundMethodDeclaration> Lower(ImmutableArray<BoundMethodDeclaration> methods, LabelGenerator labelGenerator, LowererOptions? options = null)
            => new Lowerer(labelGenerator, options).RewriteMethodDeclarations(methods);

        public static Statements Lower(Statements statements, LabelGenerator labelGenerator, LowererOptions? options = null)
            => new Lowerer(labelGenerator, options).Lower(statements);

        #endregion Methods

        private Statements Lower(Statements statements)
        {
            var result = statements;

            bool reRunLowering;
            do
            {
                result = RewriteStatements(result);
                if (!_options.Optimize)
                    break;

                result = Optimize(result, out reRunLowering);
            } while (reRunLowering);

            return result.SequenceEqual(statements) ? statements : result;
        }

        #region RewriteMethodDeclaration

        protected override BoundMethodDeclaration RewriteMethodDeclaration(BoundMethodDeclaration node)
        {
            var declaration = Lower(node.Declaration);
            if (declaration == node.Declaration)
                return node;

            return new BoundMethodDeclaration(node.Method, declaration);
        }

        #endregion RewriteMethodDeclaration

        #region RewriteStatement

        protected override Statements? RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseBody.IsEmpty)
            {
                /**
                 * if (<condition>)
                 *    <then>
                 *
                 * -------->
                 *
                 * gotoFalse <condition> end
                 * <then>
                 * end:
                 *
                 */

                var endLabel = _labelGenerator.GenerateLabel(LabelCategory.End);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var conditionalGotoEnd = new BoundConditionalGotoStatement(endLabel, node.Condition, jumpIfFalse: true);

                var result = ImmutableArray.CreateBuilder<BoundStatement>(2 + node.ThenBody.Length);
                result.Add(conditionalGotoEnd);
                result.AddRange(node.ThenBody);
                result.Add(endLabelStatement);

                return RewriteStatements(result.MoveToImmutable());
            }
            else
            {
                /**
                 *
                 * if (<condition>)
                 *    <then>
                 * else
                 *    <else>
                 *
                 * -------->
                 *
                 * gotoFalse <condition> else
                 * <then>
                 * goto end
                 * else:
                 * <else>
                 * end:
                 *
                 */

                var elseLabel = _labelGenerator.GenerateLabel(LabelCategory.Else);
                var endLabel = _labelGenerator.GenerateLabel(LabelCategory.End);

                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var conditionalGotoElse = new BoundConditionalGotoStatement(elseLabel, node.Condition, jumpIfFalse: true);
                var gotoEnd = new BoundGotoStatement(endLabel);

                var result = ImmutableArray.CreateBuilder<BoundStatement>(4 + node.ThenBody.Length + node.ElseBody.Length);

                result.Add(conditionalGotoElse);
                result.AddRange(node.ThenBody);
                result.Add(gotoEnd);
                result.Add(elseLabelStatement);
                result.AddRange(node.ElseBody);
                result.Add(endLabelStatement);

                return RewriteStatements(result.MoveToImmutable());
            }
        }

        protected override Statements? RewriteWhileStatement(BoundWhileStatement node)
        {
            /**
             *
             * while (<condition>)
             *    <body>
             *
             * -------->
             *
             * check:
             * gotoFalse <condition> end
             * <body>
             * goto check
             * end:
             *
             */

            var checkLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var endLabelStatement = new BoundLabelStatement(node.BreakLabel);

            var gotoCheck = new BoundGotoStatement(node.ContinueLabel);
            var conditionalGotoEnd = new BoundConditionalGotoStatement(node.BreakLabel, node.Condition, jumpIfFalse: true);

            var result = ImmutableArray.CreateBuilder<BoundStatement>(4 + node.Body.Length);
            result.Add(checkLabelStatement);
            result.Add(conditionalGotoEnd);
            result.AddRange(node.Body);
            result.Add(gotoCheck);
            result.Add(endLabelStatement);

            return RewriteStatements(result.MoveToImmutable());
        }

        protected override Statements? RewriteForStatement(BoundForStatement node)
        {
            /**
             * for (<init>; <condition>; <update>)
             *    <body>
             *
             * ------->
             *
             * <init>
             * while (<condition>)
             *    <body>
             *    continue:
             *    <update>
             *
             */

            // Create the inner while statement (condition, body, update).
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);

            var whileBody = ImmutableArray.CreateBuilder<BoundStatement>(1 + node.Body.Length + node.UpdateStatement.Length);
            whileBody.AddRange(node.Body);
            whileBody.Add(continueLabelStatement);
            whileBody.AddRange(node.UpdateStatement);

            var whileStatement = new BoundWhileStatement(
                node.Condition ?? BoundLiteralExpression.True,
                whileBody.MoveToImmutable(),
                _labelGenerator.GenerateLabel(LabelCategory.Continue),
                node.BreakLabel);

            // Create the outer block statement (init, while).
            var result = ImmutableArray.CreateBuilder<BoundStatement>(1 + node.InitializationStatement.Length);
            result.AddRange(node.InitializationStatement);
            result.Add(whileStatement);

            return RewriteStatements(result.MoveToImmutable());
        }

        protected override Statements? RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expression = RewriteExpression(node.Expression);

            // Remove expression statements with no side effects.
            if (_options.Optimize && HasNoSideEffects(expression))
            {
                // Removed expressions may affect variable usage.
                VariableUsage.Values.ForEach(varUsage => varUsage.Remove(expression));
                return Statements.Empty;
            }

            if (expression == node.Expression)
                return null;

            return Wrap(new BoundExpressionStatement(expression));
        }

        #endregion RewriteStatement

        #region Helpers

        /// <summary>
        /// Optimizes the given statements by removing statements that have no side-effects.
        /// </summary>
        /// <param name="reRunLowering">True if the lowering has to be re-run due to changed state.</param>
        private Statements Optimize(Statements statements, out bool reRunLowering)
        {
            reRunLowering = false;

            var result = statements.ToList();

            // Remove unused variables
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] is BoundVariableDeclarationStatement variableDeclaration)
                {
                    var variable = GetActiveVariable(variableDeclaration.Variable);
                    if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                    {
                        reRunLowering = true;
                        result[i] = new BoundExpressionStatement(variableDeclaration.Initializer);
                    }
                }
                else if (result[i] is BoundExpressionStatement expressionStatement &&
                   expressionStatement.Expression is BoundAssignmentExpression assignmentExpression)
                {
                    var variable = GetActiveVariable(assignmentExpression.Variable);
                    if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                    {
                        reRunLowering = true;
                        result[i] = new BoundExpressionStatement(assignmentExpression.Expression);
                    }
                }
            }

            // Convert single-init variables to read-only -or- constants.
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] is BoundVariableDeclarationStatement variableDeclaration)
                {
                    var oldVariable = GetActiveVariable(variableDeclaration.Variable);
                    if (!oldVariable.IsReadOnly && !ReassignedVariables.Contains(oldVariable))
                    {
                        reRunLowering = true;

                        var newVariable = new VariableSymbol(oldVariable.Name, isReadOnly: true, oldVariable.Type);
                        UpdateVariable(oldVariable, newVariable);

                        result[i] = new BoundVariableDeclarationStatement(newVariable, variableDeclaration.Initializer);
                    }
                }
            }

            // Remove unused labels.
            var labels = new HashSet<LabelSymbol>();
            foreach (var s in result)
            {
                switch (s)
                {
                    case BoundGotoStatement gs:
                        labels.Add(gs.Label);
                        break;

                    case BoundConditionalGotoStatement cgs:
                        labels.Add(cgs.Label);
                        break;
                }
            }
            result.RemoveAll(s => s is BoundLabelStatement labelStatement && !labels.Remove(labelStatement.Label));

            return result.ToImmutableArray();
        }

        #endregion Helpers
    }
}