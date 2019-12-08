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

using PHPSharp.Symbols;
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
        [MemberData(nameof(GetStatementsData))]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        #region Reports

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
        public void Evaluator_Assigned_Reports_CannotConvert()
        {
            string text = @"
                {
                    var x = 10;
                    x = [true];
                }
            ";

            string diagnostics = @"
                Cannot convert type 'bool' to 'int'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Unary_Reports_UndefinedOperator()
        {
            string text = @"[+]true;";

            string diagnostics = @"
                Unary operator '+' is not defined for type 'bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Binary_Reports_UndefinedOperator()
        {
            string text = @"10 [*] true;";

            string diagnostics = @"
                Binary operator '*' is not defined for types 'int' and 'bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        #endregion Reports

        public static IEnumerable<object[]> GetStatementsData()
        {
            foreach ((string statement, object result) in GetStatements())
                yield return new object[] { statement, result };
        }

        private static IEnumerable<(string statement, object result)> GetStatements()
        {
            // Int statements
            yield return ("1;", 1);
            yield return ("+1;", 1);
            yield return ("-1;", -1);
            yield return ("~1;", ~1);
            yield return ("14 + 12;", 26);
            yield return ("12 - 3;", 9);
            yield return ("4 * 2;", 8);
            yield return ("9 / 3;", 3);
            yield return ("(10);", 10);

            // Int comparisons
            yield return ("12 == 3;", false);
            yield return ("3 == 3;", true);
            yield return ("12 != 3;", true);
            yield return ("3 != 3;", false);
            yield return ("3 < 4;", true);
            yield return ("5 < 4;", false);
            yield return ("4 <= 4;", true);
            yield return ("4 <= 5;", true);
            yield return ("5 <= 4;", false);
            yield return ("4 > 3;", true);
            yield return ("4 > 5;", false);
            yield return ("4 >= 4;", true);
            yield return ("5 >= 4;", true);
            yield return ("4 >= 5;", false);

            // Bitwise int operations
            yield return ("1 | 2;", 3);
            yield return ("1 | 0;", 1);
            yield return ("1 & 2;", 0);
            yield return ("1 & 1;", 1);
            yield return ("1 & 0;", 0);
            yield return ("1 ^ 0;", 1);
            yield return ("0 ^ 1;", 1);
            yield return ("1 ^ 3;", 2);

            // Bool statements
            yield return ("true;", true);
            yield return ("false;", false);
            yield return ("!true;", false);
            yield return ("!false;", true);
            yield return ("true || true;", true);
            yield return ("true || false;", true);
            yield return ("false || true;", true);
            yield return ("false || false;", false);
            yield return ("true && true;", true);
            yield return ("true && false;", false);
            yield return ("false && true;", false);
            yield return ("false && false;", false);

            // Bool comparisons
            yield return ("false == false;", true);
            yield return ("true == false;", false);
            yield return ("false != false;", false);
            yield return ("true != false;", true);

            // Bitwize bool operations
            yield return ("false | false;", false);
            yield return ("false | true;", true);
            yield return ("true | false;", true);
            yield return ("true | true;", true);
            yield return ("false & false;", false);
            yield return ("false & true;", false);
            yield return ("true & false;", false);
            yield return ("true & true;", true);
            yield return ("false ^ false;", false);
            yield return ("false ^ true;", true);
            yield return ("true ^ false;", true);
            yield return ("true ^ true;", false);

            // Variable & assignment
            yield return ("{ var a = 0; (a = 10) * a; }", 100);
            yield return ("{ var a = 11; ++a; }", 12);
            yield return ("{ var a = 11; --a; }", 10);
            yield return ("{ var a = 11; a++; }", 11);
            yield return ("{ var a = 11; a--; }", 11);
            yield return ("{ var a = 11; ++a; a; }", 12);
            yield return ("{ var a = 11; --a; a; }", 10);
            yield return ("{ var a = 11; a++; a; }", 12);
            yield return ("{ var a = 11; a--; a; }", 10);
            yield return ("{ var a = 11; a += -1; }", 10);
            yield return ("{ var a = 11; a -= 1; }", 10);
            yield return ("{ var a = 10; a *= 2; }", 20);
            yield return ("{ var a = 10; a /= 2; }", 5);

            // If-else-statement
            yield return ("{ var a = 0; if (a == 0) a = 10; a; }", 10);
            yield return ("{ var a = 4; if (a == 0) a = 10; a; }", 4);
            yield return ("{ var a = 0; if (a == 0) a = 10; else a = 34; a; }", 10);
            yield return ("{ var a = 4; if (a == 0) a = 10; else a = 32; a; }", 32);

            // While, for statement
            yield return ("{ var a = 0; while (a < 10) a = a + 1; a; }", 10);
            yield return ("{ var result = 0; for (var i = 0; i <= 10; ++i) result = result + i; result; }", 55);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            Compilation compilation = new Compilation(syntaxTree);
            Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();
            EvaluationResult result = compilation.Evaluate(variables);

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
                string? actualMessage = result.Diagnostics[i].Message;

                TextSpan expectedSpan = annotaedText.Spans[i];
                TextSpan actualSpan = result.Diagnostics[i].Span;

                Assert.Equal(expectedMessage, actualMessage);
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}