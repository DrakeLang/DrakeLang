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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DrakeLang.Binding;
using DrakeLang.Symbols;

namespace DrakeLang.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private readonly LabelGenerator _labelGenerator;

        private Lowerer(LabelGenerator labelGenerator)
        {
            _labelGenerator = labelGenerator;
        }

        #region Methods

        public static ImmutableArray<BoundStatement> Lower(IReadOnlyList<BoundStatement> statements, LabelGenerator labelGenerator)
        {
            return new Lowerer(labelGenerator).Lower(statements);
        }

        public static ImmutableArray<BoundStatement> Lower(BoundStatement statement, LabelGenerator labelGenerator)
        {
            return new Lowerer(labelGenerator).Lower(new[] { statement });
        }

        #endregion Methods

        private ImmutableArray<BoundStatement> Lower(IReadOnlyList<BoundStatement> statements)
        {
            var result = statements.ToImmutableArray();

            bool reRunLowering;
            do
            {
                result = RewriteStatements(result) ?? result;
                result = FlattenAndClean(result, out reRunLowering);
            } while (reRunLowering);

            return result;
        }

        #region RewriteStatement

        protected override BoundStatement RewriteMethodDeclarationStatement(BoundMethodDeclaration node)
        {
            var declaration = Lower(node.Declaration);

            var methods = declaration.OfType<BoundMethodDeclaration>();
            var generalStatements = declaration.Except(methods).ToImmutableArray();

            var method = new BoundMethodDeclaration(node.Method, generalStatements);
            methods = methods.Append(method);

            return new BoundBlockStatement(methods.ToImmutableArray<BoundStatement>());
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundBlockStatement result;
            if (node.ElseStatement is null)
            {
                /**
                 * if (<condition>)
                 *    <then>
                 *
                 * -------->
                 *
                 * {
                 *    gotoFalse <condition> end
                 *    <then>
                 *    end:
                 * }
                 *
                 */

                var endLabel = _labelGenerator.GenerateLabel(LabelCategory.End);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var conditionalGotoEnd = new BoundConditionalGotoStatement(endLabel, node.Condition, jumpIfFalse: true);

                result = new BoundBlockStatement(ImmutableArray.Create(
                    conditionalGotoEnd,
                    node.ThenStatement,
                    endLabelStatement));
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
                 * {
                 *    gotoFalse <condition> else
                 *    <then>
                 *    goto end
                 *    else:
                 *    <else>
                 *    end:
                 * }
                 *
                 */

                var elseLabel = _labelGenerator.GenerateLabel(LabelCategory.Else);
                var endLabel = _labelGenerator.GenerateLabel(LabelCategory.End);

                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var conditionalGotoElse = new BoundConditionalGotoStatement(elseLabel, node.Condition, jumpIfFalse: true);
                var gotoEnd = new BoundGotoStatement(endLabel);

                result = new BoundBlockStatement(ImmutableArray.Create(
                    conditionalGotoElse,
                    node.ThenStatement,
                    gotoEnd,
                    elseLabelStatement,
                    node.ElseStatement,
                    endLabelStatement));
            }

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
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

            var result = new BoundBlockStatement(ImmutableArray.Create(
                checkLabelStatement,
                conditionalGotoEnd,
                node.Body,
                gotoCheck,
                endLabelStatement));

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            /**
             * for (<init>; <condition>; <update>)
             *    <body>
             *
             * ------->
             *
             * {
             *    <init>
             *    while (<condition>)
             *    {
             *       <body>
             *       continue:
             *       <update>
             *    }
             * }
             *
             */

            // Create the inner while statement (condition, body, update).
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, continueLabelStatement, node.UpdateStatement ?? BoundNoOpStatement.Instance));
            var whileStatement = new BoundWhileStatement(node.Condition ?? BoundLiteralExpression.True, whileBlock, _labelGenerator.GenerateLabel(LabelCategory.Continue), node.BreakLabel);

            // Create the outer block statement (init, while).
            var result = new BoundBlockStatement(ImmutableArray.Create(node.InitializationStatement ?? BoundNoOpStatement.Instance, whileStatement));

            return RewriteStatement(result);
        }

        #endregion RewriteStatement

        #region Helpers

        /// <summary>
        /// Flattens into a single block statement, removing unused labels and similar statements.
        /// </summary>
        /// <param name="reRunLowering">True if the lowering has to be re-run due to changed state.</param>
        private ImmutableArray<BoundStatement> FlattenAndClean(ImmutableArray<BoundStatement> statements, out bool reRunLowering)
        {
            reRunLowering = false;

            var result = new List<BoundStatement>();

            var statementStack = new Stack<BoundStatement>(statements.Reverse());

            // Remove nested block statements.
            while (statementStack.Count > 0)
            {
                var current = statementStack.Pop();
                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                        statementStack.Push(s);
                }
                else
                {
                    result.Add(current);
                }
            }

            // Remove unused variables
            bool removedVariables;
            do
            {
                removedVariables = false;

                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is BoundVariableDeclarationStatement variableDeclaration)
                    {
                        var variable = GetActiveVariable(variableDeclaration.Variable);
                        if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                        {
                            removedVariables = true;
                            result[i] = RewriteStatement(new BoundExpressionStatement(variableDeclaration.Initializer));
                        }
                    }
                    else if (result[i] is BoundExpressionStatement expressionStatement &&
                       expressionStatement.Expression is BoundAssignmentExpression assignmentExpression)
                    {
                        var variable = GetActiveVariable(assignmentExpression.Variable);
                        if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                        {
                            removedVariables = true;
                            result[i] = RewriteStatement(new BoundExpressionStatement(assignmentExpression.Expression));
                        }
                    }
                }
            }
            while (removedVariables);

            // Convert single-init variables to read-only -or- constants.
            bool updatedVariables;
            do
            {
                updatedVariables = false;

                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is BoundVariableDeclarationStatement variableDeclaration)
                    {
                        var oldVariable = GetActiveVariable(variableDeclaration.Variable);
                        if (!oldVariable.IsReadOnly && !ReassignedVariables.Contains(oldVariable))
                        {
                            updatedVariables = true;
                            reRunLowering = true;

                            var newVariable = new VariableSymbol(oldVariable.Name, isReadOnly: true, oldVariable.Type);
                            UpdateVariable(oldVariable, newVariable);

                            result[i] = RewriteStatement(new BoundVariableDeclarationStatement(newVariable, variableDeclaration.Initializer));
                        }
                    }
                }
            } while (updatedVariables);

            // Remove unused labels, no-op statements.
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
            result.RemoveAll(s => s is BoundNoOpStatement ||
                                  s is BoundLabelStatement labelStatement && !labels.Remove(labelStatement.Label));

            return result.ToImmutableArray();
        }

        #endregion Helpers
    }
}