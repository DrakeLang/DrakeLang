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
using System.Linq;

namespace VSharp.Binding
{
    internal sealed class ControlFlowGraph
    {
        private readonly GraphBlock _start = new GraphBlock();
        private readonly GraphBlock _end = new GraphBlock();

        public ControlFlowGraph(BoundBlockStatement block)
        {
            BuildGraph(block);
        }

        #region Methods

        public bool AllPathsReturn()
        {
            return _end.Incoming.All(branch => branch.From.Statements[^1].Kind == BoundNodeKind.ReturnStatement);
        }

        #endregion Methods

        #region Private members

        #region Methods

        private (List<GraphBlock>, List<GraphBranch>) BuildGraph(BoundBlockStatement block)
        {
            var branches = new List<GraphBranch>();
            if (block.Statements.Length == 0)
            {
                ConnectBlocks(_start, _end);
                return (new List<GraphBlock>(), branches);
            }

            var blocks = CreateGraphBlocks(block);
            var labelTable = blocks.Select(block => (block, label: block.Statements[0] as BoundLabelStatement))
                .Where(pair => pair.label is not null)
                .ToDictionary(pair => pair.label!.Label, pair => pair.block);

            for (int i = 0; i < blocks.Count; i++)
            {
                var current = blocks[i];
                var next = i < blocks.Count - 1 ? blocks[i + 1] : _end;

                foreach (var statement in current.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.NoOpStatement:
                        case BoundNodeKind.ExpressionStatement:
                            break;

                        case BoundNodeKind.LabelStatement:
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

                            ConnectBlocks(current, cgsBlock);
                            ConnectBlocks(current, next);
                            break;

                        case BoundNodeKind.ReturnStatement:
                            ConnectBlocks(current, _end);
                            break;

                        default:
                            throw new Exception($"Unexpected statement kind '{statement.Kind}'.");
                    }
                }
            }

            ClearUnreachableBlocks(blocks);
            return (blocks, branches);

            void ConnectBlocks(GraphBlock start, GraphBlock end)
            {
                var branch = new GraphBranch(start, end);

                start.Outgoing.Add(branch);
                end.Incoming.Add(branch);
                branches.Add(branch);
            }
        }

        private static void ClearUnreachableBlocks(List<GraphBlock> blocks)
        {
            var removedBlocks = new HashSet<GraphBlock>();
            while (true)
            {
                foreach (var block in blocks)
                {
                    if (block.Incoming.Count == 0)
                    {
                        removedBlocks.Add(block);
                        foreach (var outgoingBranches in block.Outgoing)
                        {
                            outgoingBranches.To.Incoming.Remove(outgoingBranches);
                        }
                    }
                }
                if (removedBlocks.Count == 0)
                    break;

                blocks.RemoveAll(block => removedBlocks.Remove(block));
            }
        }

        #endregion Methods

        #region Helpers

        private static List<GraphBlock> CreateGraphBlocks(BoundBlockStatement block)
        {
            var blocks = new List<GraphBlock>();

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

        #endregion Helpers

        #endregion Private members

        #region Utility classes

        private sealed class GraphBlock
        {
            public GraphBlock()
            {
            }

            #region Properties

            public HashSet<GraphBranch> Incoming { get; } = new HashSet<GraphBranch>();
            public HashSet<GraphBranch> Outgoing { get; } = new HashSet<GraphBranch>();

            public List<BoundStatement> Statements { get; } = new List<BoundStatement>();

            #endregion Properties
        }

        private sealed class GraphBranch
        {
            public GraphBranch(GraphBlock from, GraphBlock to)
            {
                From = from;
                To = to;
            }

            #region Properties

            public GraphBlock From { get; }
            public GraphBlock To { get; }

            #endregion Properties
        }

        #endregion Utility classes
    }
}