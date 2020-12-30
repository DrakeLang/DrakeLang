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
    public abstract class WithAliasStatementSyntax : StatementSyntax
    {
        internal WithAliasStatementSyntax(
            SyntaxToken withKeyword, SyntaxToken alias, SyntaxToken equalsToken,
            SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken identifier)
        {
            WithKeyword = withKeyword;
            Alias = alias;
            EqualsToken = equalsToken;
            NamespaceNames = namespaceNames;
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.WithAliasStatement;
        public SyntaxToken WithKeyword { get; }
        public SyntaxToken Alias { get; }
        public SyntaxToken EqualsToken { get; }
        public SeparatedSyntaxList<SyntaxToken>? NamespaceNames { get; }
        public SyntaxToken Identifier { get; }
        public abstract ImmutableArray<StatementSyntax> Statements { get; }
    }

    public abstract class WithMethodAliasStatementSyntax : WithAliasStatementSyntax
    {
        internal WithMethodAliasStatementSyntax(
            SyntaxToken withKeyword, SyntaxToken alias, SyntaxToken equalsToken,
            SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken methodName)
            : base(withKeyword, alias, equalsToken, namespaceNames, methodName)
        {
        }
    }

    public sealed class BodiedWithMethodAliasStatementSyntax : WithMethodAliasStatementSyntax
    {
        internal BodiedWithMethodAliasStatementSyntax(
            SyntaxToken withKeyword, SyntaxToken alias, SyntaxToken equalsToken,
            SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken methodName,
            BlockStatementSyntax body)
            : base(withKeyword, alias, equalsToken, namespaceNames, methodName)
        {
            Body = body;
        }

        public BlockStatementSyntax Body { get; }
        public override ImmutableArray<StatementSyntax> Statements => Body.Statements;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithKeyword;
            yield return Alias;
            yield return EqualsToken;

            if (NamespaceNames is not null)
            {
                foreach (var item in NamespaceNames.GetWithSeparators())
                {
                    yield return item;
                }
            }

            yield return Identifier;
            yield return Body;
        }
    }

    public sealed class SimpleWithMethodAliasStatementSyntax : WithMethodAliasStatementSyntax
    {
        internal SimpleWithMethodAliasStatementSyntax(
            SyntaxToken withKeyword, SyntaxToken alias, SyntaxToken equalsToken,
            SeparatedSyntaxList<SyntaxToken>? namespaceNames, SyntaxToken methodName,
            SyntaxToken semicolonToken, ImmutableArray<StatementSyntax> statements)
            : base(withKeyword, alias, equalsToken, namespaceNames, methodName)
        {
            SemicolonToken = semicolonToken;
            Statements = statements;
        }

        public SyntaxToken SemicolonToken { get; }
        public override ImmutableArray<StatementSyntax> Statements { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WithKeyword;
            yield return Alias;
            yield return EqualsToken;

            if (NamespaceNames is not null)
            {
                foreach (var item in NamespaceNames.GetWithSeparators())
                {
                    yield return item;
                }
            }

            yield return Identifier;
            yield return SemicolonToken;

            foreach (var statement in Statements)
            {
                yield return statement;
            }
        }
    }
}