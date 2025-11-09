namespace NovaSharp.Interpreter.Tree
{
    using Execution;
    using Execution.VM;

    internal abstract class NodeBase
    {
        public Script Script { get; private set; }
        protected ScriptLoadingContext LoadingContext { get; private set; }

        public NodeBase(ScriptLoadingContext lcontext)
        {
            Script = lcontext.Script;
        }

        public abstract void Compile(ByteCode bc);

        protected static Token UnexpectedTokenType(Token t)
        {
            throw new SyntaxErrorException(t, "unexpected symbol near '{0}'", t.Text)
            {
                IsPrematureStreamTermination = (t.type == TokenType.Eof),
            };
        }

        protected static Token CheckTokenType(ScriptLoadingContext lcontext, TokenType tokenType)
        {
            Token t = lcontext.Lexer.Current;
            if (t.type != tokenType)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        protected static Token CheckTokenType(
            ScriptLoadingContext lcontext,
            TokenType tokenType1,
            TokenType tokenType2
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.type != tokenType1 && t.type != tokenType2)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        protected static Token CheckTokenType(
            ScriptLoadingContext lcontext,
            TokenType tokenType1,
            TokenType tokenType2,
            TokenType tokenType3
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.type != tokenType1 && t.type != tokenType2 && t.type != tokenType3)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        protected static void CheckTokenTypeNotNext(
            ScriptLoadingContext lcontext,
            TokenType tokenType
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.type != tokenType)
            {
                UnexpectedTokenType(t);
            }
        }

        protected static Token CheckMatch(
            ScriptLoadingContext lcontext,
            Token originalToken,
            TokenType expectedTokenType,
            string expectedTokenText
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.type != expectedTokenType)
            {
                throw new SyntaxErrorException(
                    lcontext.Lexer.Current,
                    "'{0}' expected (to close '{1}' at line {2}) near '{3}'",
                    expectedTokenText,
                    originalToken.Text,
                    originalToken.fromLine,
                    t.Text
                )
                {
                    IsPrematureStreamTermination = (t.type == TokenType.Eof),
                };
            }

            lcontext.Lexer.Next();

            return t;
        }
    }
}
