namespace NovaSharp.Hardwire.Utils
{
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

    /// <summary>
    /// Captures parameter metadata emitted by the hardwire dump and exposes CodeDOM helpers.
    /// </summary>
    public class HardwireParameterDescriptor
    {
        /// <summary>
        /// Gets the CodeDOM expression that constructs a <see cref="ParameterDescriptor"/>.
        /// </summary>
        public CodeExpression Expression { get; private set; }

        /// <summary>
        /// Gets the fully qualified parameter type name.
        /// </summary>
        public string ParamType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the parameter has a default value.
        /// </summary>
        public bool HasDefaultValue { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the parameter is an out parameter.
        /// </summary>
        public bool IsOut { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the parameter is by-reference.
        /// </summary>
        public bool IsRef { get; private set; }

        /// <summary>
        /// Gets the temporary variable name assigned when emitting ref/out handling.
        /// </summary>
        public string TempVarName { get; private set; }

        public HardwireParameterDescriptor(Table tpar)
        {
            CodeExpression ename = new CodePrimitiveExpression(tpar.Get("name").String);
            CodeExpression etype = new CodeTypeOfExpression(tpar.Get("origtype").String);
            CodeExpression hasDefaultValue = new CodePrimitiveExpression(
                tpar.Get("default").Boolean
            );
            CodeExpression defaultValue = tpar.Get("default").Boolean
                ? (CodeExpression)(new CodeObjectCreateExpression(typeof(DefaultValue)))
                : (CodeExpression)(new CodePrimitiveExpression(null));
            CodeExpression isOut = new CodePrimitiveExpression(tpar.Get("out").Boolean);
            CodeExpression isRef = new CodePrimitiveExpression(tpar.Get("ref").Boolean);
            CodeExpression isVarArg = new CodePrimitiveExpression(tpar.Get("varargs").Boolean);
            CodeExpression restrictType = tpar.Get("restricted").Boolean
                ? (CodeExpression)(new CodeTypeOfExpression(tpar.Get("type").String))
                : (CodeExpression)(new CodePrimitiveExpression(null));

            Expression = new CodeObjectCreateExpression(
                typeof(ParameterDescriptor),
                new CodeExpression[]
                {
                    ename,
                    etype,
                    hasDefaultValue,
                    defaultValue,
                    isOut,
                    isRef,
                    isVarArg,
                }
            );

            ParamType = tpar.Get("origtype").String;
            HasDefaultValue = tpar.Get("default").Boolean;
            IsOut = tpar.Get("out").Boolean;
            IsRef = tpar.Get("ref").Boolean;
        }

        /// <summary>
        /// Creates descriptors for every entry in the given dump table.
        /// </summary>
        public static List<HardwireParameterDescriptor> LoadDescriptorsFromTable(Table t)
        {
            List<HardwireParameterDescriptor> list = new();

            for (int i = 1; i <= t.Length; i++)
            {
                list.Add(new HardwireParameterDescriptor(t.Get(i).Table));
            }

            return list;
        }

        /// <summary>
        /// Records the temporary variable name used to materialize ref/out parameters.
        /// </summary>
        public void SetTempVar(string varName)
        {
            if (!IsOut && !IsRef)
            {
                throw new InvalidOperationException("ReplaceExprWithVar on byval param");
            }

            TempVarName = varName;
        }
    }
}
