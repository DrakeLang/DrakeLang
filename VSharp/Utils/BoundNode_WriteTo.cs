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
using System.IO;
using System.Linq;
using VSharp.Utils;

namespace VSharp.Binding
{
    internal static class BoundNode_WriteTo
    {
        #region WriteTo

        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            if (writer is null) throw new ArgumentNullException(nameof(writer));

            node.WriteTo(new WriteContext(writer));
        }

        private static void WriteTo(this BoundNode node, WriteContext context)
        {
            var marker = context.IsLast ? "└──" : "├──";
            context.Writer.WriteClr(context.Indent, ConsoleColor.DarkGray);
            context.Writer.WriteClr(marker, ConsoleColor.DarkGray);

            WriteNode(node, context);
        }

        #endregion WriteTo

        #region WriteNode

        private static void WriteNode(BoundNode node, WriteContext context)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.VariableDeclarationStatement:
                    WriteVariableDeclarationStatement((BoundVariableDeclarationStatement)node, context);
                    break;

                case BoundNodeKind.MethodDeclarationStatement:
                    WriteMethodDeclarationStatement((BoundMethodDeclarationStatement)node, context);
                    break;

                case BoundNodeKind.LabelStatement:
                    WriteLabelStatement((BoundLabelStatement)node, context);
                    break;

                case BoundNodeKind.GotoStatement:
                    WriteGotoStatement((BoundGotoStatement)node, context);
                    break;

                case BoundNodeKind.ConditionalGotoStatement:
                    WriteConditionalGotoStatement((BoundConditionalGotoStatement)node, context);
                    break;

                case BoundNodeKind.ReturnStatement:
                    WriteReturnStatement((BoundReturnStatement)node, context);
                    break;

                case BoundNodeKind.NoOpStatement:
                    WriteNoOpStatement((BoundNoOpStatement)node, context);
                    break;

                case BoundNodeKind.ExpressionStatement:
                    WriteExpressionStatement((BoundExpressionStatement)node, context);
                    break;

                case BoundNodeKind.ErrorExpression:
                    WriteErrorExpression((BoundErrorExpression)node, context);
                    break;

                case BoundNodeKind.LiteralExpression:
                    WriteLiteralExpression((BoundLiteralExpression)node, context);
                    break;

                case BoundNodeKind.VariableExpression:
                    WriteVariableExpression((BoundVariableExpression)node, context);
                    break;

                case BoundNodeKind.AssignmentExpression:
                    WriteAssignmentExpression((BoundAssignmentExpression)node, context);
                    break;

                case BoundNodeKind.UnaryExpression:
                    WriteUnaryExpression((BoundUnaryExpression)node, context);
                    break;

                case BoundNodeKind.BinaryExpression:
                    WriteBinaryExpression((BoundBinaryExpression)node, context);
                    break;

                case BoundNodeKind.CallExpression:
                    WriteCallExpression((BoundCallExpression)node, context);
                    break;

                case BoundNodeKind.ExplicitCastExpression:
                    WriteExplicitCastExpression((BoundExplicitCastExpression)node, context);
                    break;

                default:
                    throw new Exception($"Unexpected node '{node.Kind}'.");
            }
        }

        private static void WriteVariableDeclarationStatement(BoundVariableDeclarationStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteVariable(node.Variable, printType: true);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteMethodDeclarationStatement(BoundMethodDeclarationStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteMethod(node.Method, printParamNames: true);

            context.Writer.WriteLine();

            PrintChildren(node.Declaration, context);
        }

        private static void WriteLabelStatement(BoundLabelStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteLabel(node.Label);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteGotoStatement(BoundGotoStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteLabel(node.Label);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteConditionalGotoStatement(BoundConditionalGotoStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteLabel(node.Label);
            context.Writer.WriteClr(node.JumpIfFalse ? " on false" : " on true", ConsoleColor.Cyan);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteReturnStatement(BoundReturnStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteNoOpStatement(BoundNoOpStatement node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, WriteContext context)
        {
            WriteNode(node.Expression, context);
        }

        private static void WriteErrorExpression(BoundErrorExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteType(node.Type);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteType(node.Type);
            context.Writer.WriteClr(": ", ConsoleColor.Cyan);
            if (node.Value is string strValue)
                context.Writer.WriteClr("\"" + strValue + "\"", ConsoleColor.Magenta);
            else
                context.Writer.WriteClr(node.Value, ConsoleColor.Magenta);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteVariableExpression(BoundVariableExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteVariable(node.Variable, printType: false);
            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteAssignmentExpression(BoundAssignmentExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteVariable(node.Variable, printType: false);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteUnaryExpression(BoundUnaryExpression node, WriteContext context)
        {
            context.Writer.WriteClr(node.Op.Kind, ConsoleColor.White);
            context.Writer.WriteClr("Expression", ConsoleColor.DarkGray);
            context.Writer.WriteClr(" returns ", ConsoleColor.Cyan);
            context.Writer.WriteType(node.Type);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, WriteContext context)
        {
            context.Writer.WriteClr(node.Op.Kind, ConsoleColor.White);
            context.Writer.WriteClr("Expression", ConsoleColor.DarkGray);
            context.Writer.WriteClr(" returns ", ConsoleColor.Cyan);
            context.Writer.WriteType(node.Type);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteCallExpression(BoundCallExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteMethod(node.Method, printParamNames: false);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        private static void WriteExplicitCastExpression(BoundExplicitCastExpression node, WriteContext context)
        {
            context.Writer.WriteSyntaxKind(node.Kind);
            context.Writer.Write(" ");
            context.Writer.WriteType(node.Type);

            context.Writer.WriteLine();

            PrintChildren(node, context);
        }

        #endregion WriteNode

        #region WriteHelpers

        private static void PrintChildren(BoundNode node, WriteContext context)
        {
            var lastChild = node.GetChildren().LastOrDefault();
            foreach (var child in node.GetChildren())
            {
                child.WriteTo(context with
                {
                    Indent = context.Indent + (context.IsLast ? "   " : "│  "),
                    IsLast = child == lastChild,
                });
            }
        }

        #endregion WriteHelpers

        #region Classes

        private sealed record WriteContext(TextWriter Writer, string Indent = "", bool IsLast = true);

        #endregion Classes
    }
}