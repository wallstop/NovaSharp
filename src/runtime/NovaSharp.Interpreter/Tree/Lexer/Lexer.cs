namespace NovaSharp.Interpreter.Tree
{
    using System.Text;

    internal class Lexer
    {
        private Token _current = null;
        private readonly string _code;
        private int _prevLineTo = 0;
        private int _prevColTo = 1;
        private int _cursor = 0;
        private int _line = 1;
        private int _col = 0;
        private readonly int _sourceId;
        private bool _autoSkipComments = false;

        public Lexer(int sourceId, string scriptContent, bool autoSkipComments)
        {
            _code = scriptContent;
            _sourceId = sourceId;

            // remove unicode BOM if any
            if (_code.Length > 0 && _code[0] == 0xFEFF)
            {
                _code = _code.Substring(1);
            }

            _autoSkipComments = autoSkipComments;
        }

        public Token Current
        {
            get
            {
                if (_current == null)
                {
                    Next();
                }

                return _current;
            }
        }

        private Token FetchNewToken()
        {
            while (true)
            {
                Token t = ReadToken();

                //System.Diagnostics.Debug.WriteLine("LEXER : " + T.ToString());

                if (
                    (t.type != TokenType.Comment && t.type != TokenType.HashBang)
                    || (!_autoSkipComments)
                )
                {
                    return t;
                }
            }
        }

        public void Next()
        {
            _current = FetchNewToken();
        }

        public Token PeekNext()
        {
            int snapshot = _cursor;
            Token current = _current;
            int line = _line;
            int col = _col;

            Next();
            Token t = Current;

            _cursor = snapshot;
            _current = current;
            _line = line;
            _col = col;

            return t;
        }

        private void CursorNext()
        {
            if (CursorNotEof())
            {
                if (CursorChar() == '\n')
                {
                    _col = 0;
                    _line += 1;
                }
                else
                {
                    _col += 1;
                }

                _cursor += 1;
            }
        }

        private char CursorChar()
        {
            if (_cursor < _code.Length)
            {
                return _code[_cursor];
            }
            else
            {
                return '\0'; //  sentinel
            }
        }

        private char CursorCharNext()
        {
            CursorNext();
            return CursorChar();
        }

        private bool CursorMatches(string pattern)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                int j = _cursor + i;

                if (j >= _code.Length)
                {
                    return false;
                }

                if (_code[j] != pattern[i])
                {
                    return false;
                }
            }
            return true;
        }

        private bool CursorNotEof()
        {
            return _cursor < _code.Length;
        }

        private bool IsWhiteSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        private void SkipWhiteSpace()
        {
            for (; CursorNotEof() && IsWhiteSpace(CursorChar()); CursorNext()) { }
        }

        private Token ReadToken()
        {
            SkipWhiteSpace();

            int fromLine = _line;
            int fromCol = _col;

            if (!CursorNotEof())
            {
                return CreateToken(TokenType.Eof, fromLine, fromCol, "<eof>");
            }

            char c = CursorChar();

            switch (c)
            {
                case '|':
                    CursorCharNext();
                    return CreateToken(TokenType.Lambda, fromLine, fromCol, "|");
                case ';':
                    CursorCharNext();
                    return CreateToken(TokenType.SemiColon, fromLine, fromCol, ";");
                case '=':
                    return PotentiallyDoubleCharOperator(
                        '=',
                        TokenType.OpAssignment,
                        TokenType.OpEqual,
                        fromLine,
                        fromCol
                    );
                case '<':
                    return PotentiallyDoubleCharOperator(
                        '=',
                        TokenType.OpLessThan,
                        TokenType.OpLessThanEqual,
                        fromLine,
                        fromCol
                    );
                case '>':
                    return PotentiallyDoubleCharOperator(
                        '=',
                        TokenType.OpGreaterThan,
                        TokenType.OpGreaterThanEqual,
                        fromLine,
                        fromCol
                    );
                case '~':
                case '!':
                    if (CursorCharNext() != '=')
                    {
                        throw new SyntaxErrorException(
                            CreateToken(TokenType.Invalid, fromLine, fromCol),
                            "unexpected symbol near '{0}'",
                            c
                        );
                    }

                    CursorCharNext();
                    return CreateToken(TokenType.OpNotEqual, fromLine, fromCol, "~=");
                case '.':
                {
                    char next = CursorCharNext();
                    if (next == '.')
                    {
                        return PotentiallyDoubleCharOperator(
                            '.',
                            TokenType.OpConcat,
                            TokenType.VarArgs,
                            fromLine,
                            fromCol
                        );
                    }
                    else if (LexerUtils.CharIsDigit(next))
                    {
                        return ReadNumberToken(fromLine, fromCol, true);
                    }
                    else
                    {
                        return CreateToken(TokenType.Dot, fromLine, fromCol, ".");
                    }
                }
                case '+':
                    return CreateSingleCharToken(TokenType.OpAdd, fromLine, fromCol);
                case '-':
                {
                    char next = CursorCharNext();
                    if (next == '-')
                    {
                        return ReadComment(fromLine, fromCol);
                    }
                    else
                    {
                        return CreateToken(TokenType.OpMinusOrSub, fromLine, fromCol, "-");
                    }
                }
                case '*':
                    return CreateSingleCharToken(TokenType.OpMul, fromLine, fromCol);
                case '/':
                    return CreateSingleCharToken(TokenType.OpDiv, fromLine, fromCol);
                case '%':
                    return CreateSingleCharToken(TokenType.OpMod, fromLine, fromCol);
                case '^':
                    return CreateSingleCharToken(TokenType.OpPwr, fromLine, fromCol);
                case '$':
                    return PotentiallyDoubleCharOperator(
                        '{',
                        TokenType.OpDollar,
                        TokenType.BrkOpenCurlyShared,
                        fromLine,
                        fromCol
                    );
                case '#':
                    if (_cursor == 0 && _code.Length > 1 && _code[1] == '!')
                    {
                        return ReadHashBang(fromLine, fromCol);
                    }

                    return CreateSingleCharToken(TokenType.OpLen, fromLine, fromCol);
                case '[':
                {
                    char next = CursorCharNext();
                    if (next == '=' || next == '[')
                    {
                        string str = ReadLongString(fromLine, fromCol, null, "string");
                        return CreateToken(TokenType.StringLong, fromLine, fromCol, str);
                    }
                    return CreateToken(TokenType.BrkOpenSquare, fromLine, fromCol, "[");
                }
                case ']':
                    return CreateSingleCharToken(TokenType.BrkCloseSquare, fromLine, fromCol);
                case '(':
                    return CreateSingleCharToken(TokenType.BrkOpenRound, fromLine, fromCol);
                case ')':
                    return CreateSingleCharToken(TokenType.BrkCloseRound, fromLine, fromCol);
                case '{':
                    return CreateSingleCharToken(TokenType.BrkOpenCurly, fromLine, fromCol);
                case '}':
                    return CreateSingleCharToken(TokenType.BrkCloseCurly, fromLine, fromCol);
                case ',':
                    return CreateSingleCharToken(TokenType.Comma, fromLine, fromCol);
                case ':':
                    return PotentiallyDoubleCharOperator(
                        ':',
                        TokenType.Colon,
                        TokenType.DoubleColon,
                        fromLine,
                        fromCol
                    );
                case '"':
                case '\'':
                    return ReadSimpleStringToken(fromLine, fromCol);
                case '\0':
                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unexpected symbol near '{0}'",
                        CursorChar()
                    )
                    {
                        IsPrematureStreamTermination = true,
                    };
                default:
                    {
                        if (char.IsLetter(c) || c == '_')
                        {
                            string name = ReadNameToken();
                            return CreateNameToken(name, fromLine, fromCol);
                        }
                        else if (LexerUtils.CharIsDigit(c))
                        {
                            return ReadNumberToken(fromLine, fromCol, false);
                        }
                    }

                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unexpected symbol near '{0}'",
                        CursorChar()
                    );
            }
        }

        private string ReadLongString(
            int fromLine,
            int fromCol,
            string startpattern,
            string subtypeforerrors
        )
        {
            // here we are at the first '=' or second '['
            StringBuilder text = new(1024);
            string endPattern = "]";

            if (startpattern == null)
            {
                for (char c = CursorChar(); ; c = CursorCharNext())
                {
                    if (c == '\0' || !CursorNotEof())
                    {
                        throw new SyntaxErrorException(
                            CreateToken(TokenType.Invalid, fromLine, fromCol),
                            "unfinished long {0} near '<eof>'",
                            subtypeforerrors
                        )
                        {
                            IsPrematureStreamTermination = true,
                        };
                    }
                    else if (c == '=')
                    {
                        endPattern += "=";
                    }
                    else if (c == '[')
                    {
                        endPattern += "]";
                        break;
                    }
                    else
                    {
                        throw new SyntaxErrorException(
                            CreateToken(TokenType.Invalid, fromLine, fromCol),
                            "invalid long {0} delimiter near '{1}'",
                            subtypeforerrors,
                            c
                        )
                        {
                            IsPrematureStreamTermination = true,
                        };
                    }
                }
            }
            else
            {
                endPattern = startpattern.Replace('[', ']');
            }

            for (char c = CursorCharNext(); ; c = CursorCharNext())
            {
                if (c == '\r') // XXI century and we still debate on how a newline is made. throw new DeveloperExtremelyAngryException.
                {
                    continue;
                }

                if (c == '\0' || !CursorNotEof())
                {
                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unfinished long {0} near '{1}'",
                        subtypeforerrors,
                        text.ToString()
                    )
                    {
                        IsPrematureStreamTermination = true,
                    };
                }
                else if (c == ']' && CursorMatches(endPattern))
                {
                    for (int i = 0; i < endPattern.Length; i++)
                    {
                        CursorCharNext();
                    }

                    return LexerUtils.AdjustLuaLongString(text.ToString());
                }
                else
                {
                    text.Append(c);
                }
            }
        }

        private Token ReadNumberToken(int fromLine, int fromCol, bool leadingDot)
        {
            StringBuilder text = new(32);

            //INT : Digit+
            //HEX : '0' [xX] HexDigit+
            //FLOAT : Digit+ '.' Digit* ExponentPart?
            //		| '.' Digit+ ExponentPart?
            //		| Digit+ ExponentPart
            //HEX_FLOAT : '0' [xX] HexDigit+ '.' HexDigit* HexExponentPart?
            //			| '0' [xX] '.' HexDigit+ HexExponentPart?
            //			| '0' [xX] HexDigit+ HexExponentPart
            //
            // ExponentPart : [eE] [+-]? Digit+
            // HexExponentPart : [pP] [+-]? Digit+

            bool isHex = false;
            bool dotAdded = false;
            bool exponentPart = false;
            bool exponentSignAllowed = false;

            if (leadingDot)
            {
                text.Append("0.");
            }
            else if (CursorChar() == '0')
            {
                text.Append(CursorChar());
                char secondChar = CursorCharNext();

                if (secondChar == 'x' || secondChar == 'X')
                {
                    isHex = true;
                    text.Append(CursorChar());
                    CursorCharNext();
                }
            }

            for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
            {
                if (exponentSignAllowed && (c == '+' || c == '-'))
                {
                    exponentSignAllowed = false;
                    text.Append(c);
                }
                else if (LexerUtils.CharIsDigit(c))
                {
                    text.Append(c);
                }
                else if (c == '.' && !dotAdded)
                {
                    dotAdded = true;
                    text.Append(c);
                }
                else if (LexerUtils.CharIsHexDigit(c) && isHex && !exponentPart)
                {
                    text.Append(c);
                }
                else if (c == 'e' || c == 'E' || (isHex && (c == 'p' || c == 'P')))
                {
                    text.Append(c);
                    exponentPart = true;
                    exponentSignAllowed = true;
                    dotAdded = true;
                }
                else
                {
                    break;
                }
            }

            TokenType numberType = TokenType.Number;

            if (isHex && (dotAdded || exponentPart))
            {
                numberType = TokenType.NumberHexFloat;
            }
            else if (isHex)
            {
                numberType = TokenType.NumberHex;
            }

            string tokenStr = text.ToString();
            return CreateToken(numberType, fromLine, fromCol, tokenStr);
        }

        private Token CreateSingleCharToken(TokenType tokenType, int fromLine, int fromCol)
        {
            char c = CursorChar();
            CursorCharNext();
            return CreateToken(tokenType, fromLine, fromCol, c.ToString());
        }

        private Token ReadHashBang(int fromLine, int fromCol)
        {
            StringBuilder text = new(32);

            for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
            {
                if (c == '\n')
                {
                    CursorCharNext();
                    return CreateToken(TokenType.HashBang, fromLine, fromCol, text.ToString());
                }
                else if (c != '\r')
                {
                    text.Append(c);
                }
            }

            return CreateToken(TokenType.HashBang, fromLine, fromCol, text.ToString());
        }

        private Token ReadComment(int fromLine, int fromCol)
        {
            StringBuilder text = new(32);

            bool extraneousFound = false;

            for (char c = CursorCharNext(); CursorNotEof(); c = CursorCharNext())
            {
                if (c == '[' && !extraneousFound && text.Length > 0)
                {
                    text.Append('[');
                    //CursorCharNext();
                    string comment = ReadLongString(fromLine, fromCol, text.ToString(), "comment");
                    return CreateToken(TokenType.Comment, fromLine, fromCol, comment);
                }
                else if (c == '\n')
                {
                    extraneousFound = true;
                    CursorCharNext();
                    return CreateToken(TokenType.Comment, fromLine, fromCol, text.ToString());
                }
                else if (c != '\r')
                {
                    if (c != '[' && c != '=')
                    {
                        extraneousFound = true;
                    }

                    text.Append(c);
                }
            }

            return CreateToken(TokenType.Comment, fromLine, fromCol, text.ToString());
        }

        private Token ReadSimpleStringToken(int fromLine, int fromCol)
        {
            StringBuilder text = new(32);
            char separator = CursorChar();

            for (char c = CursorCharNext(); CursorNotEof(); c = CursorCharNext())
            {
                redo_Loop:

                if (c == '\\')
                {
                    text.Append(c);
                    c = CursorCharNext();
                    text.Append(c);

                    if (c == '\r')
                    {
                        c = CursorCharNext();
                        if (c == '\n')
                        {
                            text.Append(c);
                        }
                        else
                        {
                            goto redo_Loop;
                        }
                    }
                    else if (c == 'z')
                    {
                        c = CursorCharNext();

                        if (char.IsWhiteSpace(c))
                        {
                            SkipWhiteSpace();
                        }

                        c = CursorChar();

                        goto redo_Loop;
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unfinished string near '{0}'",
                        text.ToString()
                    );
                }
                else if (c == separator)
                {
                    CursorCharNext();
                    Token t = CreateToken(TokenType.String, fromLine, fromCol);
                    t.Text = LexerUtils.UnescapeLuaString(t, text.ToString());
                    return t;
                }
                else
                {
                    text.Append(c);
                }
            }

            throw new SyntaxErrorException(
                CreateToken(TokenType.Invalid, fromLine, fromCol),
                "unfinished string near '{0}'",
                text.ToString()
            )
            {
                IsPrematureStreamTermination = true,
            };
        }

        private Token PotentiallyDoubleCharOperator(
            char expectedSecondChar,
            TokenType singleCharToken,
            TokenType doubleCharToken,
            int fromLine,
            int fromCol
        )
        {
            string op = CursorChar().ToString();

            CursorCharNext();

            if (CursorChar() == expectedSecondChar)
            {
                CursorCharNext();
                return CreateToken(doubleCharToken, fromLine, fromCol, op + expectedSecondChar);
            }
            else
            {
                return CreateToken(singleCharToken, fromLine, fromCol, op);
            }
        }

        private Token CreateNameToken(string name, int fromLine, int fromCol)
        {
            TokenType? reservedType = Token.GetReservedTokenType(name);

            if (reservedType.HasValue)
            {
                return CreateToken(reservedType.Value, fromLine, fromCol, name);
            }
            else
            {
                return CreateToken(TokenType.Name, fromLine, fromCol, name);
            }
        }

        private Token CreateToken(
            TokenType tokenType,
            int fromLine,
            int fromCol,
            string text = null
        )
        {
            Token t = new(
                tokenType,
                _sourceId,
                fromLine,
                fromCol,
                _line,
                _col,
                _prevLineTo,
                _prevColTo
            )
            {
                Text = text,
            };
            _prevLineTo = _line;
            _prevColTo = _col;
            return t;
        }

        private string ReadNameToken()
        {
            StringBuilder name = new(32);

            for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    name.Append(c);
                }
                else
                {
                    break;
                }
            }

            return name.ToString();
        }
    }
}
