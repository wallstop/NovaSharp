namespace NovaSharp.Interpreter.Tree.Statements
{
    using System;
    using System.Collections.Generic;
    using Debugging;
    using Execution;
    using Expressions;
    using NovaSharp.Interpreter.DataTypes;

    internal class AssignmentStatement : Statement
    {
        private readonly List<IVariable> _lValues = new();
        private readonly List<Expression> _rValues;
        private readonly SourceRef _ref;

        public AssignmentStatement(ScriptLoadingContext lcontext, Token startToken)
            : base(lcontext)
        {
            List<string> names = new();

            Token first = startToken;

            while (true)
            {
                Token name = CheckTokenType(lcontext, TokenType.Name);
                names.Add(name.Text);

                if (lcontext.Lexer.Current.type != TokenType.Comma)
                {
                    break;
                }

                lcontext.Lexer.Next();
            }

            if (lcontext.Lexer.Current.type == TokenType.OpAssignment)
            {
                CheckTokenType(lcontext, TokenType.OpAssignment);
                _rValues = Expression.ExprList(lcontext);
            }
            else
            {
                _rValues = new List<Expression>();
            }

            foreach (string name in names)
            {
                SymbolRef localVar = lcontext.Scope.TryDefineLocal(name);
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

            while (lcontext.Lexer.Current.type == TokenType.Comma)
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

        private IVariable CheckVar(ScriptLoadingContext lcontext, Expression firstExpression)
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

                bc.Emit_Pop(_rValues.Count);
            }
        }
    }
}
