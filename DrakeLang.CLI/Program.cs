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

using CommandLine;
using DrakeLang;
using DrakeLang.Symbols;
using DrakeLang.Syntax;
using DrakeLang.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Drake
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
                using var parser = new Parser(options =>
                {
                    options.AutoHelp = true;
                    options.AutoVersion = true;

                    options.CaseInsensitiveEnumValues = true;
                    options.CaseSensitive = false;
                    options.HelpWriter = Console.Out;
                });

                parser.ParseArguments<Options>(args)
                    .WithParsed(Run);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"Unhandled exception.", ex);
                Console.ResetColor();
            }
        }

        private static void Run(Options o)
        {
            if (string.IsNullOrEmpty(o.Project) || !File.Exists(o.Project))
            {
                ConsoleExt.WriteError($"Path '{o.Project}' is not a valid project.");
                return;
            }

            var project = Path.GetFullPath(o.Project);
            var source = ReadSource(project);
            if (source is null)
                return; // error has already been reported.

            ParseAndEvaluate(source, o);
        }

        private static string[]? ReadSource(string project)
        {
            var sourceFiles = new List<string>();
            var sourceDirectories = new Stack<string>();

            var projectDir = Path.GetDirectoryName(project)!;
            sourceDirectories.Push(projectDir);

            while (sourceDirectories.Count > 0)
            {
                var currentDir = sourceDirectories.Pop();

                Directory.GetFiles(currentDir).Where(f => Path.GetExtension(f) == Globals.SourceFileExtension).ForEach(file => sourceFiles.Add(file));
                Directory.GetDirectories(currentDir).ForEach(dir => sourceDirectories.Push(dir));
            }

            return sourceFiles.ToArray();
        }

        private static void ParseAndEvaluate(string[] source, Options options)
        {
            var syntaxTree = SyntaxTree.FromFiles(source);
            var compilation = new Compilation(syntaxTree);

            var debugOutput = options.GetAggregatedDebugValues();
            if (debugOutput.HasFlag(DebugOutput.ShowTree)) syntaxTree.PrintTree(Console.Out);
            if (debugOutput.HasFlag(DebugOutput.ShowProgram)) compilation.BindingResult.PrintProgram(Console.Out);
            if (debugOutput.HasFlag(DebugOutput.PrintControlFlowGraph)) PrintControlFlowGraph(compilation);

            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);
            if (!result.Diagnostics.IsEmpty)
            {
                HandleDiagonstics(result.Diagnostics);
                return;
            }

            ConsoleExt.WriteLine("Program executed successfully", ConsoleColor.Green);
        }

        private static readonly ImmutableHashSet<char> _invalidChars = Path.GetInvalidFileNameChars().ToImmutableHashSet();

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

                var fileName = $"{sanitizeFilename(methodName)}.dot";
                var filepath = Path.Combine(outputDir, fileName);

                return new StreamWriter(filepath);
            }, writer => writer.Dispose());

            ConsoleExt.Write($"Printed {generatedGraphs} control flow graph(s) to ", ConsoleColor.Green);
            ConsoleExt.Write(outputDir, ConsoleColor.Cyan);
            ConsoleExt.Write(".", ConsoleColor.Green);
            Console.WriteLine();

            static string sanitizeFilename(string filename)
            {
                var rawFileName = filename.ToCharArray();
                var hasIllegalChar = false;
                for (int i = 0; i < rawFileName.Length; i++)
                {
                    if (!_invalidChars.Contains(rawFileName[i]))
                        continue;

                    hasIllegalChar = true;
                    rawFileName[i] = '_';
                }

                return hasIllegalChar
                    ? new string(rawFileName) + Guid.NewGuid()
                    : filename;
            }
        }

        private static void HandleDiagonstics(ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics.OrderBy(d => d.Span).Distinct(new SpanComparer()))
            {
                var text = diagnostic.Text;
                var span = diagnostic.Span;

                Console.WriteLine();
                ConsoleExt.WriteLine(diagnostic.ToString(), ConsoleExt.ErrorColor);

                if (text is null)
                    continue;

                int lineIndex = text.GetLineIndex(span.Start);
                var line = text.Lines[lineIndex];

                TextSpan prefixSpan = TextSpan.FromBounds(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.FromBounds(span.End, line.End);

                var prefixText = text.ToString(prefixSpan);
                var offendingText = text.ToString(span);
                var suffixText = text.ToString(suffixSpan);

                Console.Write("    ");
                Console.Write(prefixText);

                ConsoleExt.Write(offendingText, ConsoleExt.ErrorColor);

                Console.Write(suffixText);
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        private class SpanComparer : IEqualityComparer<Diagnostic>
        {
            public bool Equals(Diagnostic? x, Diagnostic? y)
            {
                return x?.Span == y?.Span;
            }

            public int GetHashCode(Diagnostic obj) => obj.Span.GetHashCode();
        }
    }
}