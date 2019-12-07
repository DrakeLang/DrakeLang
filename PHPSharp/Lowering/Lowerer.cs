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
using System.Collections.Immutable;

namespace PHPSharp.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private Lowerer()
        {
        }

        public static BoundStatement Lower(BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            return lowerer.RewriteStatement(statement);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            /**
             * Initial form:
             * for (<init>; <condition>; <update>)
             * {
             *    <body>
             * }
             *
             * --------
             *
             * Lowered form:
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
    }
}