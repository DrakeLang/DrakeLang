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

        private static Compilation _currentState;
        private readonly static Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        private static void Main()
        {
            bool firstLine = true;
            StringBuilder input = new StringBuilder();

            while (true)
            {
                if (firstLine)
                    Console.Write("> ");
                else
                    Console.Write("| ");

                string line = Console.ReadLine();
                if (line != null && line.StartsWith('#'))
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
                    input.Append(line);
                    firstLine = false;
                }
                else
                {
                    if (input.Length > 0)
                    {
                        Parse(input.ToString());
                        input.Clear();
                    }

                    firstLine = true;
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

            if (result.Diagnostics.Count == 0)
            {
                Console.WriteLine(result.Value);
                _currentState = compilation;
            }
            else
            {
                SourceText text = syntaxTree.Text;

                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    int lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                    int lineNumer = lineIndex + 1;
                    int character = diagnostic.Span.Start - text.Lines[lineIndex].Start + 1;

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine();
                    Console.Write($"({lineNumer}, {character}): ");
                    Console.WriteLine(diagnostic);
                    Console.ResetColor();

                    string prefix = content.Substring(0, diagnostic.Span.Start);
                    string error = content.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
                    string suffix = content.Substring(diagnostic.Span.End);

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

        private static void PrintTreeToConsole(SyntaxTree tree)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            tree.PrintTree(Console.Out);

            Console.ResetColor();
        }
    }
}