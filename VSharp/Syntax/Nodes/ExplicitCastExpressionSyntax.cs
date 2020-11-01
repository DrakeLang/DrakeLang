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
    public sealed class ExplicitCastExpressionSyntax : ExpressionSyntax
    {
        public ExplicitCastExpressionSyntax(SyntaxToken openParenthesisToken, TypeExpressionSyntax typeExpression, SyntaxToken closeParenthesisToken, ExpressionSyntax expression)
        {
            OpenParenthesisToken = openParenthesisToken;
            TypeExpression = typeExpression;
            CloseParenthesisToken = closeParenthesisToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.ExplicitCastExpression;

        public SyntaxToken OpenParenthesisToken { get; }
        public TypeExpressionSyntax TypeExpression { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public ExpressionSyntax Expression { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesisToken;
            yield return TypeExpression;
            yield return CloseParenthesisToken;
            yield return Expression;
        }
    }
}