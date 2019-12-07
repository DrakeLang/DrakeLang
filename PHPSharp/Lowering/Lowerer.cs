//------------------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PHPSharp.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private Lowerer()
        {
        }

        public static BoundBlockStatement Lower(BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            BoundStatement result = lowerer.RewriteStatement(statement);

            return Flatten(result);
        }

        #region RewriteStatement

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

                LabelSymbol endLabel = GenerateLabel(LabelCategory.End);
                BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);

                BoundConditionalGotoStatement conditionalGotoEnd = new BoundConditionalGotoStatement(endLabel, node.Condition, jumpIfFalse: true);

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

                LabelSymbol elseLabel = GenerateLabel(LabelCategory.Else);
                LabelSymbol endLabel = GenerateLabel(LabelCategory.End);

                BoundLabelStatement elseLabelStatement = new BoundLabelStatement(elseLabel);
                BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);

                BoundConditionalGotoStatement conditionalGotoElse = new BoundConditionalGotoStatement(elseLabel, node.Condition, jumpIfFalse: true);
                BoundGotoStatement gotoEnd = new BoundGotoStatement(endLabel);

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

            LabelSymbol checkLabel = GenerateLabel(LabelCategory.Check);
            LabelSymbol endLabel = GenerateLabel(LabelCategory.End);

            BoundLabelStatement checkLabelStatement = new BoundLabelStatement(checkLabel);
            BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);

            BoundGotoStatement gotoCheck = new BoundGotoStatement(checkLabel);
            BoundConditionalGotoStatement conditionalGotoEnd = new BoundConditionalGotoStatement(endLabel, node.Condition, jumpIfFalse: true);

            BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(
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
             *       <update>
             *    }
             * }
             *
             */

            // Create the inner while statement (condition, body, update).
            BoundBlockStatement whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, node.UpdateStatement));
            BoundWhileStatement whileStatement = new BoundWhileStatement(node.Condition, whileBlock);

            // Create the outer block statement (init, while).
            BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(node.InitializationStatement, whileStatement));

            return RewriteStatement(result);
        }

        #endregion RewriteStatement

        #region Utilities

        private int _labelCount;
        private readonly Dictionary<string, int> _labelCounters = new Dictionary<string, int>();

        private enum LabelCategory
        {
            Check,
            Else,
            End,
        }

        private LabelSymbol GenerateLabel(LabelCategory category)
        {
            _labelCounters.TryGetValue(category.ToString(), out int count);

            string name = $"{category}{count}_{_labelCount}";

            count++;
            _labelCount++;

            _labelCounters[name] = count;
            return new LabelSymbol(name);
        }

        #endregion Utilities

        #region Helpers

        private static BoundBlockStatement Flatten(BoundStatement statement)
        {
            ImmutableArray<BoundStatement>.Builder builder = ImmutableArray.CreateBuilder<BoundStatement>();

            Stack<BoundStatement> stack = new Stack<BoundStatement>();

            stack.Push(statement);
            while (stack.Count > 0)
            {
                BoundStatement current = stack.Pop();
                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                        stack.Push(s);
                }
                else
                {
                    builder.Add(current);
                }
            }

            return new BoundBlockStatement(builder.ToImmutableArray());
        }

        #endregion
    }
}