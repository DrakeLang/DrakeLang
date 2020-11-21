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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VSharp.Symbols;

namespace VSharp.Binding.CFA
{
    internal sealed class ControlFlowGraph
    {
        private readonly GraphBlock _start = new GraphBlock(isStart: true);
        private readonly GraphBlock _end = new GraphBlock(isStart: false);
        private readonly List<GraphBlock> _blocks;
        private readonly List<GraphBranch> _branches;

        public ControlFlowGraph(BoundBlockStatement block)
        {
            (_blocks, _branches) = BuildGraph(block);
        }

        #region Methods

        public bool AllPathsReturn() => _end.Incoming.All(branch => branch.From.Statements[^1].Kind == BoundNodeKind.ReturnStatement);

        public void WriteTo(TextWriter writer)
        {
            writer.WriteLine("digraph G {");

            var blockIds = _blocks.Select((block, index) => (block, index)).ToDictionary(pair => pair.block, (pair) => $"N{pair.index}");
            foreach (var blockPair in blockIds)
            {
                var block = blockPair.Key;
                var id = blockPair.Value;

                var statements = block.ToString();
                writer.WriteLine($"    {id} [label = \"{quote(statements)}\" shape = box]");
            }

            foreach (var branch in _branches)
            {
                var condition = branch.Condition?.ToFriendlyString() ?? string.Empty;
                writer.WriteLine($"    {blockIds[branch.From]} -> {blockIds[branch.To]} [label = \"{quote(condition)}\"]");
            }

            writer.WriteLine("}");

            static string quote(string s)
            {
                return s.Replace("\"", "\\\"");
            }
        }

        #endregion Methods

        #region Private members

        #region Methods

        private (List<GraphBlock>, List<GraphBranch>) BuildGraph(BoundBlockStatement block)
        {
            var blocks = CreateGraphBlocks(block);
            var branches = new List<GraphBranch>();

            if (block.Statements.Length == 0)
            {
                ConnectBlocks(_start, _end);
                return (blocks, branches);
            }

            var labelTable = blocks.Select(block => (block, label: block.Statements.FirstOrDefault() as BoundLabelStatement))
                .Where(pair => pair.label is not null)
                .ToDictionary(pair => pair.label!.Label, pair => pair.block);

            for (int i = 0; i < blocks.Count - 1; i++)
            {
                var current = blocks[i];
                var next = blocks[i + 1];

                if (current.Statements.Count == 0)
                    ConnectBlocks(current, next);
                else
                {
                    for (int j = 0; j < current.Statements.Count; j++)
                    {
                        var statement = current.Statements[j];
                        var isLastStatememt = j == current.Statements.Count - 1;

                        switch (statement.Kind)
                        {
                            case BoundNodeKind.VariableDeclarationStatement:
                            case BoundNodeKind.NoOpStatement:
                            case BoundNodeKind.ExpressionStatement:
                            case BoundNodeKind.LabelStatement:
                                if (isLastStatememt)
                                    ConnectBlocks(current, next);
                                break;

                            case BoundNodeKind.GotoStatement:
                                var gs = (BoundGotoStatement)statement;
                                var gsBlock = labelTable[gs.Label];
                                ConnectBlocks(current, gsBlock);
                                break;

                            case BoundNodeKind.ConditionalGotoStatement:
                                var cgs = (BoundConditionalGotoStatement)statement;
                                var cgsBlock = labelTable[cgs.Label];

                                var condition = cgs.Condition;
                                var negatedCondition = NegateCondition(condition);

                                ConnectBlocks(current, cgsBlock, cgs.JumpIfFalse ? negatedCondition : condition);
                                ConnectBlocks(current, next, cgs.JumpIfFalse ? condition : negatedCondition);
                                break;

                            case BoundNodeKind.ReturnStatement:
                                ConnectBlocks(current, _end);
                                break;

                            default:
                                throw new Exception($"Unexpected statement kind '{statement.Kind}'.");
                        }
                    }
                }
            }

            ClearUnreachableBlocks(blocks, branches);
            return (blocks, branches);

            void ConnectBlocks(GraphBlock start, GraphBlock end, BoundExpression? condition = null)
            {
                var branch = new GraphBranch(start, end, condition);

                start.Outgoing.Add(branch);
                end.Incoming.Add(branch);
                branches.Add(branch);
            }

            BoundExpression NegateCondition(BoundExpression condition)
            {
                var negation = BoundUnaryOperator.Operators[(BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.Boolean)];
                return new BoundUnaryExpression(negation, condition);
            }
        }

        #endregion Methods

        #region Helpers

        private List<GraphBlock> CreateGraphBlocks(BoundBlockStatement block)
        {
            var blocks = new List<GraphBlock> { _start };

            var statements = new List<BoundStatement>();
            foreach (var statement in block.Statements)
            {
                switch (statement.Kind)
                {
                    case BoundNodeKind.VariableDeclarationStatement:
                    case BoundNodeKind.NoOpStatement:
                    case BoundNodeKind.ExpressionStatement:
                        statements.Add(statement);
                        break;

                    case BoundNodeKind.LabelStatement:
                        nextBlock();
                        statements.Add(statement);
                        break;

                    case BoundNodeKind.GotoStatement:
                    case BoundNodeKind.ConditionalGotoStatement:
                    case BoundNodeKind.ReturnStatement:
                        statements.Add(statement);
                        nextBlock();
                        break;

                    default:
                        throw new Exception($"Unexpected statement kind '{statement.Kind}'.");
                }
            }

            nextBlock();

            blocks.Add(_end);
            return blocks;

            void nextBlock()
            {
                if (statements.Count == 0)
                    return;

                var block = new GraphBlock();
                blocks.Add(block);

                block.Statements.AddRange(statements);
                statements.Clear();
            }
        }

        private static void ClearUnreachableBlocks(List<GraphBlock> blocks, List<GraphBranch> branches)
        {
            var removedBlocks = new HashSet<GraphBlock>();
            while (true)
            {
                foreach (var block in blocks)
                {
                    if (block.IsStart is true)
                        continue;

                    if (block.Incoming.Count == 0)
                    {
                        removedBlocks.Add(block);
                        foreach (var outgoingBranche in block.Outgoing)
                        {
                            outgoingBranche.To.Incoming.Remove(outgoingBranche);
                            branches.Remove(outgoingBranche);
                        }
                    }
                }
                if (removedBlocks.Count == 0)
                    break;

                blocks.RemoveAll(block => removedBlocks.Remove(block));
            }
        }

        #endregion Helpers

        #endregion Private members
    }
}