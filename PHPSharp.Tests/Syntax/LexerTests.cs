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

using PHPSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PHPSharp.Tests.Syntax
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
                                   .Select(k => (kind: k, text: SyntaxFacts.GetText(k)))
                                   .Where(t => t.text != null);

            var dynamicTokens = new[]
            {
                (SyntaxKind.NumberToken, "1"),
                (SyntaxKind.NumberToken, "123"),
                (SyntaxKind.IdentifierToken, "a"),
                (SyntaxKind.IdentifierToken, "abc"),
                (SyntaxKind.StringToken, "\"a\""),
                (SyntaxKind.StringToken, "\"JOIN THE ASCENDENCY!\""),
            };

            // We know that fixedTokens contains no null-reference strings.
            return fixedTokens!.Concat(dynamicTokens);
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceToken, " "),
                (SyntaxKind.WhitespaceToken, "  "),
                (SyntaxKind.WhitespaceToken, "\r"),
                (SyntaxKind.WhitespaceToken, "\n"),
                (SyntaxKind.WhitespaceToken, "\r\n"),
            };
        }

        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            bool t1IsKeyword = t1Kind.ToString().EndsWith("Keyword");
            bool t2IsKeyword = t2Kind.ToString().EndsWith("Keyword");

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1IsKeyword && t2IsKeyword)
                return true;

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
                return true;

            if (t1Kind == SyntaxKind.NumberToken && t2Kind == SyntaxKind.NumberToken)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusToken)
                return true;

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusPlusToken)
                return true;

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusToken)
                return true;

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusMinusToken)
                return true;

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandEqualsToken)
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
                        foreach ((SyntaxKind separatorKind, string separatorText) in GetSeparators())
                            yield return (t1Kind, t1Text, separatorKind, separatorText, t2Kind, t2Text);
                    }
                }
            }
        }
    }
}