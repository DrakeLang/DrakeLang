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

using VSharp.Text;
using System.Collections.Generic;
using System.IO;

namespace VSharp.Syntax
{
    public class SyntaxTree
    {
        private SyntaxTree(SourceText text)
        {
            Parser parser = new Parser(text);

            Text = text;
            Root = parser.ParseCompilationUnit();
            Diagnostics = parser.GetDiagnostics();
        }

        #region Properties

        public SourceText Text { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        #endregion Properties

        #region Methods

        public void PrintTree(TextWriter writer)
        {
            Root.WriteTo(writer);
        }

        #endregion Methods

        #region Statics

        public static SyntaxTree Parse(string text)
        {
            var sourceText = SourceText.From(text);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text);
        }

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            var sourceText = SourceText.From(text);
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