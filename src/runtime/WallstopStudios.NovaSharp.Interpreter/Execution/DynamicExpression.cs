namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Expressions;

    /// <summary>
    /// Represents a dynamic expression in the script
    /// </summary>
    public class DynamicExpression : IScriptPrivateResource
    {
        private readonly DynamicExprExpression _exp;
        private readonly DynValue _constant;

        /// <summary>
        /// The code which generated this expression
        /// </summary>
        public string ExpressionCode { get; }

        internal DynamicExpression(Script s, string strExpr, DynamicExprExpression expr)
        {
            ExpressionCode = strExpr;
            OwnerScript = s;
            _exp = expr;
        }

        internal DynamicExpression(Script s, string strExpr, DynValue constant)
        {
            ExpressionCode = strExpr;
            OwnerScript = s;
            _constant = constant;
        }

        /// <summary>
        /// Evaluates the expression
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public DynValue Evaluate(ScriptExecutionContext context = null)
        {
            context = context ?? OwnerScript.CreateDynamicExecutionContext();

            this.CheckScriptOwnership(context.Script);

            if (_constant != null)
            {
                return _constant;
            }

            return _exp.Eval(context);
        }

        /// <summary>
        /// Finds a symbol in the expression
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public SymbolRef FindSymbol(ScriptExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.CheckScriptOwnership(context.Script);

            if (_exp != null)
            {
                return _exp.FindDynamic(context);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        /// <value>
        /// The script owning this resource.
        /// </value>
        public Script OwnerScript { get; private set; }

        /// <summary>
        /// Determines whether this instance is a constant expression
        /// </summary>
        /// <returns></returns>
        public bool IsConstant()
        {
            return _constant != null;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return DataStructs.HashCodeHelper.HashCode(ExpressionCode);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is not DynamicExpression o)
            {
                return false;
            }

            return o.ExpressionCode == ExpressionCode;
        }
    }
}
