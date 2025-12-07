namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;

    /// <summary>
    /// Hardwired descriptor that dispatches to a generated method body without reflection.
    /// </summary>
    public abstract class HardwiredMethodMemberDescriptor : FunctionMemberDescriptorBase
    {
        /// <inheritdoc />
        public override DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        )
        {
            this.CheckAccess(MemberDescriptorAccess.CanExecute, obj);

            // Note: We don't use pooled arrays here because the pars array is passed to
            // user-defined Invoke methods which may store references to it. For backward
            // compatibility, we use the non-pooled version.
            object[] pars = base.BuildArgumentList(
                script,
                obj,
                context,
                args,
                out IList<int> outParams
            );
            object retv = Invoke(script, obj, pars, CalcArgsCount(pars));

            return DynValue.FromObject(script, retv);
        }

        /// <summary>
        /// Computes the number of arguments actually supplied (ignoring default-value placeholders).
        /// </summary>
        private int CalcArgsCount(object[] pars)
        {
            int count = pars.Length;

            for (int i = 0; i < pars.Length; i++)
            {
                if (Parameters[i].HasDefaultValue && (pars[i] is DefaultValue))
                {
                    count -= 1;
                }
            }

            return count;
        }

        /// <summary>
        /// Invokes the underlying method represented by this descriptor.
        /// </summary>
        protected abstract object Invoke(Script script, object obj, object[] pars, int argscount);
    }
}
