//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
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

using VSharp;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace VSharpO
{
    internal static class Program
    {
        private const string ProgramPath = "Program.ps";

        private static bool _shutDownInitiated;

        private static bool _showTree = false;
        private static bool _showProgram = false;

        private static Compilation? _currentState;
        private readonly static Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        private static void Main()
        {
            _showProgram = true;

            var code = File.ReadAllText(ProgramPath);
            Parse(code);

            Console.CancelKeyPress += delegate
            {
                Environment.Exit(0);
            };

            while (true)
            {
                Console.ReadKey(intercept: true);
            }
        }

        private static bool TryEvaluateSharpOCommand(string input)
        {
            if (!input.StartsWith('#'))
                return false;

            switch (input.Substring(1))
            {
                case "tree":
                    _showTree ^= true;
                    Console.WriteLine(_showTree ? "Parse tree visible" : "Parse tree hidden");
                    break;

                case "program":
                    _showProgram ^= true;
                    Console.WriteLine(_showProgram ? "Program visible" : "Program hidden");
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
                    _shutDownInitiated = true;
                    break;

                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }

            return true;
        }

        /// <summary>
        /// Parses the given code.
        /// </summary>
        private static void Parse(string code)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(code);
            Compilation compilation = _currentState?.ContinueWith(syntaxTree) ?? new Compilation(syntaxTree);

            if (_showTree) PrintTreeToConsole(syntaxTree);
            if (_showProgram) PrintProgramToConsole(compilation);

            EvaluationResult result = compilation.Evaluate(_variables);
            if (result.Diagnostics.Length == 0)
            {
                PrintResult(result.Value);
                _currentState = compilation;
            }
            else
            {
                HandleDiagonstics(syntaxTree.Text, result.Diagnostics);
            }
        }

        private static void HandleDiagonstics(SourceText text, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                int lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                TextLine line = text.Lines[lineIndex];
                int lineNumer = lineIndex + 1;
                int character = diagnostic.Span.Start - line.Start + 1;

                Console.WriteLine();
                ConsoleExt.WriteLine($"({lineNumer}, {character}): {diagnostic}", ConsoleColor.DarkRed);

                TextSpan prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
                TextSpan suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

                string prefix = text.ToString(prefixSpan);
                string error = text.ToString(diagnostic.Span);
                string suffix = text.ToString(suffixSpan);

                Console.Write("    ");
                Console.Write(prefix);

                ConsoleExt.Write(error, ConsoleColor.DarkRed);

                Console.Write(suffix);
                Console.WriteLine();
            }

            Console.WriteLine();
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

        private static void PrintProgramToConsole(Compilation compilation)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            compilation.PrintProgram(Console.Out);

            Console.ResetColor();
        }

        #endregion Console helpers
    }
}