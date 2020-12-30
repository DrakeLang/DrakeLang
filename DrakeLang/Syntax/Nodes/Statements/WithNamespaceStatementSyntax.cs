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
using System.Collections.Immutable;

namespace DrakeLang.Syntax
{
    public abstract class WithNamespaceStatementSyntax : StatementSyntax
    {
        protected WithNamespaceStatementSyntax(SyntaxToken withKeyword, SeparatedSyntaxList<SyntaxToken> names)
        {
            WithKeyword = withKeyword;
            Names = names;
        }

        public override SyntaxKind Kind => SyntaxKind.WithNamespaceStatement;
        public SyntaxToken WithKeyword { get; }
        public SeparatedSyntaxList<SyntaxToken> Names { get; }
        public abstract ImmutableArray<StatementSyntax> Statements { get; }
    }

    public sealed class BodiedWithNamespaceStatementSyntax : WithNamespaceStatementSyntax
    {
        internal BodiedWithNamespaceStatementSyntax(SyntaxToken withKeyword, SeparatedSyntaxList<SyntaxToken> names, BlockStatementSyntax body)
            : base(withKeyword, names)
        {
            Body = body;
        }

        public BlockStatementSyntax Body { get; }
        public override ImmutableArray<StatementSyntax> Statements => Body.Statements;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithKeyword;
            foreach (var name in Names.GetWithSeparators())
            {
                yield return name;
            }
            yield return Body;
        }
    }

    public sealed class SimpleWithNamespaceStatementSyntax : WithNamespaceStatementSyntax
    {
        internal SimpleWithNamespaceStatementSyntax(SyntaxToken withKeyword, SeparatedSyntaxList<SyntaxToken> names, SyntaxToken semicolonToken, ImmutableArray<StatementSyntax> statements)
            : base(withKeyword, names)
        {
            SemicolonToken = semicolonToken;
            Statements = statements;
        }

        public SyntaxToken SemicolonToken { get; }
        public override ImmutableArray<StatementSyntax> Statements { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithKeyword;
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