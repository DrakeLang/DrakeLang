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
    public abstract class WithNamespaceStatementSyntax : StatementSyntax
    {
        protected WithNamespaceStatementSyntax(SyntaxToken withToken, SeparatedSyntaxList<SyntaxToken> names, ImmutableArray<StatementSyntax> statements)
        {
            WithToken = withToken;
            Names = names;
            Statements = statements;
        }

        public override SyntaxKind Kind => SyntaxKind.WithNamespaceStatement;
        public SyntaxToken WithToken { get; }
        public SeparatedSyntaxList<SyntaxToken> Names { get; }
        public ImmutableArray<StatementSyntax> Statements { get; }
    }

    public sealed class BodiedWithNamespaceStatementSyntax : WithNamespaceStatementSyntax
    {
        internal BodiedWithNamespaceStatementSyntax(SyntaxToken withToken, SeparatedSyntaxList<SyntaxToken> names, BlockStatementSyntax body)
            : base(withToken, names, body.Statements)
        {
            Body = body;
        }

        public BlockStatementSyntax Body { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithToken;
            foreach (var name in Names.GetWithSeparators())
            {
                yield return name;
            }
            yield return Body;
        }
    }

    public sealed class SimpleWithNamespaceStatementSyntax : WithNamespaceStatementSyntax
    {
        internal SimpleWithNamespaceStatementSyntax(SyntaxToken withToken, SeparatedSyntaxList<SyntaxToken> names, SyntaxToken semicolonToken, ImmutableArray<StatementSyntax> statements)
            : base(withToken, names, statements)
        {
            SemicolonToken = semicolonToken;
        }

        public SyntaxToken SemicolonToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithToken;
            foreach (var name in Names.GetWithSeparators())
            {
                yield return name;
            }
            yield return SemicolonToken;
            foreach (var statement in Statements)
            {
                yield return statement;
            }
        }
    }
}