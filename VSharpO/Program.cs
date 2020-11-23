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

using CommandLine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using VSharp;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;

namespace VSharpO
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += delegate
            {
                Environment.Exit(0);
            };
            try
            {
                new Parser(options =>
                {
                    options.AutoHelp = true;
                    options.AutoVersion = true;

                    options.CaseInsensitiveEnumValues = true;
                    options.CaseSensitive = false;
                    options.HelpWriter = Console.Out;
                }).ParseArguments<Options>(args)
                    .WithParsed(Run);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine($"Unhandled exception. " + ex, ConsoleColor.DarkRed);
                Console.ResetColor();
            }
        }

        private static void Run(Options o)
        {
            var sb = new StringBuilder();

            ReadSource(o.Source, sb);
            var code = sb.ToString();

            Parse(code, o);
        }

        private static void ReadSource(IEnumerable<string> source, StringBuilder sb)
        {
            foreach (var s in source)
            {
                if (Directory.Exists(s))
                {
                    ReadSource(Directory.GetDirectories(s), sb);
                    ReadSource(Directory.GetFiles(s), sb);
                }
                else if (File.Exists(s))
                {
                    var code = File.ReadAllText(s);
                    sb.Append(code);
                }
                else
                {
                    ConsoleExt.WriteLine($"Source '{s}' does not exist.", ConsoleColor.Red);
                }
            }
        }

        /// <summary>
        /// Parses the given code.
        /// </summary>
        private static void Parse(string code, Options options)
        {
            var syntaxTree = SyntaxTree.Parse(code);
            var compilation = new Compilation(syntaxTree);

            var debugOutput = options.GetAggregatedDebugValues();
            if (debugOutput.HasFlag(DebugOutput.ShowTree)) syntaxTree.PrintTree(Console.Out);
            if (debugOutput.HasFlag(DebugOutput.ShowProgram)) compilation.BindingResult.PrintProgram(Console.Out);
            if (debugOutput.HasFlag(DebugOutput.PrintControlFlowGraph)) PrintControlFlowGraph(compilation);

            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            if (result.Diagnostics.Length == 0)
            {
                ConsoleExt.WriteLine("Program executed successfully", ConsoleColor.Green);
            }
            else
            {
                HandleDiagonstics(syntaxTree.Text, result.Diagnostics);
            }
        }

        private static void PrintControlFlowGraph(Compilation compilation)
        {
            var appPath = typeof(Program).Assembly.Location;
            var workingDirectory = Path.GetDirectoryName(appPath);
            if (workingDirectory is null)
                throw new Exception("Failed to resolve working directory.");

            var outputDir = Path.Combine(workingDirectory, "ControlFlowGraphs");

            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, recursive: true);
            Directory.CreateDirectory(outputDir);

            int generatedGraphs = 0;
            compilation.BindingResult.GenerateControlFlowGraphs(methodName =>
            {
                generatedGraphs++;

                var fileName = $"{methodName}.dot";
                var filepath = Path.Combine(outputDir, fileName);

                return new StreamWriter(filepath);
            }, writer => writer.Dispose());

            ConsoleExt.Write($"Printed {generatedGraphs} control flow graph(s) to ", ConsoleColor.Green);
            ConsoleExt.Write(outputDir, ConsoleColor.Cyan);
            ConsoleExt.Write(".", ConsoleColor.Green);
            Console.WriteLine();
        }

        private static void HandleDiagonstics(SourceText text, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderBy(d => d.Span))
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
    }
}