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

using CommandLine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using VSharp;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;

namespace VSharpO
{
    public class Options
    {
        [Option('t', "tree", Default = false, HelpText = "Show the syntax tree")]
        public bool ShowTree { get; set; }

        [Option('p', "program", Default = false, HelpText = "Show the program")]
        public bool ShowProgram { get; set; }

        [Option('s', "source", HelpText = "The path to the source to compile", Required = true)]
        public string Source { get; set; } = string.Empty;
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += delegate
            {
                Environment.Exit(0);
            };

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if (!File.Exists(o.Source))
                {
                    ConsoleExt.WriteLine($"Source '{o.Source}' does not exist.", ConsoleColor.Red);
                    return;
                }

                var code = File.ReadAllText(o.Source);
                Parse(code, o);
            });
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine($"Unhandled exception. " + ex, ConsoleColor.DarkRed);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Parses the given code.
        /// </summary>
        private static void Parse(string code, Options options)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(code);
            Compilation compilation = new Compilation(syntaxTree);

            if (options.ShowTree) PrintTreeToConsole(syntaxTree);
            if (options.ShowProgram) PrintProgramToConsole(compilation);

            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            if (result.Diagnostics.Length == 0)
            {
                ConsoleExt.WriteLine("Program executed successfully", ConsoleColor.Green);
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