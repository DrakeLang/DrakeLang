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

namespace DrakeLang.Syntax
{
    public abstract class ArrayInitializationExpressionSyntax : ExpressionSyntax
    {
        internal ArrayInitializationExpressionSyntax(
            TypeExpressionSyntax? typeToken, SyntaxToken openBracketToken, SyntaxToken closeBracketToken)
        {
            TypeToken = typeToken;
            OpenBracketToken = openBracketToken;
            CloseBracketToken = closeBracketToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayInitializationExpression;

        public TypeExpressionSyntax? TypeToken { get; }
        public SyntaxToken OpenBracketToken { get; }
        public SyntaxToken CloseBracketToken { get; }
    }

    public sealed class DynamicallySizedArrayInitializationExpressionSyntax : ArrayInitializationExpressionSyntax
    {
        public DynamicallySizedArrayInitializationExpressionSyntax(
            TypeExpressionSyntax? typeToken, SyntaxToken openBracketToken, ExpressionSyntax sizeExpression, SyntaxToken closeBracketToken,
            SyntaxToken lambdaToken, ExpressionSyntax initializationExpression)
            : base(typeToken, openBracketToken, closeBracketToken)
        {
            SizeExpression = sizeExpression;
            LambdaToken = lambdaToken;
            InitializationExpression = initializationExpression;
        }

        public ExpressionSyntax SizeExpression { get; }
        public SyntaxToken LambdaToken { get; }
        public ExpressionSyntax InitializationExpression { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (TypeToken is not null)
                yield return TypeToken;

            yield return OpenBracketToken;
            yield return SizeExpression;
            yield return CloseBracketToken;
            yield return LambdaToken;
            yield return InitializationExpression;
        }
    }

    public abstract class FixedSizedArrayInitializationExpressionSyntax : ArrayInitializationExpressionSyntax
    {
        internal FixedSizedArrayInitializationExpressionSyntax(
            TypeExpressionSyntax? typeToken, SyntaxToken openBracketToken, SyntaxToken closeBracketToken, SeparatedSyntaxList<ExpressionSyntax> initializer)
            : base(typeToken, openBracketToken, closeBracketToken)
        {
            Initializer = initializer;
        }

        public SeparatedSyntaxList<ExpressionSyntax> Initializer { get; }
    }

    public sealed class BodiedArrayInitializationExpressionSyntax : FixedSizedArrayInitializationExpressionSyntax
    {
        internal BodiedArrayInitializationExpressionSyntax(
            TypeExpressionSyntax? typeToken,
            SyntaxToken openBracketToken, SyntaxToken closeBracketToken,
            SyntaxToken openBraceToken, SeparatedSyntaxList<ExpressionSyntax> initializer, SyntaxToken closeBraceToken)
            : base(typeToken, openBracketToken, closeBracketToken, initializer)
        {
            OpenBraceToken = openBraceToken;
            CloseBraceToken = closeBraceToken;
        }

        public SyntaxToken OpenBraceToken { get; }
        public SyntaxToken CloseBraceToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (TypeToken is not null)
                yield return TypeToken;

            yield return OpenBracketToken;
            yield return CloseBracketToken;

            yield return OpenBraceToken;
            foreach (var item in Initializer.GetWithSeparators())
            {
                yield return item;
            }
            yield return CloseBraceToken;
        }
    }

    public sealed class SimpleArrayInitializerExpressionSyntax : FixedSizedArrayInitializationExpressionSyntax
    {
        internal SimpleArrayInitializerExpressionSyntax(
            TypeExpressionSyntax? typeToken, SyntaxToken openBracketToken, SeparatedSyntaxList<ExpressionSyntax> initializer, SyntaxToken closeBracketToken)
            : base(typeToken, openBracketToken, closeBracketToken, initializer)
        { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (TypeToken is not null)
                yield return TypeToken;

            yield return OpenBracketToken;

            foreach (var item in Initializer.GetWithSeparators())
            {
                yield return item;
            }

            yield return CloseBracketToken;
        }
    }
}