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

using System;
using System.Collections.Generic;
using System.Linq;
using VSharp.Syntax;
using Xunit;

namespace VSharp.Tests.Syntax
{
    public class LexerTests
    {
        [Fact]
        public void Lexer_Lexes_AllTokens()
        {
            var tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                                   .Cast<SyntaxKind>()
                                   .Where(k => k.ToString().EndsWith("Keyword") ||
                                               k.ToString().EndsWith("Token"));

            IEnumerable<SyntaxKind> testedTokenKinds = GetTokens().Concat(GetSeparators()).Select(t => t.kind);
            var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
            untestedTokenKinds.ExceptWith(testedTokenKinds);

            Assert.Empty(untestedTokenKinds);
        }

        [Theory]
        [MemberData(nameof(GetTokensData))]
        public void Lexer_Lexes_Token(SyntaxKind kind, string text)
        {
            IEnumerable<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);

            SyntaxToken token = Assert.Single(tokens);

            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void Lexer_Lexes_TokenPairs(SyntaxKind t1kind, string t1text,
                                           SyntaxKind t2kind, string t2text)
        {
            string text = t1text + t2text;

            SyntaxToken[] tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);

            Assert.Equal(t1kind, tokens[0].Kind);
            Assert.Equal(t1text, tokens[0].Text);

