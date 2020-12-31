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
using DrakeLang.Syntax;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DrakeLang.Utils
{
    internal static class TreePrintingUtil
    {
        public static void WriteClr(this TextWriter writer, object? value, ConsoleColor color)
        {
            if (writer == Console.Out)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                writer.Write(value);

                Console.ForegroundColor = oldColor;
            }
            else
            {
                writer.Write(value);
            }
        }

        private static readonly Regex _syntaxKindRegex = new Regex("([A-Za-z]*)(Expression|Statement|Token|Keyword)");

        public static void WriteSyntaxKind(this TextWriter writer, SyntaxKind kind)
        {
            if (writer != Console.Out)
            {
                writer.Write(kind);
                return;
            }

            var kindStr = kind.ToString();
            var match = _syntaxKindRegex.Match(kindStr);
            if (!match.Success)
            {
                writer.WriteClr(kindStr, ConsoleColor.White);
                return;
            }

            writer.WriteClr(match.Groups[1], ConsoleColor.White);
            writer.WriteClr(match.Groups[2], ConsoleColor.DarkGray);
        }

        private static readonly Regex _boundNodeKindRegex = new Regex("([A-Za-z]*)(Expression|Statement)");

        public static void WriteSyntaxKind(this TextWriter writer, BoundNodeKind kind)
        {
            if (writer != Console.Out)
            {
                writer.Write(kind);
                return;
            }

            var kindStr = kind.ToString();
            var match = _boundNodeKindRegex.Match(kindStr);
            if (!match.Success)
            {
                writer.WriteClr(kindStr, ConsoleColor.White);
                return;
            }

            writer.WriteClr(match.Groups[1], ConsoleColor.White);
            writer.WriteClr(match.Groups[2], ConsoleColor.DarkGray);
        }

        public static void WriteType(this TextWriter writer, TypeSymbol type)
        {
            writer.WriteClr(type, ConsoleColor.Red);
        }

        public static void WriteMethod(this TextWriter writer, MethodSymbol method, bool printParamNames)
        {
            writer.WriteType(method.ReturnType);
            writer.Write(" ");
            writer.WriteClr(method.FullName, ConsoleColor.Cyan);
            writer.WriteClr('(', ConsoleColor.White);
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var param = method.Parameters[i];
                var isLastParam = i == method.Parameters.Length - 1;

                writer.WriteType(param.Type);

                if (printParamNames)
                {
                    writer.Write(" ");
                    writer.WriteClr(param.Name, ConsoleColor.Yellow);
                }

                if (!isLastParam)
                    writer.WriteClr(", ", ConsoleColor.White);
            }
            writer.WriteClr(')', ConsoleColor.White);
        }

        public static void WriteVariable(this TextWriter writer, VariableSymbol variable, bool printType)
        {
            if (printType)
            {
                writer.WriteType(variable.Type);
                writer.Write(" ");
            }

            writer.WriteClr(variable.Name, ConsoleColor.Cyan);
        }

        public static void WriteLabel(this TextWriter writer, LabelSymbol label)
        {
            writer.WriteClr(label.Name, ConsoleColor.White);
        }

        public static void WriteOneLineExpression(this TextWriter writer, BoundExpression expression)
        {
            if (expression is BoundLiteralExpression literalSize)
                writer.WriteClr(literalSize.Value, ConsoleColor.Magenta);
            else
                writer.WriteClr(expression.ToFriendlyString(), ConsoleColor.Cyan);
        }
    }
}