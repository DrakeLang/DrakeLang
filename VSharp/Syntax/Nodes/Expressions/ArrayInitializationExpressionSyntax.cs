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

using System.Collections.Generic;

namespace VSharp.Syntax
{
    public abstract class ArrayInitializationExpressionSyntax : ExpressionSyntax
    {
        internal ArrayInitializationExpressionSyntax(
            TypeExpressionSyntax typeToken,
            SyntaxToken openBracketToken, ExpressionSyntax? sizeExpression, SyntaxToken closeBracketToken)
        {
            TypeToken = typeToken;
            OpenBracketToken = openBracketToken;
            SizeExpression = sizeExpression;
            CloseBracketToken = closeBracketToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayInitializationExpression;

        public TypeExpressionSyntax TypeToken { get; }
        public SyntaxToken OpenBracketToken { get; }
        public ExpressionSyntax? SizeExpression { get; }
        public SyntaxToken CloseBracketToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeToken;
            yield return OpenBracketToken;

            if (SizeExpression is not null)
                yield return SizeExpression;

            yield return CloseBracketToken;
        }
    }

    public sealed class BodiedArrayInitializationExpressionSyntax : ArrayInitializationExpressionSyntax
    {
        internal BodiedArrayInitializationExpressionSyntax(
            TypeExpressionSyntax typeToken,
            SyntaxToken openBracketToken, ExpressionSyntax? sizeExpression, SyntaxToken closeBracketToken,
            SyntaxToken openBraceToken, SeparatedSyntaxList<ExpressionSyntax> initializer, SyntaxToken closeBraceToken)
            : base(typeToken, openBracketToken, sizeExpression, closeBracketToken)
        {
            OpenBraceToken = openBraceToken;
            Initializer = initializer;
            CloseBraceToken = closeBraceToken;
        }

        public SyntaxToken OpenBraceToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Initializer { get; }
        public SyntaxToken CloseBraceToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var c in base.GetChildren())
                yield return c;

            yield return OpenBraceToken;
            foreach (var item in Initializer.GetWithSeparators())
            {
                yield return item;
            }
            yield return CloseBraceToken;
        }
    }

    public sealed class SimpleArrayInitializerExpressionSyntax : ArrayInitializationExpressionSyntax
    {
        internal SimpleArrayInitializerExpressionSyntax(
            TypeExpressionSyntax typeToken,
            SyntaxToken openBracketToken, ExpressionSyntax? sizeExpression, SyntaxToken closeBracketToken,
            SyntaxToken lambdaOperator, SeparatedSyntaxList<ExpressionSyntax> initializer)
            : base(typeToken, openBracketToken, sizeExpression, closeBracketToken)
        {
            LambdaOperator = lambdaOperator;
            Initializer = initializer;
        }

        public SyntaxToken LambdaOperator { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Initializer { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var c in base.GetChildren())
                yield return c;

            yield return LambdaOperator;
            foreach (var item in Initializer.GetWithSeparators())
            {
                yield return item;
            }
        }
    }
}