            Assert.Equal(t2kind, tokens[1].Kind);
            Assert.Equal(t2text, tokens[1].Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsWithSeparatorData))]
        public void Lexer_Lexes_TokenPairsWithSeparator(SyntaxKind t1kind, string t1text,
                                                        SyntaxKind separatorKind, string separatorText,
                                                        SyntaxKind t2kind, string t2text)
        {
            string text = t1text + separatorText + t2text;

            SyntaxToken[] tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(3, tokens.Length);

            Assert.Equal(t1kind, tokens[0].Kind);
            Assert.Equal(t1text, tokens[0].Text);

            Assert.Equal(separatorKind, tokens[1].Kind);
            Assert.Equal(separatorText, tokens[1].Text);

            Assert.Equal(t2kind, tokens[2].Kind);
            Assert.Equal(t2text, tokens[2].Text);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            foreach ((SyntaxKind kind, string text) in GetTokens().Concat(GetSeparators()))
                yield return new object[] { kind, text };
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            foreach ((SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text) in GetTokenPairs())
                yield return new object[] { t1Kind, t1Text, t2Kind, t2Text };
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            foreach ((SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text) in GetTokenPairsWithSeparator())
                yield return new object[] { t1Kind, t1Text, separatorKind, separatorText, t2Kind, t2Text };
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxKind))
                                   .Cast<SyntaxKind>()
                                   .Select(k => (kind: k, text: k.GetText()))
                                   .Where(t => t.text != null)
                                   .Cast<(SyntaxKind, string)>();

            var dynamicTokens = new[]
            {
                (SyntaxKind.IntegerToken, "1"),
                (SyntaxKind.IntegerToken, "123"),
                (SyntaxKind.FloatToken, "4f"),
                (SyntaxKind.FloatToken, "1.1"),
                (SyntaxKind.FloatToken, "0.1"),
                (SyntaxKind.FloatToken, ".4"),
                (SyntaxKind.FloatToken, ".4f"),
                (SyntaxKind.IdentifierToken, "a"),
                (SyntaxKind.IdentifierToken, "abc"),
                (SyntaxKind.StringToken, "\"a\""),
                (SyntaxKind.StringToken, "\"JOIN THE ASCENDENCY!\""),
                (SyntaxKind.LineCommentToken, "//"),
                (SyntaxKind.LineCommentToken, "// "),
                (SyntaxKind.LineCommentToken, "// comment"),
                (SyntaxKind.LineCommentToken, "///////"),
                (SyntaxKind.LineCommentToken, "///* */"),
                (SyntaxKind.MultiLineCommentToken, "/* */"),
                (SyntaxKind.MultiLineCommentToken, "/* /////// */"),
                (SyntaxKind.MultiLineCommentToken, "/* \n\n */"),
            };

            return fixedTokens.Concat(dynamicTokens);
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetSeparators(bool linebreaksOnly = false)
        {
            if (!linebreaksOnly)
            {
                yield return (SyntaxKind.WhitespaceToken, " ");
                yield return (SyntaxKind.WhitespaceToken, "  ");
            }

            yield return (SyntaxKind.WhitespaceToken, "\r");
            yield return (SyntaxKind.WhitespaceToken, "\n");
            yield return (SyntaxKind.WhitespaceToken, "\r\n");
        }

        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            bool t1IsKeyword = t1Kind.ToString().EndsWith("Keyword");
            bool t2IsKeyword = t2Kind.ToString().EndsWith("Keyword");
            if (t1IsKeyword && t2IsKeyword)
                return true;

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
                return true;

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.IntegerToken)
            {
                if (t2Kind == SyntaxKind.IntegerToken) return true;
                if (t2Kind == SyntaxKind.FloatToken) return true;

                if (t2Kind == SyntaxKind.ForKeyword) return true; // the 'f' is mistaken for the float specifier
                if (t2Kind == SyntaxKind.FloatKeyword) return true; // the 'f' is mistaken for the float specifier
                if (t2Kind == SyntaxKind.FalseKeyword) return true; // the 'f' is mistaken for the float specifier
            }

            if (t1Kind == SyntaxKind.FloatToken)
            {
                if (t2Kind == SyntaxKind.IntegerToken) return true;
                if (t2Kind == SyntaxKind.FloatToken) return true;

                if (t2Kind == SyntaxKind.ForKeyword) return true; // the 'f' is mistaken for the float specifier
                if (t2Kind == SyntaxKind.FloatKeyword) return true; // the 'f' is mistaken for the float specifier
                if (t2Kind == SyntaxKind.FalseKeyword) return true; // the 'f' is mistaken for the float specifier
            }

            if (t1Kind == SyntaxKind.BangToken)
            {
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.EqualsToken)
            {
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.GreaterEqualsToken) return true;
                if (t2Kind == SyntaxKind.GreaterToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.LessToken)
            {
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.GreaterToken)
            {
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.PlusToken)
            {
                if (t2Kind == SyntaxKind.PlusToken) return true;
                if (t2Kind == SyntaxKind.PlusPlusToken) return true;
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.PlusEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.MinusToken)
            {
                if (t2Kind == SyntaxKind.MinusToken) return true;
                if (t2Kind == SyntaxKind.MinusMinusToken) return true;
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.MinusEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.StarToken)
            {
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.SlashToken)
            {
                if (t2Kind == SyntaxKind.SlashToken) return true;
                if (t2Kind == SyntaxKind.StarToken) return true;
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.SlashEqualsToken) return true;
                if (t2Kind == SyntaxKind.StarEqualsToken) return true;
                if (t2Kind == SyntaxKind.LineCommentToken) return true;
                if (t2Kind == SyntaxKind.MultiLineCommentToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.PipeToken)
            {
                if (t2Kind == SyntaxKind.PipeToken) return true;
                if (t2Kind == SyntaxKind.PipePipeToken) return true;
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.PipeEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken)
            {
                if (t2Kind == SyntaxKind.AmpersandToken) return true;
                if (t2Kind == SyntaxKind.AmpersandAmpersandToken) return true;
                if (t2Kind == SyntaxKind.EqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualsEqualsToken) return true;
                if (t2Kind == SyntaxKind.AmpersandEqualsToken) return true;
                if (t2Kind == SyntaxKind.EqualGreaterToken) return true;
            }

            return RequiresLinebreak(t1Kind, t2Kind);
        }

        private static bool RequiresLinebreak(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            if (t1Kind == SyntaxKind.LineCommentToken)
                return true;

            return false;
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
        {
            foreach ((SyntaxKind kind1, string text1) in GetTokens())
                foreach ((SyntaxKind kind2, string text2) in GetTokens())
                {
                    if (RequiresSeparator(kind1, kind2))
                        continue;

                    yield return (kind1, text1, kind2, text2);
                }
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text,
                                    SyntaxKind separatorKind, string separatorText,
                                    SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
        {
            foreach ((SyntaxKind t1Kind, string t1Text) in GetTokens())
            {
                foreach ((SyntaxKind t2Kind, string t2Text) in GetTokens())
                {
                    if (RequiresSeparator(t1Kind, t2Kind))
                    {
                        bool requiresLinebreak = RequiresLinebreak(t1Kind, t2Kind);
                        foreach ((SyntaxKind separatorKind, string separatorText) in GetSeparators(requiresLinebreak))
                        {
                            yield return (t1Kind, t1Text, separatorKind, separatorText, t2Kind, t2Text);
                        }
                    }
                }
            }
        }
    }
}