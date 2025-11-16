namespace NovaSharp.Interpreter.Tree
{
    using System.Collections.Generic;
    using Expressions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal abstract class Expression : NodeBase
    {
        public Expression(ScriptLoadingContext lcontext)
            : base(lcontext) { }

        public virtual string GetFriendlyDebugName()
        {
            return null;
        }

        public abstract DynValue Eval(ScriptExecutionContext context);

        public virtual SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return null;
        }

        internal static List<Expression> ExprListAfterFirstExpr(
            ScriptLoadingContext lcontext,
            Expression expr1
        )
        {
            List<Expression> exps = new();

            exps.Add(expr1);

            while ((lcontext.Lexer.Current.type == TokenType.Comma))
            {
                lcontext.Lexer.Next();
                exps.Add(Expr(lcontext));
            }

            return exps;
        }

        internal static List<Expression> ExprList(ScriptLoadingContext lcontext)
        {
            List<Expression> exps = new();

            while (true)
            {
                exps.Add(Expr(lcontext));

                if (lcontext.Lexer.Current.type != TokenType.Comma)
                {
                    break;
                }

                lcontext.Lexer.Next();
            }

            return exps;
        }

        internal static Expression Expr(ScriptLoadingContext lcontext)
        {
            return SubExpr(lcontext, true);
        }

        internal static Expression SubExpr(ScriptLoadingContext lcontext, bool isPrimary)
        {
            Expression e = null;

            Token t = lcontext.Lexer.Current;

            if (t.IsUnaryOperator())
            {
                lcontext.Lexer.Next();
                e = SubExpr(lcontext, false);

                // check for power operator -- it be damned forever and ever for being higher priority than unary ops
                Token unaryOp = t;
                t = lcontext.Lexer.Current;

                if (isPrimary && t.type == TokenType.OpPwr)
                {
                    List<Expression> powerChain = new();
                    powerChain.Add(e);

                    while (isPrimary && t.type == TokenType.OpPwr)
                    {
                        lcontext.Lexer.Next();
                        powerChain.Add(SubExpr(lcontext, false));
                        t = lcontext.Lexer.Current;
                    }

                    e = powerChain[^1];

                    for (int i = powerChain.Count - 2; i >= 0; i--)
                    {
                        e = BinaryOperatorExpression.CreatePowerExpression(
                            powerChain[i],
                            e,
                            lcontext
                        );
                    }
                }

                e = new UnaryOperatorExpression(lcontext, e, unaryOp);
            }
            else
            {
                e = SimpleExp(lcontext);
            }

            t = lcontext.Lexer.Current;

            if (isPrimary && t.IsBinaryOperator())
            {
                object chain = BinaryOperatorExpression.BeginOperatorChain();

                BinaryOperatorExpression.AddExpressionToChain(chain, e);

                while (t.IsBinaryOperator())
                {
                    BinaryOperatorExpression.AddOperatorToChain(chain, t);
                    lcontext.Lexer.Next();
                    Expression right = SubExpr(lcontext, false);
                    BinaryOperatorExpression.AddExpressionToChain(chain, right);
                    t = lcontext.Lexer.Current;
                }

                e = BinaryOperatorExpression.CommitOperatorChain(chain, lcontext);
            }

            return e;
        }

        internal static Expression SimpleExp(ScriptLoadingContext lcontext)
        {
            Token t = lcontext.Lexer.Current;

            switch (t.type)
            {
                case TokenType.Number:
                case TokenType.NumberHex:
                case TokenType.NumberHexFloat:
                case TokenType.String:
                case TokenType.StringLong:
                case TokenType.Nil:
                case TokenType.True:
                case TokenType.False:
                    return new LiteralExpression(lcontext, t);
                case TokenType.VarArgs:
                    return new SymbolRefExpression(t, lcontext);
                case TokenType.BrkOpenCurly:
                case TokenType.BrkOpenCurlyShared:
                    return new TableConstructor(lcontext, t.type == TokenType.BrkOpenCurlyShared);
                case TokenType.Function:
                    lcontext.Lexer.Next();
                    return new FunctionDefinitionExpression(lcontext, false, false);
                case TokenType.Lambda:
                    return new FunctionDefinitionExpression(lcontext, false, true);
                default:
                    return PrimaryExp(lcontext);
            }
        }

        /// <summary>
        /// Primaries the exp.
        /// </summary>
        /// <param name="lcontext">The lcontext.</param>
        /// <returns></returns>
        internal static Expression PrimaryExp(ScriptLoadingContext lcontext)
        {
            Expression e = PrefixExp(lcontext);

            while (true)
            {
                Token t = lcontext.Lexer.Current;
                Token thisCallName = null;

                switch (t.type)
                {
                    case TokenType.Dot:
                        {
                            lcontext.Lexer.Next();
                            Token name = CheckTokenType(lcontext, TokenType.Name);
                            e = new IndexExpression(e, name.Text, lcontext);
                        }
                        break;
                    case TokenType.BrkOpenSquare:
                        {
                            Token openBrk = lcontext.Lexer.Current;
                            lcontext.Lexer.Next(); // skip bracket
                            Expression index = Expr(lcontext);

                            // support NovaSharp multiple indexers for userdata
                            if (lcontext.Lexer.Current.type == TokenType.Comma)
                            {
                                List<Expression> explist = ExprListAfterFirstExpr(lcontext, index);
                                index = new ExprListExpression(explist, lcontext);
                            }

                            CheckMatch(lcontext, openBrk, TokenType.BrkCloseSquare, "]");
                            e = new IndexExpression(e, index, lcontext);
                        }
                        break;
                    case TokenType.Colon:
                        lcontext.Lexer.Next();
                        thisCallName = CheckTokenType(lcontext, TokenType.Name);
                        goto case TokenType.BrkOpenRound;
                    case TokenType.BrkOpenRound:
                    case TokenType.String:
                    case TokenType.StringLong:
                    case TokenType.BrkOpenCurly:
                    case TokenType.BrkOpenCurlyShared:
                        e = new FunctionCallExpression(lcontext, e, thisCallName);
                        break;
                    default:
                        return e;
                }
            }
        }

        private static Expression PrefixExp(ScriptLoadingContext lcontext)
        {
            Token t = lcontext.Lexer.Current;
            switch (t.type)
            {
                case TokenType.BrkOpenRound:
                    lcontext.Lexer.Next();
                    Expression e = Expr(lcontext);
                    e = new AdjustmentExpression(lcontext, e);
                    CheckMatch(lcontext, t, TokenType.BrkCloseRound, ")");
                    return e;
                case TokenType.Name:
                    return new SymbolRefExpression(t, lcontext);
                default:
                    throw new SyntaxErrorException(t, "unexpected symbol near '{0}'", t.Text)
                    {
                        IsPrematureStreamTermination = (t.type == TokenType.Eof),
                    };
            }
        }
    }
}
