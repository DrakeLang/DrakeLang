//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
// Copyright (C) 2019  Niklas Gransjøen
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
// along with this program.  If not, see https://www.gnu.org/licenses/.
//------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PHPSharp.Syntax
{
    internal class Parser
    {
        private readonly SyntaxToken[] _tokens;

        private readonly List<string> _diagnostics = new List<string>();
        private int _position;

        public Parser(string text)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>();

            Lexer lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind != SyntaxKind.BadToken)
                    tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        #region Properties

        public IEnumerable<string> Diagnostics => _diagnostics;

        #endregion Properties

        #region Private properties

        private SyntaxToken Current => Peek(0);

        #endregion Private properties

        #region Methods

        public SyntaxTree Parse()
        {
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);

            return new SyntaxTree(_diagnostics, expression, endOfFileToken);
        }

        #endregion Methods

        #region Private methods

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;
            _position++;

            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.Add($"ERROR: Unexpected token <{Current.Kind}>, expected <{kind}>");
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence > parentPrecedence)
            {
                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax operand = ParseExpression(unaryOperatorPrecedence);

                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
            {
                switch (Current.Kind)
                {
                    case SyntaxKind.OpenParenthesisToken:
                        SyntaxToken leftParenthesis = NextToken();
                        ExpressionSyntax expression = ParseExpression();
                        SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

                        left = new ParenthesizedExpressionSyntax(leftParenthesis, expression, rightParenthesis);
                        break;

                    case SyntaxKind.TrueKeyword:
                    case SyntaxKind.FalseKeyword:
                        SyntaxToken keywordToken = NextToken();
                        left = new LiteralExpressionSyntax(keywordToken, keywordToken.Kind == SyntaxKind.TrueKeyword);
                        break;

                    default:
                        SyntaxToken numberToken = MatchToken(SyntaxKind.NumberToken);
                        left = new LiteralExpressionSyntax(numberToken);
                        break;
                }
            }

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence != 0 && precedence > parentPrecedence)
                {
                    SyntaxToken operatorToken = NextToken();
                    ExpressionSyntax right = ParseExpression(precedence);
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }
                else break;
            }

            return left;
        }

        #endregion Private methods
    }
}