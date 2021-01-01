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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DrakeLang.Syntax
{
    public sealed class TypeExpressionSyntax : ExpressionSyntax
    {
        private TypeExpressionSyntax(SyntaxToken typeToken, ImmutableArray<SyntaxToken>? extraTokens = null)
        {
            TypeToken = typeToken;
            ExtraTokens = extraTokens ?? ImmutableArray<SyntaxToken>.Empty;
        }

        internal static TypeExpressionSyntax Create(SyntaxToken typeToken) => new(typeToken);

        internal static TypeExpressionSyntax CreateArray(SyntaxToken typeToken, ImmutableArray<SyntaxToken> bracketTokens)
        {
            if (bracketTokens.Length % 2 != 0)
                throw new ArgumentException("Bracket tokens must be of a length dividable by two (opening and closing bracket pairs).", nameof(bracketTokens));

            return new(typeToken, bracketTokens);
        }

        public override SyntaxKind Kind => SyntaxKind.TypeExpression;
        public SyntaxToken TypeToken { get; }
        public ImmutableArray<SyntaxToken> ExtraTokens { get; }
        public bool IsArray => ExtraTokens.Length > 0;

        public TypeExpressionSyntax GetArrayTypeArgument() =>
            IsArray ?
                new(TypeToken, ExtraTokens.SkipLast(2).ToImmutableArray()) :
                throw new InvalidOperationException("Type expression does not represent an array.");

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeToken;
            foreach (var token in ExtraTokens)
                yield return token;
        }
    }
}