//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
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
using VSharp.Binding;
using VSharp.Symbols;

namespace VSharp.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private readonly LabelGenerator _labelGenerator;

        private Lowerer(LabelGenerator labelGenerator)
        {
            _labelGenerator = labelGenerator;
        }

        #region Methods

        public static ImmutableArray<BoundMethodDeclarationStatement> Lower(ImmutableArray<BoundMethodDeclarationStatement> methods, LabelGenerator labelGenerator)
        {
            var lowerer = new Lowerer(labelGenerator);
            var rewrittenMethods = lowerer.RewriteStatements(methods);

            if (rewrittenMethods is null)
                return methods;

            return rewrittenMethods.Value
                .SelectMany(m =>
                {
                    bool reRunLowering;
                    BoundBlockStatement result;

                    do
                    {
                        result = lowerer.FlattenAndClean(m, out reRunLowering);
                    } while (reRunLowering);

                    return result.Statements;
                })
                .Cast<BoundMethodDeclarationStatement>()
                .ToImmutableArray();
        }

        public static BoundBlockStatement Lower(BoundStatement statement, LabelGenerator labelGenerator)
        {
            return new Lowerer(labelGenerator).Lower(statement);
        }

        #endregion Methods

        private BoundBlockStatement Lower(BoundStatement statement)
        {
            bool reRunLowering;
            BoundBlockStatement flattenedResult;

            do
            {
                var result = RewriteStatement(statement);
                flattenedResult = FlattenAndClean(result, out reRunLowering);
            } while (reRunLowering);

            return flattenedResult;
        }

        #region RewriteStatement

        protected override BoundStatement RewriteMethodDeclarationStatement(BoundMethodDeclarationStatement node)
        {
            var declaration = Lower(node.Declaration);

            var methods = declaration.Statements.OfType<BoundMethodDeclarationStatement>();
            var generalStatements = declaration.Statements.Except(methods).ToImmutableArray();

            var method = new BoundMethodDeclarationStatement(node.Method, new BoundBlockStatement(generalStatements));
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
            var whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, continueLabelStatement, node.UpdateStatement));
            var whileStatement = new BoundWhileStatement(node.Condition, whileBlock, _labelGenerator.GenerateLabel(LabelCategory.Continue), node.BreakLabel);

            // Create the outer block statement (init, while).
            var result = new BoundBlockStatement(ImmutableArray.Create(node.InitializationStatement, whileStatement));

            return RewriteStatement(result);
        }

        #endregion RewriteStatement

        #region Helpers

        /// <summary>
        /// Flattens into a single block statement, removing unused labels and similar statements.
        /// </summary>
        /// <param name="reRunLowering">True if the lowering have to be re-run due to changed state.</param>
        private BoundBlockStatement FlattenAndClean(BoundStatement statement, out bool reRunLowering)
        {
            reRunLowering = false;

            var statements = new List<BoundStatement>();

            var statementStack = new Stack<BoundStatement>();
            statementStack.Push(statement);

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
                    statements.Add(current);
                }
            }

            // Remove unused variables
            bool removedVariables;
            do
            {
                removedVariables = false;

                for (int i = 0; i < statements.Count; i++)
                {
                    if (statements[i] is BoundVariableDeclarationStatement variableDeclaration)
                    {
                        var variable = GetActiveVariable(variableDeclaration.Variable);
                        if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                        {
                            removedVariables = true;
                            statements[i] = RewriteStatement(new BoundExpressionStatement(variableDeclaration.Initializer));
                        }
                    }
                    else if (statements[i] is BoundExpressionStatement expressionStatement &&
                       expressionStatement.Expression is BoundAssignmentExpression assignmentExpression)
                    {
                        var variable = GetActiveVariable(assignmentExpression.Variable);
                        if (VariableUsage.TryGetValue(variable, out var variableUsage) && variableUsage.Count == 0)
                        {
                            removedVariables = true;
                            statements[i] = RewriteStatement(new BoundExpressionStatement(assignmentExpression.Expression));
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

                for (int i = 0; i < statements.Count; i++)
                {
                    if (statements[i] is BoundVariableDeclarationStatement variableDeclaration)
                    {
                        var oldVariable = GetActiveVariable(variableDeclaration.Variable);
                        if (!oldVariable.IsReadOnly && !ReassignedVariables.Contains(oldVariable))
                        {
                            updatedVariables = true;
                            reRunLowering = true;

                            var newVariable = new VariableSymbol(oldVariable.Name, isReadOnly: true, oldVariable.Type);
                            UpdateVariable(oldVariable, newVariable);

                            statements[i] = RewriteStatement(new BoundVariableDeclarationStatement(newVariable, variableDeclaration.Initializer));
                        }
                    }
                }
            } while (updatedVariables);

            // Remove unused labels, no-op statements.
            var labels = new HashSet<LabelSymbol>();
            foreach (var s in statements)
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
            statements.RemoveAll(s => s is BoundNoOpStatement ||
                                      s is BoundLabelStatement labelStatement && !labels.Remove(labelStatement.Label));

            return new BoundBlockStatement(statements.ToImmutableArray());
        }

        #endregion Helpers
    }
}