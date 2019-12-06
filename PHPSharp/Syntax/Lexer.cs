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
        private object? _value;

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

            SyntaxKind? kind = LookupTokenKind();
            if (kind != null)
                _kind = (SyntaxKind)kind;
            else
            {
                switch (Current)
                {
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
            }

            string? text = SyntaxFacts.GetText(_kind);
            if (text is null)
            {
                int length = _position - _start;
                text = _text.ToString(_start, length);
            }

            return new SyntaxToken(_kind, _start, text, _value);
        }

        private SyntaxKind? LookupTokenKind()
        {
            switch (Current)
            {
                case '\0':
                    return SyntaxKind.EndOfFileToken;

                case ';':
                    _position++;
                    return SyntaxKind.SemicolonToken;

                case '+':
                    _position++;
                    if (Current == '+')
                    {
                        _position++;
                        return SyntaxKind.PlusPlusToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.PlusEqualsToken;
                    }
                    else
                        return SyntaxKind.PlusToken;

                case '-':
                    _position++;
                    if (Current == '-')
                    {
                        _position++;
                        return SyntaxKind.MinusMinusToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.MinusEqualsToken;
                    }
                    else
                        return SyntaxKind.MinusToken;

                case '*':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.StarEqualsToken;
                    }
                    else
                        return SyntaxKind.StarToken;

                case '/':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.SlashEqualsToken;
                    }
                    else
                        return SyntaxKind.SlashToken;

                case '(':
                    _position++;
                    return SyntaxKind.OpenParenthesisToken;

                case ')':
                    _position++;
                    return SyntaxKind.CloseParenthesisToken;

                case '{':
                    _position++;
                    return SyntaxKind.OpenBraceToken;

                case '}':
                    _position++;
                    return SyntaxKind.CloseBraceToken;

                case '~':
                    _position++;
                    return SyntaxKind.TildeToken;

                case '^':
                    _position++;
                    return SyntaxKind.HatToken;

                case '&':
                    _position++;
                    if (Current == '&')
                    {
                        _position++;
                        return SyntaxKind.AmpersandAmpersandToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.AmpersandEqualsToken;
                    }
                    else
                        return SyntaxKind.AmpersandToken;

                case '|':
                    _position++;
                    if (Current == '|')
                    {
                        _position++;
                        return SyntaxKind.PipePipeToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.PipeEqualsToken;
                    }
                    else
                        return SyntaxKind.PipeToken;

                case '=':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.EqualsEqualsToken;
                    }
                    else
                        return SyntaxKind.EqualsToken;

                case '!':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.BangEqualsToken;
                    }
                    else
                        return SyntaxKind.BangToken;

                case '<':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.LessOrEqualsToken;
                    }
                    else
                        return SyntaxKind.LessToken;

                case '>':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        return SyntaxKind.GreaterOrEqualsToken;
                    }
                    else
                        return SyntaxKind.GreaterToken;

                default:
                    return null;
            }
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

        private void ReadNumberToken()
        {
            while (char.IsDigit(Current))
                Next();

            _kind = SyntaxKind.NumberToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current))
                Next();

            int length = _position - _start;
            string word = _text.ToString(_start, length);

            _kind = SyntaxFacts.GetKeywordKind(word);
        }

        #endregion Private methods
    }
}