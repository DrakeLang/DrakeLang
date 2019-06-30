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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using PHPSharp.Text;

namespace PHPSharp.Syntax
{
    internal class Lexer
    {
        private readonly SourceText _text;

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object _value;

        public Lexer(SourceText text)
        {
            _text = text;
        }

        #region Public properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        #endregion Public properties

        #region Private properties

        private char Current => Peek(0);

        #endregion Private properties

        #region Methods

        public SyntaxToken Lex()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;

                case ';':
                    _kind = SyntaxKind.SemicolonToken;
                    _position++;
                    break;

                case '+':
                    _position++;
                    if (Current == '+')
                    {
                        _kind = SyntaxKind.PlusPlusToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.PlusToken;
                    break;

                case '-':
                    _position++;
                    if (Current == '-')
                    {
                        _kind = SyntaxKind.MinusMinusToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.MinusToken;
                    break;

                case '*':
                    _kind = SyntaxKind.StarToken;
                    _position++;
                    break;

                case '/':
                    _kind = SyntaxKind.SlashToken;
                    _position++;
                    break;

                case '(':
                    _kind = SyntaxKind.OpenParenthesisToken;
                    _position++;
                    break;

                case ')':
                    _kind = SyntaxKind.CloseParenthesisToken;
                    _position++;
                    break;

                case '{':
                    _kind = SyntaxKind.OpenBraceToken;
                    _position++;
                    break;

                case '}':
                    _kind = SyntaxKind.CloseBraceToken;
                    _position++;
                    break;

                case '&':
                    _position++;
                    if (Current == '&')
                    {
                        _kind = SyntaxKind.AmpersandAmpersandToken;
                        _position++;
                    }

                    break;

                case '|':
                    _position++;
                    if (Current == '|')
                    {
                        _kind = SyntaxKind.PipePipeToken;
                        _position++;
                    }
                    break;

                case '=':
                    _position++;
                    if (Current == '=')
                    {
                        _kind = SyntaxKind.EqualsEqualsToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.EqualsToken;
                    break;

                case '!':
                    _position++;
                    if (Current == '=')
                    {
                        _kind = SyntaxKind.BangEqualsToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.BangToken;
                    break;

                case '<':
                    _position++;
                    if (Current == '=')
                    {
                        _kind = SyntaxKind.LessOrEqualsToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.LessToken;

                    break;

                case '>':
                    _position++;
                    if (Current == '=')
                    {
                        _kind = SyntaxKind.GreaterOrEqualsToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.GreaterToken;
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    ReadNumberToken();
                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    ReadWhitespace();
                    break;

                default:
                    if (char.IsLetter(Current))
                        ReadIdentifierOrKeyword();
                    else if (char.IsWhiteSpace(Current))
                        ReadWhitespace();
                    else
                    {
                        Diagnostics.ReportBadCharacter(_position, Current);
                        _position++;
                    }
                    break;
            }

            string text = SyntaxFacts.GetText(_kind);
            if (text == null)
            {
                int length = _position - _start;
                text = _text.ToString(_start, length);
            }

            return new SyntaxToken(_kind, _start, text, _value);
        }

        #endregion Methods

        #region Private methods

        private char Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _text.Length)
                return '\0';

            return _text[index];
        }

        private void Next()
        {
            _position++;
        }

        private void ReadWhitespace()
        {
            while (char.IsWhiteSpace(Current))
                Next();

            _kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current))
                Next();

            int length = _position - _start;
            string word = _text.ToString(_start, length);

            _kind = SyntaxFacts.GetKeywordKind(word);
        }

        private void ReadNumberToken()
        {
            while (char.IsDigit(Current))
                Next();

            _kind = SyntaxKind.NumberToken;
        }

        #endregion Private methods
    }
}