namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    ///
    /// </summary>
    internal class BinaryOperatorExpression : Expression
    {
        [Flags]
        internal enum Operator
        {
            NotAnOperator = 0,

            Or = 0x1,
            And = 0x2,
            Less = 0x4,
            Greater = 0x8,
            LessOrEqual = 0x10,

            GreaterOrEqual = 0x20,
            NotEqual = 0x40,
            Equal = 0x80,
            StrConcat = 0x100,
            Add = 0x200,
            Sub = 0x400,
            Mul = 0x1000,
            Div = 0x2000,
            Mod = 0x4000,
            Power = 0x8000,
        }

        private class Node
        {
            public Expression Expression;
            public Operator Operator;
            public Node Previous;
            public Node Next;
        }

        private class LinkedList
        {
            public Node Head;
            public Node Tail;
            public Operator OperatorMask;
        }

        private const Operator PowerOperator = Operator.Power;
        private const Operator MultiplicationDivisionModuloOperators =
            Operator.Mul | Operator.Div | Operator.Mod;
        private const Operator AdditionSubtractionOperators = Operator.Add | Operator.Sub;
        private const Operator StringConcatenationOperator = Operator.StrConcat;

        private const Operator ComparisonOperators =
            Operator.Less
            | Operator.Greater
            | Operator.GreaterOrEqual
            | Operator.LessOrEqual
            | Operator.Equal
            | Operator.NotEqual;

        private const Operator LogicalAndOperator = Operator.And;
        private const Operator LogicalOrOperator = Operator.Or;

        internal void SetOperatorForTests(Operator op)
        {
            _operator = op;
        }

        internal static void RemoveFirstExpressionForTests(object chain)
        {
            if (chain == null)
            {
                throw new ArgumentNullException(nameof(chain));
            }

            LinkedList list = (LinkedList)chain;
            if (list.Head != null)
            {
                list.Head.Expression = null;
            }
        }

        public static object BeginOperatorChain()
        {
            return new LinkedList();
        }

        public static void AddExpressionToChain(object chain, Expression exp)
        {
            LinkedList list = (LinkedList)chain;
            Node node = new() { Expression = exp };
            AddNode(list, node);
        }

        public static void AddOperatorToChain(object chain, Token op)
        {
            LinkedList list = (LinkedList)chain;
            Node node = new() { Operator = ParseBinaryOperator(op) };
            AddNode(list, node);
        }

        public static Expression CommitOperatorChain(object chain, ScriptLoadingContext lcontext)
        {
            return CreateSubTree((LinkedList)chain, lcontext);
        }

        public static Expression CreatePowerExpression(
            Expression op1,
            Expression op2,
            ScriptLoadingContext lcontext
        )
        {
            return new BinaryOperatorExpression(op1, op2, Operator.Power, lcontext);
        }

        private static void AddNode(LinkedList list, Node node)
        {
            list.OperatorMask |= node.Operator;

            if (list.Head == null)
            {
                list.Head = list.Tail = node;
            }
            else
            {
                list.Tail.Next = node;
                node.Previous = list.Tail;
                list.Tail = node;
            }
        }

        /// <summary>
        /// Creates a sub tree of binary expressions
        /// </summary>
        private static Expression CreateSubTree(LinkedList list, ScriptLoadingContext lcontext)
        {
            Operator opfound = list.OperatorMask;

            Node nodes = list.Head;

            if ((opfound & PowerOperator) != 0)
            {
                nodes = PrioritizeRightAssociative(nodes, lcontext, PowerOperator);
            }

            if ((opfound & MultiplicationDivisionModuloOperators) != 0)
            {
                nodes = PrioritizeLeftAssociative(
                    nodes,
                    lcontext,
                    MultiplicationDivisionModuloOperators
                );
            }

            if ((opfound & AdditionSubtractionOperators) != 0)
            {
                nodes = PrioritizeLeftAssociative(nodes, lcontext, AdditionSubtractionOperators);
            }

            if ((opfound & StringConcatenationOperator) != 0)
            {
                nodes = PrioritizeRightAssociative(nodes, lcontext, StringConcatenationOperator);
            }

            if ((opfound & ComparisonOperators) != 0)
            {
                nodes = PrioritizeLeftAssociative(nodes, lcontext, ComparisonOperators);
            }

            if ((opfound & LogicalAndOperator) != 0)
            {
                nodes = PrioritizeLeftAssociative(nodes, lcontext, LogicalAndOperator);
            }

            if ((opfound & LogicalOrOperator) != 0)
            {
                nodes = PrioritizeLeftAssociative(nodes, lcontext, LogicalOrOperator);
            }

            if (nodes.Next != null || nodes.Previous != null)
            {
                throw new InternalErrorException("Expression reduction didn't work! - 1");
            }

            if (nodes.Expression == null)
            {
                throw new InternalErrorException("Expression reduction didn't work! - 2");
            }

            return nodes.Expression;
        }

        private static Node PrioritizeLeftAssociative(
            Node nodes,
            ScriptLoadingContext lcontext,
            Operator operatorsToFind
        )
        {
            for (Node n = nodes; n != null; n = n.Next)
            {
                Operator o = n.Operator;

                if ((o & operatorsToFind) == 0)
                {
                    continue;
                }

                n.Operator = Operator.NotAnOperator;
                n.Expression = new BinaryOperatorExpression(
                    n.Previous.Expression,
                    n.Next.Expression,
                    o,
                    lcontext
                );
                n.Previous = n.Previous.Previous;
                n.Next = n.Next.Next;

                if (n.Next != null)
                {
                    n.Next.Previous = n;
                }

                if (n.Previous != null)
                {
                    n.Previous.Next = n;
                }
                else
                {
                    nodes = n;
                }
            }

            return nodes;
        }

        private static Node PrioritizeRightAssociative(
            Node nodes,
            ScriptLoadingContext lcontext,
            Operator operatorsToFind
        )
        {
            Node last;
            for (last = nodes; last.Next != null; last = last.Next) { }

            for (Node n = last; n != null; n = n.Previous)
            {
                Operator o = n.Operator;

                if ((o & operatorsToFind) == 0)
                {
                    continue;
                }

                n.Operator = Operator.NotAnOperator;
                n.Expression = new BinaryOperatorExpression(
                    n.Previous.Expression,
                    n.Next.Expression,
                    o,
                    lcontext
                );
                n.Previous = n.Previous.Previous;
                n.Next = n.Next.Next;

                if (n.Next != null)
                {
                    n.Next.Previous = n;
                }

                if (n.Previous != null)
                {
                    n.Previous.Next = n;
                }
                else
                {
                    nodes = n;
                }
            }

            return nodes;
        }

        private static Operator ParseBinaryOperator(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Or:
                    return Operator.Or;
                case TokenType.And:
                    return Operator.And;
                case TokenType.OpLessThan:
                    return Operator.Less;
                case TokenType.OpGreaterThan:
                    return Operator.Greater;
                case TokenType.OpLessThanEqual:
                    return Operator.LessOrEqual;
                case TokenType.OpGreaterThanEqual:
                    return Operator.GreaterOrEqual;
                case TokenType.OpNotEqual:
                    return Operator.NotEqual;
                case TokenType.OpEqual:
                    return Operator.Equal;
                case TokenType.OpConcat:
                    return Operator.StrConcat;
                case TokenType.OpAdd:
                    return Operator.Add;
                case TokenType.OpMinusOrSub:
                    return Operator.Sub;
                case TokenType.OpMul:
                    return Operator.Mul;
                case TokenType.OpDiv:
                    return Operator.Div;
                case TokenType.OpMod:
                    return Operator.Mod;
                case TokenType.OpPwr:
                    return Operator.Power;
                default:
                    throw new InternalErrorException(
                        "Unexpected binary operator '{0}'",
                        token.Text
                    );
            }
        }

        private readonly Expression _exp1;

        private readonly Expression _exp2;

        private Operator _operator;

        private BinaryOperatorExpression(
            Expression exp1,
            Expression exp2,
            Operator op,
            ScriptLoadingContext lcontext
        )
            : base(lcontext)
        {
            _exp1 = exp1;
            _exp2 = exp2;
            _operator = op;
        }

        private static bool ShouldInvertBoolean(Operator op)
        {
            return (op == Operator.NotEqual)
                || (op == Operator.GreaterOrEqual)
                || (op == Operator.Greater);
        }

        private static OpCode OperatorToOpCode(Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                case Operator.GreaterOrEqual:
                    return OpCode.Less;
                case Operator.LessOrEqual:
                case Operator.Greater:
                    return OpCode.LessEq;
                case Operator.Equal:
                case Operator.NotEqual:
                    return OpCode.Eq;
                case Operator.StrConcat:
                    return OpCode.Concat;
                case Operator.Add:
                    return OpCode.Add;
                case Operator.Sub:
                    return OpCode.Sub;
                case Operator.Mul:
                    return OpCode.Mul;
                case Operator.Div:
                    return OpCode.Div;
                case Operator.Mod:
                    return OpCode.Mod;
                case Operator.Power:
                    return OpCode.Power;
                default:
                    throw new InternalErrorException("Unsupported operator {0}", op);
            }
        }

        public override void Compile(ByteCode bc)
        {
            _exp1.Compile(bc);

            if (_operator == Operator.Or)
            {
                Instruction i = bc.EmitJump(OpCode.JtOrPop, -1);
                _exp2.Compile(bc);
                i.NumVal = bc.GetJumpPointForNextInstruction();
                return;
            }

            if (_operator == Operator.And)
            {
                Instruction i = bc.EmitJump(OpCode.JfOrPop, -1);
                _exp2.Compile(bc);
                i.NumVal = bc.GetJumpPointForNextInstruction();
                return;
            }

            if (_exp2 != null)
            {
                _exp2.Compile(bc);
            }

            bc.EmitOperator(OperatorToOpCode(_operator));

            if (ShouldInvertBoolean(_operator))
            {
                bc.EmitOperator(OpCode.Not);
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            DynValue v1 = _exp1.Eval(context).ToScalar();

            if (_operator == Operator.Or)
            {
                if (v1.CastToBool())
                {
                    return v1;
                }
                else
                {
                    return _exp2.Eval(context).ToScalar();
                }
            }

            if (_operator == Operator.And)
            {
                if (!v1.CastToBool())
                {
                    return v1;
                }
                else
                {
                    return _exp2.Eval(context).ToScalar();
                }
            }

            DynValue v2 = _exp2.Eval(context).ToScalar();

            if ((_operator & ComparisonOperators) != 0)
            {
                return DynValue.NewBoolean(EvalComparison(v1, v2, _operator));
            }
            else if (_operator == Operator.StrConcat)
            {
                string s1 = v1.CastToString();
                string s2 = v2.CastToString();

                if (s1 == null || s2 == null)
                {
                    throw new DynamicExpressionException(
                        "Attempt to perform concatenation on non-strings."
                    );
                }

                return DynValue.NewString(s1 + s2);
            }
            else
            {
                return DynValue.NewNumber(EvalArithmetic(v1, v2));
            }
        }

        private double EvalArithmetic(DynValue v1, DynValue v2)
        {
            double? nd1 = v1.CastToNumber();
            double? nd2 = v2.CastToNumber();

            if (nd1 == null || nd2 == null)
            {
                throw new DynamicExpressionException(
                    "Attempt to perform arithmetic on non-numbers."
                );
            }

            double d1 = nd1.Value;
            double d2 = nd2.Value;

            switch (_operator)
            {
                case Operator.Add:
                    return d1 + d2;
                case Operator.Sub:
                    return d1 - d2;
                case Operator.Mul:
                    return d1 * d2;
                case Operator.Div:
                    return d1 / d2;
                case Operator.Mod:
                {
                    double mod = Math.IEEERemainder(d1, d2);
                    if (mod < 0)
                    {
                        mod += d2;
                    }

                    return mod;
                }
                case Operator.Power:
                    return Math.Pow(d1, d2);
                default:
                    throw new DynamicExpressionException("Unsupported operator {0}", _operator);
            }
        }

        private bool EvalComparison(DynValue l, DynValue r, Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                    if (l.Type == DataType.Number && r.Type == DataType.Number)
                    {
                        return (l.Number < r.Number);
                    }
                    else if (l.Type == DataType.String && r.Type == DataType.String)
                    {
                        return (l.String.CompareTo(r.String) < 0);
                    }
                    else
                    {
                        throw new DynamicExpressionException(
                            "Attempt to compare non-numbers, non-strings."
                        );
                    }
                case Operator.LessOrEqual:
                    if (l.Type == DataType.Number && r.Type == DataType.Number)
                    {
                        return (l.Number <= r.Number);
                    }
                    else if (l.Type == DataType.String && r.Type == DataType.String)
                    {
                        return (l.String.CompareTo(r.String) <= 0);
                    }
                    else
                    {
                        throw new DynamicExpressionException(
                            "Attempt to compare non-numbers, non-strings."
                        );
                    }
                case Operator.Equal:
                    if (ReferenceEquals(r, l))
                    {
                        return true;
                    }
                    else if (r.Type != l.Type)
                    {
                        if (
                            (l.Type == DataType.Nil && r.Type == DataType.Void)
                            || (l.Type == DataType.Void && r.Type == DataType.Nil)
                        )
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return r.Equals(l);
                    }
                case Operator.Greater:
                    return !EvalComparison(l, r, Operator.LessOrEqual);
                case Operator.GreaterOrEqual:
                    return !EvalComparison(l, r, Operator.Less);
                case Operator.NotEqual:
                    return !EvalComparison(l, r, Operator.Equal);
                default:
                    throw new DynamicExpressionException("Unsupported operator {0}", op);
            }
        }
    }
}
