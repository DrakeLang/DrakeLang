﻿//------------------------------------------------------------------------------
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

using PHPSharp;
using PHPSharp.Syntax;
using PHPSharp.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace PHPSharpO
{
    internal class Program
    {
        private static bool _showTree = false;

        private static Compilation? _currentState;
        private readonly static Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        private static void Main()
        {
            StringBuilder input = new StringBuilder();

            while (true)
            {
                string line = ReadInput(firstLine: input.Length == 0);
                if (line.StartsWith('#'))
                {
                    switch (line.Substring(1))
                    {
                        case "tree":
                            _showTree ^= true;
                            Console.WriteLine(_showTree ? "Parse tree visible" : "Parse tree hidden");
                            break;

                        case "cls":
                        case "clear":
                            Console.Clear();
                            break;

                        case "reset":
                            _currentState = null;
                            _variables.Clear();
                            break;

                        case "exit":
                            return;

                        default:
                            Console.WriteLine("Invalid command.");
                            break;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    input.AppendLine(line);
                }
                else if (input.Length > 0)
                {
                    Parse(input.ToString());
                    input.Clear();
                }
            }
        }

        /// <summary>
        /// Parses a string.
        /// </summary>
        /// <param name="content">The string to parse.</param>
        private static void Parse(string content)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(content);

            if (_showTree)
                PrintTreeToConsole(syntaxTree);

            Compilation compilation = _currentState?.ContinueWith(syntaxTree) ?? new Compilation(syntaxTree);
            EvaluationResult result = compilation.Evaluate(_variables);

            if (result.Diagnostics.Length == 0)
            {
                PrintResult(result.Value);
                _currentState = compilation;
            }
            else
            {
                SourceText text = syntaxTree.Text;

                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    int lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                    TextLine line = text.Lines[lineIndex];
                    int lineNumer = lineIndex + 1;
                    int character = diagnostic.Span.Start - line.Start + 1;

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine();
                    Console.Write($"({lineNumer}, {character}): ");
                    Console.WriteLine(diagnostic);
                    Console.ResetColor();

                    TextSpan prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
                    TextSpan suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

                    string prefix = text.ToString(prefixSpan);
                    string error = text.ToString(diagnostic.Span);
                    string suffix = text.ToString(suffixSpan);

                    Console.Write("    ");
                    Console.Write(prefix);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(error);
                    Console.ResetColor();

                    Console.Write(suffix);

                    Console.WriteLine();
                }

                Console.WriteLine();
            }
        }

        #region Console helpers

        private static string ReadInput(bool firstLine)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            if (firstLine)
                Console.Write("> ");
            else
                Console.Write("| ");

            string input = Console.ReadLine();
            Console.ResetColor();

            return input;
        }

        private static void PrintResult(object? result)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.WriteLine(result);

            Console.ResetColor();
        }

        private static void PrintTreeToConsole(SyntaxTree tree)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            tree.PrintTree(Console.Out);

            Console.ResetColor();
        }

        #endregion Console helpers
    }
}