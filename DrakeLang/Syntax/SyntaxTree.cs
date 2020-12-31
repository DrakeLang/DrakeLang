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

using DrakeLang.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace DrakeLang.Syntax
{
    public class SyntaxTree
    {
        private SyntaxTree(ImmutableArray<SourceText> source)
        {
            var diagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
            var compilationBuilder = ImmutableArray.CreateBuilder<CompilationUnitSyntax>();

            source.ForEach(text =>
            {
                var parser = new Parser(text);

                var compilation = parser.ParseCompilationUnit();

                diagnosticsBuilder.AddRange(parser.Diagnostics);
                compilationBuilder.Add(compilation);
            });

            Diagnostics = diagnosticsBuilder.ToImmutable();
            CompilationUnits = compilationBuilder.ToImmutable();
        }

        #region Properties

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<CompilationUnitSyntax> CompilationUnits { get; }

        #endregion Properties

        #region Methods

        public void PrintTree(TextWriter writer)
        {
            CompilationUnits.ForEach(compilation => compilation.WriteTo(writer));
        }

        #endregion Methods

        #region Statics

        public static SyntaxTree FromString(string text)
        {
            var sourceText = SourceText.FromString(text);
            return Parse(sourceText);
        }

        public static SyntaxTree FromFile(string sourceFile) => FromFiles(new[] { sourceFile });

        public static SyntaxTree FromFiles(params string[] sourceFiles) => FromFiles(sourceFiles.AsEnumerable());

        public static SyntaxTree FromFiles(IEnumerable<string> sourceFiles)
        {
            var sourceText = sourceFiles.Select(s => SourceText.FromFile(s));
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(params SourceText[] source) => Parse(source.ToImmutableArray());

        public static SyntaxTree Parse(IEnumerable<SourceText> source) => new SyntaxTree(source.ToImmutableArray());

        public static SyntaxTree Parse(ImmutableArray<SourceText> source) => new SyntaxTree(source);

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            var sourceText = SourceText.FromString(text);
            return ParseTokens(sourceText);
        }

        public static IEnumerable<SyntaxToken> ParseTokens(SourceText text)
        {
            var lexer = new Lexer(text);
            while (true)
            {
                var token = lexer.Lex();
                if (token.Kind == SyntaxKind.EndOfFileToken)
                    break;

                yield return token;
            }
        }

        #endregion Statics
    }
}