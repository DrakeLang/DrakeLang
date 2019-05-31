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
using PHPSharp.Binding;
using PHPSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PHPSharpO
{
    internal class Program
    {
        private static void Main()
        {
            bool showTree = true;
            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();

                if (File.Exists(line))
                    ParseFile(line, showTree);
                else
                {
                    switch (line)
                    {
                        case "":
                            string filepath = @"C:\users\Niklas\documents\example.phps";
                            Console.WriteLine("No file given. Defaults to {0}", filepath);
                            ParseFile(filepath, showTree);
                            break;

                        case "tree":
                            showTree ^= true;
                            Console.WriteLine(showTree ? "Showing parse tree" : "Hiding parse trees");
                            break;

                        case "cls":
                            Console.Clear();
                            break;

                        case "exit":
                            return;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a file.
        /// </summary>
        /// <param name="path">The path of the file to parse.</param>
        private static void ParseFile(string path, bool showTree)
        {
            string content = File.ReadAllText(path);
            SyntaxTree syntaxTree = SyntaxTree.Parse(content);
            Binder binder = new Binder();
            BoundExpression boundExpression = binder.BindExpression(syntaxTree.Root);

            IEnumerable<string> diagnostics = syntaxTree.Diagnostics.Concat(binder.Diagnostics);

            if (showTree)
                syntaxTree.PrintTree(ConsoleColor.DarkGray);

            if (!diagnostics.Any())
            {
                Evaluator evaluator = new Evaluator(boundExpression);
                string result = evaluator.Evaluate();

                Console.WriteLine(result);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;

                foreach (string diagnostic in diagnostics)
                    Console.WriteLine(diagnostic);

                Console.ResetColor();
            }
        }
    }
}