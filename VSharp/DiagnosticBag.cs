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

using System.Collections;
using System.Collections.Generic;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;

namespace VSharp
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public DiagnosticBag()
        {
        }

        #region Methods

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        #endregion Methods

        #region Report

        public void ReportInvalidValue(TextSpan span, string? text, TypeSymbol type)
        {
            string message = $"The number '{text}' is not a valid value for type '{type}'.";
            Report(span, message);
        }

        public void ReportBadCharacter(int position, char character)
        {
            TextSpan span = new TextSpan(position, 1);
            string message = $"Bad character input '{character}'.";
            Report(span, message);
        }

        public void ReportUnterminatedString(TextSpan span)
        {
            string message = "Unterminated string literal";
            Report(span, message);
        }

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            string message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>";
            Report(span, message);
        }

        public void ReportTypeExpected(TextSpan span, SyntaxKind actualKind)
        {
            string message = $"Unexpected token <{actualKind}>, expected type.";
            Report(span, message);
        }

        public void ReportUnexpectedVarKeyword(TextSpan span)
        {
            string message = "The contextual keyword 'var' may only appear within a local variable declaration.";
            Report(span, message);
        }

        public void ReportUnexpectedBreakOrContinue(TextSpan span)
        {
            string message = "No enclosing loop out of which to break or continue.";
            Report(span, message);
        }

        public void ReportUndefinedUnaryOperator(TextSpan span, string? operatorText, TypeSymbol type)
        {
            string message = $"Unary operator '{operatorText}' is not defined for type '{type}'.";
            Report(span, message);
        }

        public void ReportUndefinedBinaryOperator(TextSpan span, string? operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            string message = $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.";
            Report(span, message);
        }

        public void ReportUndefinedSymbol(TextSpan span, string? name)
        {
            string message = $"Symbol '{name}' does not exist.";
            Report(span, message);
        }

        public void ReportWrongArgumentCount(TextSpan span, string name, int expected, int actual)
        {
            string message = $"Method '{name}' requires {expected} arguments, but recieved {actual}.";
            Report(span, message);
        }

        public void ReportUnexpectedPipedArgument(TextSpan span)
        {
            string message = $"Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.";
            Report(span, message);
        }

        public void ReportWrongArgumentType(TextSpan span, string methodName, string parameterName, TypeSymbol expected, TypeSymbol actual)
        {
            string message = $"Parameter '{parameterName}' in method '{methodName}' requires value of type '{expected}', but recieved value of type '{actual}'.";
            Report(span, message);
        }

        public void ReportVariableAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A variable with the name '{name}' is already declared.";
            Report(span, message);
        }

        public void ReportLabelAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A label with the name '{name}' is already declared.";
            Report(span, message);
        }

        public void ReportDuplicateParameterName(TextSpan span, string name)
        {
            string message = $"Duplicate parameter name '{name}'.";
            Report(span, message);
        }

        public void ReportMethodAlreadyDeclared(TextSpan span, string? name)
        {
            string message = $"A method with the name '{name}' is already declared in this scope.";
            Report(span, message);
        }

        public void ReportCannotImplicitlyConvert(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"Cannot implicitly convert type '{fromType}' to '{toType}'. An explicit convertion exists (are you missing a cast?)";
            Report(span, message);
        }

        public void ReportNoExplicitConversion(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"No explicit convertion exists for type '{fromType}' to '{toType}'.";
            Report(span, message);
        }

        public void ReportCannotConvert(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            string message = $"Cannot convert type '{fromType}' to '{toType}'.";
            Report(span, message);
        }

        public void ReportCannotAssignVoid(TextSpan span)
        {
            string message = "Cannot assign void to an implicitly-typed variable.";
            Report(span, message);
        }

        public void ReportCannotAssignReadOnly(TextSpan span, string? name)
        {
            string message = $"Variable '{name}' is read-only and cannot be assigned.";
            Report(span, message);
        }

        public void ReportCannotDeclareConditional(TextSpan span)
        {
            string message = $"Variable declarations cannot be placed right after a condition.";
            Report(span, message);
        }

        public void ReportDeclarationOrAssignmentOnly(TextSpan span, SyntaxKind kind)
        {
            string message = $"Expected variable declaration or assignment, got <{kind}> instead.";
            Report(span, message);
        }

        public void ReportCanOnlyPipeToMethods(TextSpan span)
        {
            string message = $"Expressions can only be piped into methods.";
            Report(span, message);
        }

        public void ReportMethodNotAllPathsReturnValue(TextSpan span)
        {
            string message = $"Not all paths return a value.";
            Report(span, message);
        }

        public void ReportInvalidReturnInVoidMethod(TextSpan span)
        {
            string message = $"Cannot return an expression from a method returning void.";
            Report(span, message);
        }

        public void ReportMissingReturnExpression(TextSpan span)
        {
            string message = $"Expected to return expression in non-void returning method.";
            Report(span, message);
        }

        #endregion Report

        #region IEnumerable

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable

        #region Helpers

        private void Report(TextSpan span, string message)
        {
            Diagnostic diagnostic = new Diagnostic(span, message);
            _diagnostics.Add(diagnostic);
        }

        #endregion Helpers
    }
}