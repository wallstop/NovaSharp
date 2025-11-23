namespace NovaSharp.Interpreter.Tree.Statements
{
    using System;
    using System.Collections.Generic;
    using Debugging;
    using Expressions;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;
    using Script = NovaSharp.Interpreter.Script;

    internal class AssignmentStatement : Statement
    {
        private readonly List<IVariable> _lValues = new();
        private readonly List<Expression> _rValues;
        private readonly SourceRef _ref;

        public AssignmentStatement(ScriptLoadingContext lcontext, Token startToken)
            : base(lcontext)
        {
            List<(Token name, SymbolRefAttributes flags)> locals = new();

            Token first = startToken;

            while (true)
            {
                Token nameToken = CheckTokenType(lcontext, TokenType.Name);
                SymbolRefAttributes flags = ParseLocalAttributes(lcontext);
                locals.Add((nameToken, flags));

                if (lcontext.Lexer.Current.Type != TokenType.Comma)
                {
                    break;
                }

                lcontext.Lexer.Next();
            }

            if (lcontext.Lexer.Current.Type == TokenType.OpAssignment)
            {
                CheckTokenType(lcontext, TokenType.OpAssignment);
                _rValues = Expression.ExprList(lcontext);
            }
            else
            {
                _rValues = new List<Expression>();
            }

            foreach ((Token nameToken, SymbolRefAttributes flags) in locals)
            {
                SymbolRef localVar = lcontext.Scope.TryDefineLocal(nameToken.Text, flags);
                SymbolRefExpression symbol = new(lcontext, localVar);
                _lValues.Add(symbol);
            }

            Token last = lcontext.Lexer.Current;
            _ref = first.GetSourceRefUpTo(last);
            lcontext.Source.Refs.Add(_ref);
        }

        public AssignmentStatement(
            ScriptLoadingContext lcontext,
            Expression firstExpression,
            Token first
        )
            : base(lcontext)
        {
            _lValues.Add(CheckVar(lcontext, firstExpression));

            while (lcontext.Lexer.Current.Type == TokenType.Comma)
            {
                lcontext.Lexer.Next();
                Expression e = Expression.PrimaryExp(lcontext);
                _lValues.Add(CheckVar(lcontext, e));
            }

            CheckTokenType(lcontext, TokenType.OpAssignment);

            _rValues = Expression.ExprList(lcontext);

            Token last = lcontext.Lexer.Current;
            _ref = first.GetSourceRefUpTo(last);
            lcontext.Source.Refs.Add(_ref);
        }

        private static IVariable CheckVar(ScriptLoadingContext lcontext, Expression firstExpression)
        {
            if (firstExpression is not IVariable v)
            {
                throw new SyntaxErrorException(
                    lcontext.Lexer.Current,
                    "unexpected symbol near '{0}' - not a l-value",
                    lcontext.Lexer.Current
                );
            }

            return v;
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            using (bc.EnterSource(_ref))
            {
                foreach (Expression exp in _rValues)
                {
                    exp.Compile(bc);
                }

                for (int i = 0; i < _lValues.Count; i++)
                {
                    _lValues[i]
                        .CompileAssignment(
                            bc,
                            Math.Max(_rValues.Count - 1 - i, 0), // index of r-value
                            i - Math.Min(i, _rValues.Count - 1)
                        ); // index in last tuple
                }

                bc.EmitPop(_rValues.Count);
            }
        }

        private static SymbolRefAttributes ParseLocalAttributes(ScriptLoadingContext lcontext)
        {
            SymbolRefAttributes flags = default;
            Script script =
                lcontext.Script ?? throw new InvalidOperationException("Missing Script instance.");
            LuaCompatibilityProfile profile = script.CompatibilityProfile;

            while (lcontext.Lexer.Current.Type == TokenType.OpLessThan)
            {
                lcontext.Lexer.Next();

                Token attr = CheckTokenType(lcontext, TokenType.Name);
                SymbolRefAttributes attrFlag = attr.Text switch
                {
                    "const" => GetConstAttribute(attr, profile),
                    "close" => GetCloseAttribute(attr, profile),
                    _ => throw new SyntaxErrorException(attr, "unknown attribute '{0}'", attr.Text),
                };

                if ((flags & attrFlag) != 0)
                {
                    throw new SyntaxErrorException(attr, "duplicate attribute '{0}'", attr.Text);
                }

                CheckTokenType(lcontext, TokenType.OpGreaterThan);

                flags |= attrFlag;
            }

            return flags;
        }

        private static SymbolRefAttributes GetConstAttribute(
            Token attributeToken,
            LuaCompatibilityProfile profile
        )
        {
            EnsureAttributeSupported(
                attributeToken,
                profile.SupportsConstLocals,
                "Lua 5.4 manual §3.3.7"
            );
            return SymbolRefAttributes.Const;
        }

        private static SymbolRefAttributes GetCloseAttribute(
            Token attributeToken,
            LuaCompatibilityProfile profile
        )
        {
            EnsureAttributeSupported(
                attributeToken,
                profile.SupportsToBeClosedVariables,
                "Lua 5.4 manual §3.3.8"
            );
            return SymbolRefAttributes.ToBeClosed;
        }

        private static void EnsureAttributeSupported(
            Token attributeToken,
            bool isSupported,
            string manualSection
        )
        {
            if (isSupported)
            {
                return;
            }

            throw new SyntaxErrorException(
                attributeToken,
                "'{0}' attribute requires Lua 5.4+ compatibility ({1}). Set Script.Options.CompatibilityVersion accordingly.",
                attributeToken.Text,
                manualSection
            );
        }
    }
}
