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
using PHPSharp.Text;
using System;
using System.Collections.Generic;
using Xunit;

namespace PHPSharp.Tests
{
    public class EvaluationTests
    {
        [Theory]
        [InlineData("1;", 1)]
        [InlineData("+1;", 1)]
        [InlineData("-1;", -1)]
        [InlineData("14 + 12;", 26)]
        [InlineData("12 - 3;", 9)]
        [InlineData("4 * 2;", 8)]
        [InlineData("9 / 3;", 3)]
        [InlineData("(10);", 10)]
        [InlineData("12 == 3;", false)]
        [InlineData("3 == 3;", true)]
        [InlineData("12 != 3;", true)]
        [InlineData("3 != 3;", false)]
        [InlineData("3 < 4;", true)]
        [InlineData("5 < 4;", false)]
        [InlineData("4 <= 4;", true)]
        [InlineData("4 <= 5;", true)]
        [InlineData("5 <= 4;", false)]
        [InlineData("4 > 3;", true)]
        [InlineData("4 > 5;", false)]
        [InlineData("4 >= 4;", true)]
        [InlineData("5 >= 4;", true)]
        [InlineData("4 >= 5;", false)]
        [InlineData("false == false;", true)]
        [InlineData("true == false;", false)]
        [InlineData("false != false;", false)]
        [InlineData("true != false;", true)]
        [InlineData("true;", true)]
        [InlineData("false;", false)]
        [InlineData("!true;", false)]
        [InlineData("!false;", true)]
        [InlineData("{ var a = 0; (a = 10) * a; }", 100)]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        [Fact]
        public void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            string text = @"
                {
                    var x = 10;
                    var y = 100;
                    {
                        var x = 10;
                    }
                    var [x] = 5;
                }
            ";

            string diagnostics = @"
                Variable 'x' is already declared.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_Undefined()
        {
            string text = @"
                [x] = 10;
            ";

            string diagnostics = @"
                Variable 'x' does not exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_CannotAssign()
        {
            string text = @"
                {
                    let x = 0;
                    x [=] 3;
                }
            ";

            string diagnostics = @"
                Variable 'x' is read-only and cannot be assigned.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_CannotConvert()
        {
            string text = @"
                {
                    var x = 10;
                    x = [true];
                }
            ";

            string diagnostics = @"
                Cannot convert type 'System.Boolean' to 'System.Int32'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Unary_Reports_UndefinedOperator()
        {
            string text = @"[+]true;";

            string diagnostics = @"
                Unary operator '+' is not defined for type 'System.Boolean'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Binary_Reports_UndefinedOperator()
        {
            string text = @"10 [*] true;";

            string diagnostics = @"
                Binary operator '*' is not defined for types 'System.Int32' and 'System.Boolean'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            AnnotatedText annotaedText = AnnotatedText.Parse(text);
            SyntaxTree syntaxTree = SyntaxTree.Parse(annotaedText.Text);
            Compilation compilation = new Compilation(syntaxTree);
            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            string[] expectedDiagnostics = AnnotatedText.UnintentLines(diagnosticText);

            if (annotaedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

            Assert.Equal(expectedDiagnostics.Length, result.Diagnostics.Length);

            for (int i = 0; i < expectedDiagnostics.Length; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = result.Diagnostics[i].Message;

                TextSpan expectedSpan = annotaedText.Spans[i];
                TextSpan actualSpan = result.Diagnostics[i].Span;

                Assert.Equal(expectedMessage, actualMessage);
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}