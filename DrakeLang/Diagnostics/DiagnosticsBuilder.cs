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

using DrakeLang.Symbols;
using DrakeLang.Syntax;
using DrakeLang.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DrakeLang
{
    internal sealed class DiagnosticsBuilder : IEnumerable<Diagnostic>
    {
        public DiagnosticsBuilder(SourceText text)
        {
            Text = text;
            Diagnostics = new();
        }

        public DiagnosticsBuilder(SourceText text, DiagnosticBag diagnostics)
        {
            Diagnostics = diagnostics;
            Text = text;
        }

        public DiagnosticBag Diagnostics { get; }
        public SourceText Text { get; }

        #region Methods

        public void AddRange(IEnumerable<Diagnostic> diagnostics) => Diagnostics.AddRange(diagnostics);

        #endregion Methods

        #region Report

        public void ReportAmbigousSymbolReference(TextSpan span, IEnumerable<MemberSymbol> symbols)
        {
            var symbolStrs = symbols.Select(s => s.FullName)
                                    .OrderBy(name => name)
                                    .Select(name => '\'' + name + '\'');

            string message = $"Reference is ambiguous between the following symbols: {string.Join(", ", symbolStrs)}.";
            Report(span, message);
        }

        public void ReportBadCharacter(int position, char character)
        {
            TextSpan span = new TextSpan(position, 1);
            string message = $"Bad character input '{character}'.";
            Report(span, message);
        }

        public void ReportCannotAssignReadOnly(TextSpan span, string? name)
        {
            string message = $"Variable '{name}' is read-only and cannot be modified.";
            Report(span, message);
        }

        public void ReportCannotAssignVoid(TextSpan span)
        {
            string message = "Cannot assign void to an implicitly-typed variable.";
            Report(span, message);
        }

        public void ReportCannotConvert(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"Cannot convert type '{fromType}' to '{toType}'.";
            Report(span, message);
        }

        public void ReportCannotDeclareConditional(TextSpan span)
        {
            string message = $"Variable declarations cannot be placed right after a condition.";
            Report(span, message);
        }

        public void ReportCannotImplicitlyConvert(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"Cannot implicitly convert type '{fromType}' to '{toType}'. An explicit convertion exists (are you missing a cast?).";
            Report(span, message);
        }

        public void ReportCannotInferReturnType(TextSpan span, string? name)
        {
            string message = $"Implicit return type of method '{name}' cannot be infered.";
            Report(span, message);
        }

        public void ReportCanOnlyPipeToMethods(TextSpan span)
        {
            string message = $"Expressions can only be piped into methods.";
            Report(span, message);
        }

        public void ReportDuplicateParameterName(TextSpan span, string name)
        {
            string message = $"Duplicate parameter name '{name}'.";
            Report(span, message);
        }

        public void ReportEmptyCharacterLiteral(TextSpan span)
        {
            string message = "Empty character literal.";
            Report(span, message);
        }

        public void ReportExplicitTypeExpected(TextSpan span, SyntaxKind actualKind)
        {
            string message = $"Unexpected token <{actualKind}>, expected explicit type.";
            Report(span, message);
        }

        public void ReportIllegalExplicitType(TextSpan span)
        {
            string message = $"Explicit types cannot be used in this context (it can only be used in combination with the 'set' keyword).";
            Report(span, message);
        }

        public void ReportIllegalNamespaceDeclaration(TextSpan span)
        {
            string message = $"Namespaces may not be declared inside of methods or types.";
            Report(span, message);
        }

        public void ReportIllegalSimpleNamespaceDeclaration(TextSpan span)
        {
            string message = $"Simple namespace declarations may only exist as top-level statement (not nested in other namespaces).";
            Report(span, message);
        }

        public void ReportIllegalStatementPlacement(TextSpan span)
        {
            string message = $"Unexpected statement. Namespaces and type declarations cannot directly contain statements.";
            Report(span, message);
        }

        public void ReportIncrementOperandMustBeVariable(TextSpan span)
        {
            string message = "The operand of an increment or decrement operation must be variable.";
            Report(span, message);
        }

        public void ReportInvalidNumberValue(TextSpan span, string? text, TypeSymbol type)
        {
            string message = $"The number '{text}' is not a valid value for type '{type}'.";
            Report(span, message);
        }

        public void ReportInvalidReturnInVoidMethod(TextSpan span)
        {
            string message = $"Cannot return an expression from a method returning void.";
            Report(span, message);
        }

        public void ReportLabelAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A label with the name '{name}' is already declared.";
            Report(span, message);
        }

        public void ReportMethodAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A method with the name '{name}' is already declared in this scope.";
            Report(span, message);
        }

        public void ReportMethodNotAllPathsReturnValue(TextSpan span)
        {
            string message = $"Not all paths return a value.";
            Report(span, message);
        }

        public void ReportMissingReturnExpression(TextSpan span)
        {
            string message = $"Expected to return expression in non-void returning method.";
            Report(span, message);
        }

        public void ReportMultipleMainMethods(TextSpan span)
        {
            string message = "A project may only contain a single main method.";
            Report(span, message);
        }

        public void ReportMultipleSourceTextsWithTopLevelStatements(TextSpan span)
        {
            string message = "Top-level statements may only exist in a single source file.";
            Report(span, message);
        }

        public void ReportNoExplicitConversion(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"No explicit convertion exists for type '{fromType}' to '{toType}'.";
            Report(span, message);
        }

        public void ReportTopLevelStatementCannotBeCombinedWithExplicitMainMethod(TextSpan span)
        {
            string message = "Top-level statements may not be defined in a project that already contain an explicit main method declaration.";
            Report(span, message);
        }

        public void ReportTypeDoesNotHaveIndexer(TextSpan span, string name)
        {
            string message = $"Type '{name}' does not expose an indexer.";
            Report(span, message);
        }

        public void ReportUndefinedBinaryOperator(TextSpan span, string? operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            string message = $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.";
            Report(span, message);
        }

        public void ReportUndefinedSymbol(TextSpan span, string? name)
        {
            string message = $"Symbol '{name}' does not exist in this context.";
            Report(span, message);
        }

        public void ReportUndefinedUnaryOperator(TextSpan span, string? operatorText, TypeSymbol type)
        {
            string message = $"Unary operator '{operatorText}' is not defined for type '{type}'.";
            Report(span, message);
        }

        public void ReportUnexpectedBreakOrContinue(TextSpan span)
        {
            string message = "No enclosing loop out of which to break or continue.";
            Report(span, message);
        }

        public void ReportUnexpectedPipedArgument(TextSpan span)
        {
            string message = $"Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.";
            Report(span, message);
        }

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            string message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
            Report(span, message);
        }

        public void ReportUnrecognizedEscapeSequence(TextSpan span)
        {
            string message = $"Unrecognized escape sequence.";
            Report(span, message);
        }

        public void ReportUnterminatedCharacterLiteral(TextSpan span)
        {
            string message = "Unterminated character literal.";
            Report(span, message);
        }
        public void ReportUnterminatedString(TextSpan span)
        {
            string message = "Unterminated string literal.";
            Report(span, message);
        }

        public void ReportVariableAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A variable with the name '{name}' is already declared.";
            Report(span, message);
        }

        public void ReportWrongArgumentCount(TextSpan span, string name, int expected, int actual)
        {
            string message = $"Method or indexer '{name}' requires {expected} arguments, but recieved {actual}.";
            Report(span, message);
        }

        public void ReportWrongArgumentType(TextSpan span, string methodName, string parameterName, TypeSymbol expected, TypeSymbol actual)
        {
            string message = $"Parameter '{parameterName}' in method '{methodName}' requires value of type '{expected}', but recieved value of type '{actual}'.";
            Report(span, message);
        }
        #endregion Report

        #region Helpers

        private void Report(TextSpan span, string message) => Diagnostics.Report(Text, span, message);

        #endregion Helpers

        #region IEnumerable

        public IEnumerator<Diagnostic> GetEnumerator() => Diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable
    }
}