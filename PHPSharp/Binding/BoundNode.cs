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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PHPSharp.Binding
{
    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }

        #region Methods

        public abstract IEnumerable<BoundNode> GetChildren();

        public void WriteTo(TextWriter writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            PrintTree(writer, this);
        }

        public override string ToString()
        {
            using StringWriter writer = new StringWriter();

            WriteTo(writer);
            return writer.ToString();
        }

        #endregion Methods

        #region Private static methods

        private static void PrintTree(TextWriter write, BoundNode node, string indent = "", bool isLast = true)
        {
            write.Write(indent);
            write.Write(isLast ? "└──" : "├──");
            WriteNode(write, node);
            write.WriteLine();

            indent += isLast ? "   " : "│  ";
            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrintTree(write, child, indent, child == lastChild);
        }

        private static void WriteNode(TextWriter writer, BoundNode node)
        {
            string text = GetText(node);
            writer.Write(text);

            static string GetText(BoundNode node)
            {
                return node switch
                {
                    BoundVariableDeclarationStatement v => v.Kind.ToString() + " " + v.Variable.Type + " " + v.Variable.Name,
                    BoundVariableExpression v => v.Kind.ToString() + " " + v.Variable.Name,
                    BoundLiteralExpression l => l.Kind.ToString() + " " + l.Value,

                    BoundBinaryExpression b => b.Op.Kind.ToString() + "Expression " + b.Type,
                    BoundUnaryExpression u => u.Op.Kind.ToString() + "Expression " + u.Type,
                    BoundLabelStatement l => l.Kind.ToString() + " " + l.Label.Name,
                    BoundGotoStatement g => g.Kind.ToString() + " " + g.Label.Name,
                    BoundConditionalGotoStatement g => g.Kind.ToString() + " " + g.Label.Name + (g.JumpIfFalse ? " on false" : " on true"),

                    _ => node.Kind.ToString(),
                };
            }
        }

        #endregion Private static methods
    }
}