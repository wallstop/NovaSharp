namespace NovaSharp.Interpreter.Interop.BasicDescriptors
{
    using System;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Specialized <see cref="IMemberDescriptor"/> for members supporting overloads resolution.
    /// </summary>
    public interface IOverloadableMemberDescriptor : IMemberDescriptor
    {
        /// <summary>
        /// Invokes the member from script.
        /// Implementors should raise exceptions if the value cannot be executed or if access to an
        /// instance member through a static userdata is attempted.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        );

        /// <summary>
        /// Gets the type which this extension method extends, null if this is not an extension method.
        /// </summary>
        public Type ExtensionMethodType { get; }

        /// <summary>
        /// Gets the type of the arguments of the underlying CLR function
        /// </summary>
        public ParameterDescriptor[] Parameters { get; }

        /// <summary>
        /// Gets a value indicating the type of the ParamArray parameter of a var-args function. If the function is not var-args,
        /// null is returned.
        /// </summary>
        public Type VarArgsArrayType { get; }

        /// <summary>
        /// Gets a value indicating the type of the elements of the ParamArray parameter of a var-args function. If the function is not var-args,
        /// null is returned.
        /// </summary>
        public Type VarArgsElementType { get; }

        /// <summary>
        /// Gets a sort discriminant to give consistent overload resolution matching in case of perfectly equal scores
        /// </summary>
        public string SortDiscriminant { get; }
    }
}
