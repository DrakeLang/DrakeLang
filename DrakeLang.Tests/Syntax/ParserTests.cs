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

using System.Collections.Generic;
using DrakeLang.Syntax;
using Xunit;

namespace DrakeLang.Tests.Syntax
{
    public class ParserTests
    {
        [Theory]
        [MemberData(nameof(GetBinaryOperatorPairsData))]
        public void Parser_BinaryExpression_HonorsPrecedences(SyntaxKind op1, SyntaxKind op2)
        {
            int op1Precedence = op1.GetBinaryOperatorPrecedence();
            int op2Precedence = op2.GetBinaryOperatorPrecedence();

            string? op1Text = op1.GetText();
            string? op2Text = op2.GetText();
            string text = $"a {op1Text} b {op2Text} c";

            ExpressionSyntax expression = ParseExpression(text);

            if (op1Precedence >= op2Precedence)
            {
                using AssertingEnumerator e = new AssertingEnumerator(expression);

                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(op1, op1Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertToken(op2, op2Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "c");
            }
            else
            {
                using AssertingEnumerator e = new AssertingEnumerator(expression);

                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(op1, op1Text);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
                e.AssertToken(op2, op2Text);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "c");
            }
        }

        [Theory]
        [MemberData(nameof(GetUnaryOperatorPairsData))]
        public void Parser_UnaryExpression_HonorsPrecedences(SyntaxKind unaryKind, SyntaxKind binaryKind)
        {
            int unaryPrecedence = unaryKind.GetUnaryOperatorPrecedence();
            int binaryPrecedence = binaryKind.GetBinaryOperatorPrecedence();

            string? unaryText = unaryKind.GetText();
            string? binaryText = binaryKind.GetText();
            string text = $"{unaryText} a {binaryText} b";

            var expression = ParseExpression(text);
            using var e = new AssertingEnumerator(expression);

            if (unaryPrecedence >= binaryPrecedence)
            {
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.UnaryExpression);
                e.AssertToken(unaryKind, unaryText);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(binaryKind, binaryText);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
            }
            else
            {
                e.AssertNode(SyntaxKind.UnaryExpression);
                e.AssertToken(unaryKind, unaryText);
                e.AssertNode(SyntaxKind.BinaryExpression);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "a");
                e.AssertToken(binaryKind, binaryText);
                e.AssertNode(SyntaxKind.NameExpression);
                e.AssertToken(SyntaxKind.IdentifierToken, "b");
            }
        }

        [Fact]
        public void Parser_Allows_Top_Level_Program()
        {
            var syntaxTree = SyntaxTree.Parse(@"
var a = 5;
var b = a * a;
");
            Assert.Empty(syntaxTree.Diagnostics);
        }

        [Fact]
        public void Parser_Supports_ExpressionBodies()
        {
            var syntaxTree = SyntaxTree.Parse(@"def main() => a + a;");
            Assert.Empty(syntaxTree.Diagnostics);
        }

        private static ExpressionSyntax ParseExpression(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var statement = Assert.Single(syntaxTree.Root.Statements);

            return Assert.IsType<ExpressionStatementSyntax>(statement).Expression;
        }

        public static IEnumerable<object[]> GetBinaryOperatorPairsData()
        {
            foreach (SyntaxKind op1 in SyntaxFacts.GetBinaryOperatorKinds())
            {
                foreach (SyntaxKind op2 in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { op1, op2 };
                }
            }
        }

        public static IEnumerable<object[]> GetUnaryOperatorPairsData()
        {
            foreach (SyntaxKind unaryKind in SyntaxFacts.GetUnaryOperatorKinds())
            {
                foreach (SyntaxKind binaryKind in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { unaryKind, binaryKind };
                }
            }
        }
    }
}