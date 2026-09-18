namespace WallstopStudios.NovaSharp.Interpreter.Tree.Lexer
{
    using global::NovaSharp;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Incrementally tokenizes Lua source text for the parser.
    /// </summary>
    /// <remarks>
    /// The lexer supports comment skipping, shebang handling, and Unicode-aware source tracking so
    /// downstream AST nodes can generate precise
    /// <see cref="NovaSharp.Interpreter.Debugging.SourceRef" /> locations.
    /// </remarks>
    internal class Lexer
    {
        private Token? _current;
        private readonly string _code;
        private int _prevLineTo;
        private int _prevColTo = 1;
        private int _cursor;
        private int _line = 1;
        private int _col;
        private readonly int _sourceId;
        private bool _autoSkipComments;
        private readonly LuaCompatibilityVersion _compatibilityVersion;

        /// <summary>
        /// Characters that turn a hexadecimal numeral into hex-float form (a fractional
        /// part or a p-exponent).
        /// </summary>
        private static readonly char[] FloatFormMarkers = { '.', 'p', 'P' };

        public Lexer(
            int sourceId,
            string scriptContent,
            bool autoSkipComments,
            LuaCompatibilityVersion compatibilityVersion
        )
        {
            _code = scriptContent;
            _sourceId = sourceId;

            // remove unicode BOM if any
            if (_code.Length > 0 && _code[0] == 0xFEFF)
            {
                _code = _code.Substring(1);
            }

            _autoSkipComments = autoSkipComments;
            _compatibilityVersion = compatibilityVersion;
        }

        /// <summary>
        /// Gets the token the parser is currently positioned on, lexing on demand when necessary.
        /// </summary>
        public Token Current
        {
            get
            {
                if (!_current.HasValue)
                {
                    Next();
                }

                return _current.Value;
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

        /// <summary>
        /// Advances the lexer to the next token, honoring the auto-skip comment flag.
        /// </summary>
        public void Next()
        {
            _current = FetchNewToken();
        }

        /// <summary>
        /// Returns the next token in the stream without consuming it.
        /// </summary>
        /// <returns>The lookahead token.</returns>
        public Token PeekNext()
        {
            int snapshot = _cursor;
            Token? current = _current;
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

        private static bool IsWhiteSpace(char c)
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
                    return CreateToken(TokenType.Pipe, fromLine, fromCol, "|");
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
                {
                    CursorCharNext();
                    char current = CursorChar();
                    if (current == '<')
                    {
                        CursorCharNext();
                        return CreateToken(TokenType.OpShiftLeft, fromLine, fromCol, "<<");
                    }

                    if (current == '=')
                    {
                        CursorCharNext();
                        return CreateToken(TokenType.OpLessThanEqual, fromLine, fromCol, "<=");
                    }

                    return CreateToken(TokenType.OpLessThan, fromLine, fromCol, "<");
                }
                case '>':
                {
                    CursorCharNext();
                    char current = CursorChar();
                    if (current == '>')
                    {
                        CursorCharNext();
                        return CreateToken(TokenType.OpShiftRight, fromLine, fromCol, ">>");
                    }

                    if (current == '=')
                    {
                        CursorCharNext();
                        return CreateToken(TokenType.OpGreaterThanEqual, fromLine, fromCol, ">=");
                    }

                    return CreateToken(TokenType.OpGreaterThan, fromLine, fromCol, ">");
                }
                case '~':
                {
                    CursorCharNext();
                    if (CursorChar() == '=')
                    {
                        CursorCharNext();
                        return CreateToken(TokenType.OpNotEqual, fromLine, fromCol, "~=");
                    }

                    return CreateToken(TokenType.OpBitNotOrXor, fromLine, fromCol, "~");
                }
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
                    return CreateToken(TokenType.OpNotEqual, fromLine, fromCol, "!=");
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
                    return PotentiallyDoubleCharOperator(
                        '/',
                        TokenType.OpDiv,
                        TokenType.OpFloorDiv,
                        fromLine,
                        fromCol
                    );
                case '%':
                    return CreateSingleCharToken(TokenType.OpMod, fromLine, fromCol);
                case '^':
                    return CreateSingleCharToken(TokenType.OpPwr, fromLine, fromCol);
                case '&':
                    return CreateSingleCharToken(TokenType.OpBitAnd, fromLine, fromCol);
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
                            int nameStart = _cursor;
                            int nameLength = ReadNameTokenLength();
                            return CreateNameToken(nameStart, nameLength, fromLine, fromCol);
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
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();
            string endPattern = "]";

            if (startpattern == null)
            {
                for (char c = CursorChar(); ; c = CursorCharNext())
                {
                    if (c == '\0' || !CursorNotEof())
                    {
                        string partial = text.ToString();
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
                    string partial = text.ToString();
                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unfinished long {0} near '{1}'",
                        subtypeforerrors,
                        partial
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
            // Reference Lua's numeral scanner differs by version (llex.c read_numeral):
            // Lua 5.1 consumes digits and dots, an optional Ee exponent with one optional
            // sign, then a trailing alphanumeric/underscore run and lets strtod accept or
            // reject the whole buffer; Lua 5.2+ scan exponent marks, hexadecimal digits,
            // and dots after an optional 0x prefix, and only Lua 5.4+ fold trailing
            // alphanumerics into the token as malformed-number garbage.
            return _compatibilityVersion == LuaCompatibilityVersion.Lua51
                ? ReadNumberTokenLua51(fromLine, fromCol, leadingDot)
                : ReadNumberTokenLua52Plus(fromLine, fromCol, leadingDot);
        }

        /// <summary>
        /// Scans a numeral exactly like reference Lua 5.1.5 (<c>llex.c</c>
        /// <c>read_numeral</c>). Whether the scanned text is a valid numeral is decided
        /// later by <see cref="LuaNumber.TryParse"/>, which reproduces reference 5.1
        /// accepting <c>0x1p4</c> (strtod hex-float) while rejecting <c>0x1.5</c>
        /// (scanned as <c>0x1</c> then <c>.5</c>), <c>0x.8</c> (buffer <c>0x</c>), and
        /// <c>0x8p-3</c> (buffer <c>0x8p</c>; the signed exponent stops the scan).
        /// </summary>
        private Token ReadNumberTokenLua51(int fromLine, int fromCol, bool leadingDot)
        {
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();

            if (leadingDot)
            {
                // The leading dot stays in the token text; reference reports the raw
                // source text ('.5e', not '0.5e') in malformed-number errors.
                text.Append('.');
            }

            // Reference 5.1 has no 0x prefix check: the digits-and-dots scan runs first
            // (consuming just '0' of '0x1A'), so '0x1.5' keeps its dot for the next
            // token instead of scanning as one hex float.
            bool floatForm = false;
            for (
                char c = CursorChar();
                CursorNotEof() && (LexerUtils.CharIsDigit(c) || c == '.');
                c = CursorCharNext()
            )
            {
                text.Append(c);
            }

            if (CursorNotEof() && (CursorChar() == 'e' || CursorChar() == 'E'))
            {
                text.Append(CursorChar());
                CursorCharNext();
                char exponentSign = CursorChar();
                if (CursorNotEof() && (exponentSign == '+' || exponentSign == '-'))
                {
                    text.Append(exponentSign);
                    CursorCharNext();
                }
            }

            // The trailing alphanumeric/underscore run is what consumes the '0x' prefix
            // and any p-exponent; strtod then accepts ('0x1p4') or rejects ('0xg') the
            // whole buffer exactly like reference.
            for (
                char c = CursorChar();
                CursorNotEof() && IsAsciiAlphaNumericOrUnderscore(c);
                c = CursorCharNext()
            )
            {
                text.Append(c);
            }

            string tokenStr = text.ToString();
            bool isHex =
                tokenStr.Length >= 2
                && tokenStr[0] == '0'
                && (tokenStr[1] == 'x' || tokenStr[1] == 'X');
            if (isHex && tokenStr.IndexOfAny(FloatFormMarkers) >= 0)
            {
                floatForm = true;
            }

            return CreateValidatedNumberToken(
                ClassifyNumberTokenType(isHex, floatForm),
                fromLine,
                fromCol,
                tokenStr
            );
        }

        /// <summary>
        /// Scans a numeral like reference Lua 5.2-5.5 (<c>llex.c</c> <c>read_numeral</c>):
        /// an optional <c>0x</c>/<c>0X</c> prefix switches the exponent marker from
        /// <c>e</c>/<c>E</c> to <c>p</c>/<c>P</c>; the scan then consumes exponent marks
        /// (each with one optional sign) or hexadecimal digits and dots. Lua 5.4+
        /// additionally folds trailing alphanumerics into the token so garbage like
        /// <c>0x1g</c> or <c>1e5x</c> raises a malformed-number error, while Lua
        /// 5.2/5.3 split it off as a name.
        /// </summary>
        private Token ReadNumberTokenLua52Plus(int fromLine, int fromCol, bool leadingDot)
        {
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();

            bool isHex = false;
            bool floatForm = false;
            char exponentMarkLower = 'e';
            char exponentMarkUpper = 'E';
            if (leadingDot)
            {
                // The leading dot stays in the token text; reference reports the raw
                // source text ('.5e', not '0.5e') in malformed-number errors.
                text.Append('.');
            }

            char firstChar = CursorChar();
            text.Append(firstChar);
            char secondChar = CursorCharNext();
            if (firstChar == '0' && (secondChar == 'x' || secondChar == 'X'))
            {
                isHex = true;
                exponentMarkLower = 'p';
                exponentMarkUpper = 'P';
                text.Append(secondChar);
                CursorCharNext();
            }

            bool foldsTrailingGarbage = _compatibilityVersion >= LuaCompatibilityVersion.Lua54;
            while (CursorNotEof())
            {
                char c = CursorChar();
                if (c == exponentMarkLower || c == exponentMarkUpper)
                {
                    floatForm |= isHex;
                    text.Append(c);
                    CursorCharNext();
                    char exponentSign = CursorChar();
                    if (CursorNotEof() && (exponentSign == '+' || exponentSign == '-'))
                    {
                        text.Append(exponentSign);
                        CursorCharNext();
                    }
                }
                else if (LexerUtils.CharIsHexDigit(c) || c == '.')
                {
                    floatForm |= isHex && c == '.';
                    text.Append(c);
                    CursorCharNext();
                }
                else if (foldsTrailingGarbage && IsAsciiAlphaNumeric(c))
                {
                    text.Append(c);
                    CursorCharNext();
                }
                else
                {
                    break;
                }
            }

            return CreateValidatedNumberToken(
                ClassifyNumberTokenType(isHex, floatForm),
                fromLine,
                fromCol,
                text.ToString()
            );
        }

        /// <summary>
        /// Creates a numeral token, raising the reference-shaped
        /// <c>malformed number near '&lt;text&gt;'</c> syntax error immediately when the
        /// scanned text is not a numeral of the running profile. Reference Lua detects
        /// malformed numerals in the lexer, before the parser observes the token (e.g.
        /// <c>print(0xA.8p0)</c> reports the malformed <c>.8p0</c>, not a missing
        /// parenthesis), so NovaSharp validates at the same point.
        /// </summary>
        private Token CreateValidatedNumberToken(
            TokenType tokenType,
            int fromLine,
            int fromCol,
            string tokenStr
        )
        {
            Token token = CreateToken(tokenType, fromLine, fromCol, tokenStr);
            if (!LuaNumber.TryParse(tokenStr, _compatibilityVersion, out _))
            {
                throw new SyntaxErrorException(token, "malformed number near '{0}'", tokenStr);
            }

            return token;
        }

        private static TokenType ClassifyNumberTokenType(bool isHex, bool floatForm)
        {
            if (!isHex)
            {
                return TokenType.Number;
            }

            return floatForm ? TokenType.NumberHexFloat : TokenType.NumberHex;
        }

        private static bool IsAsciiAlphaNumeric(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || LexerUtils.CharIsDigit(c);
        }

        private static bool IsAsciiAlphaNumericOrUnderscore(char c)
        {
            return IsAsciiAlphaNumeric(c) || c == '_';
        }

        private Token CreateSingleCharToken(TokenType tokenType, int fromLine, int fromCol)
        {
            CursorCharNext();
            return CreateFixedSyntaxToken(tokenType, fromLine, fromCol);
        }

        private Token ReadHashBang(int fromLine, int fromCol)
        {
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();

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
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();

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
            using Utf16ValueStringBuilder text = ZStringBuilder.CreateNested();
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
                    string partial = text.ToString();
                    throw new SyntaxErrorException(
                        CreateToken(TokenType.Invalid, fromLine, fromCol),
                        "unfinished string near '{0}'",
                        partial
                    );
                }
                else if (c == separator)
                {
                    CursorCharNext();
                    Token t = CreateToken(TokenType.String, fromLine, fromCol);
                    return t.WithText(LexerUtils.UnescapeLuaString(t, text.ToString()));
                }
                else
                {
                    text.Append(c);
                }
            }

            string unfinished = text.ToString();
            throw new SyntaxErrorException(
                CreateToken(TokenType.Invalid, fromLine, fromCol),
                "unfinished string near '{0}'",
                unfinished
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
            CursorCharNext();

            if (CursorChar() == expectedSecondChar)
            {
                CursorCharNext();
                return CreateFixedSyntaxToken(doubleCharToken, fromLine, fromCol);
            }
            else
            {
                return CreateFixedSyntaxToken(singleCharToken, fromLine, fromCol);
            }
        }

        private Token CreateFixedSyntaxToken(TokenType tokenType, int fromLine, int fromCol)
        {
            if (Token.TryGetFixedSyntaxText(tokenType, out string text))
            {
                return CreateToken(tokenType, fromLine, fromCol, text);
            }

            throw new InternalErrorException(
                "Token type '{0}' does not have fixed syntax text.",
                tokenType
            );
        }

        private Token CreateNameToken(int nameStart, int nameLength, int fromLine, int fromCol)
        {
            if (
                Token.TryGetReservedTokenType(
                    _code,
                    nameStart,
                    nameLength,
                    out TokenType reservedType,
                    out string reservedText
                )
            )
            {
                return CreateToken(reservedType, fromLine, fromCol, reservedText);
            }

            string name = _code.Substring(nameStart, nameLength);
            return CreateToken(TokenType.Name, fromLine, fromCol, name);
        }

        private Token CreateToken(
            TokenType tokenType,
            int fromLine,
            int fromCol,
            string text = null
        )
        {
            Token t = new Token(
                tokenType,
                _sourceId,
                fromLine,
                fromCol,
                _line,
                _col,
                _prevLineTo,
                _prevColTo,
                text
            );
            _prevLineTo = _line;
            _prevColTo = _col;
            return t;
        }

        private int ReadNameTokenLength()
        {
            int start = _cursor;
            while (CursorNotEof())
            {
                char c = CursorChar();
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    CursorNext();
                }
                else
                {
                    break;
                }
            }

            return _cursor - start;
        }
    }
}
