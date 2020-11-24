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
    public abstract class NamespaceDeclarationSyntax : DeclarationSyntax
    {
        protected NamespaceDeclarationSyntax(SyntaxToken namespaceToken, SeparatedSyntaxList<SyntaxToken> names, ImmutableArray<StatementSyntax> statements)
        {
            NamespaceToken = namespaceToken;
            Names = names;
            Statements = statements;
        }

        public override SyntaxKind Kind => SyntaxKind.NamespaceDeclaration;
        public SyntaxToken NamespaceToken { get; }
        public SeparatedSyntaxList<SyntaxToken> Names { get; }
        public ImmutableArray<StatementSyntax> Statements { get; }
    }

    internal sealed class BodiedNamespaceDeclarationStatementSyntax : NamespaceDeclarationSyntax
    {
        internal BodiedNamespaceDeclarationStatementSyntax(SyntaxToken namespaceToken, SeparatedSyntaxList<SyntaxToken> names, BlockStatementSyntax namespaceBody)
            : base(namespaceToken, names, namespaceBody.Statements)
        {
            NamespaceBody = namespaceBody;
        }

        public BlockStatementSyntax NamespaceBody { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NamespaceToken;
            foreach (var name in Names.GetWithSeparators())
            {
                yield return name;
            }

            yield return NamespaceBody;
        }
    }

    internal sealed class SimpleNamespaceDeclarationStatementSyntax : NamespaceDeclarationSyntax
    {
        internal SimpleNamespaceDeclarationStatementSyntax(SyntaxToken namespaceToken, SeparatedSyntaxList<SyntaxToken> names, SyntaxToken semicolon, ImmutableArray<StatementSyntax> statements)
            : base(namespaceToken, names, statements)
        {
            Semicolon = semicolon;
        }

        public SyntaxToken Semicolon { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NamespaceToken;
            foreach (var name in Names.GetWithSeparators())
            {
                yield return name;
            }

            yield return Semicolon;
            foreach (var statement in Statements)
            {
                yield return statement;
            }
        }
    }
}