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
    public sealed class BreakStatementSyntax : StatementSyntax
    {
        internal BreakStatementSyntax(SyntaxToken breakKeyword, LiteralExpressionSyntax? layerExpression, SyntaxToken semicolon)
        {
            BreakKeyword = breakKeyword;
            LayerExpression = layerExpression;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.BreakStatement;

        public SyntaxToken BreakKeyword { get; }
        public LiteralExpressionSyntax? LayerExpression { get; }
        public SyntaxToken Semicolon { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BreakKeyword;
            if (LayerExpression != null)
                yield return LayerExpression;

            yield return Semicolon;
        }
    }
}