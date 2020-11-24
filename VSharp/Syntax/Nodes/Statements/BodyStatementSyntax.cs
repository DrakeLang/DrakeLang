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
using System.Collections.Immutable;

namespace VSharp.Syntax
{
    public abstract class BodyStatementSyntax : StatementSyntax
    {
        protected BodyStatementSyntax(ImmutableArray<StatementSyntax> statements)
        {
            Statements = statements;
        }

        public ImmutableArray<StatementSyntax> Statements { get; }
    }

    public sealed class BlockBodyStatementSyntax : BodyStatementSyntax
    {
        internal BlockBodyStatementSyntax(SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBraceToken)
            : base(statements)
        {
            OpenBraceToken = openBraceToken;
            CloseBraceToken = closeBraceToken;
        }

        public override SyntaxKind Kind => SyntaxKind.BlockBodyStatement;
        public SyntaxToken OpenBraceToken { get; }
        public SyntaxToken CloseBraceToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;

            foreach (var statement in Statements)
                yield return statement;

            yield return CloseBraceToken;
        }
    }

    public sealed class ExpressionBodyStatementSyntax : BodyStatementSyntax
    {
        internal ExpressionBodyStatementSyntax(SyntaxToken lambdaOperator, StatementSyntax statement)
            : base(ImmutableArray.Create(statement))
        {
            LambdaOperator = lambdaOperator;
            Statement = statement;
        }

        public override SyntaxKind Kind => SyntaxKind.ExpressionBodyStatement;
        public SyntaxToken LambdaOperator { get; }
        public StatementSyntax Statement { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LambdaOperator;
            yield return Statement;
        }
    }
}