namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;

    /// <summary>
    /// Class providing a simple descriptor for constant DynValues in userdata
    /// </summary>
    public class DynValueMemberDescriptor : IMemberDescriptor, IWireableDescriptor
    {
        private readonly DynValue _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynValueMemberDescriptor" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="serializedTableValue">A string containing a table whose first member is the dynvalue to be deserialized (convoluted...).</param>
        protected DynValueMemberDescriptor(string name, string serializedTableValue)
        {
            Script s = new();
            DynamicExpression exp = s.CreateDynamicExpression(serializedTableValue);
            DynValue val = exp.Evaluate(null);

            _value = val.Table.Get(1);
            Name = name;
            MemberAccess = MemberDescriptorAccess.CanRead;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynValueMemberDescriptor" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        protected DynValueMemberDescriptor(string name)
        {
            MemberAccess = MemberDescriptorAccess.CanRead;
            _value = null;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynValueMemberDescriptor"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public DynValueMemberDescriptor(string name, DynValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _value = value;
            Name = name;

            if (value.Type == DataType.ClrFunction)
            {
                MemberAccess = MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute;
            }
            else
            {
                MemberAccess = MemberDescriptorAccess.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the described member is static.
        /// </summary>
        public bool IsStatic
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the name of the member
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess { get; private set; }

        /// <summary>
        /// Gets the value wrapped by this descriptor
        /// </summary>
        [SuppressMessage(
            "Naming",
            "CA1721:The property name 'Value' is confusing given the existence of method 'GetValue'",
            Justification = "IMemberDescriptor exposes GetValue; this property predates the interface and remains for back-compat and hardwire generators."
        )]
        public virtual DynValue Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the value of this member as a <see cref="DynValue" /> to be exposed to scripts.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object owning this member, or null if static.</param>
        /// <returns>
        /// The value of this member as a <see cref="DynValue" />.
        /// </returns>
        public DynValue GetValue(Script script, object obj)
        {
            return Value;
        }

        /// <summary>
        /// Sets the value of this member from a <see cref="DynValue" />.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object owning this member, or null if static.</param>
        /// <param name="value">The value to be set.</param>
        /// <exception cref="ScriptRuntimeException">userdata '{0}' cannot be written to.</exception>
        public void SetValue(Script script, object obj, DynValue value)
        {
            throw new ScriptRuntimeException("userdata '{0}' cannot be written to.", Name);
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            t.Set("class", DynValue.NewString(GetType().FullName));
            t.Set("name", DynValue.NewString(Name));

            switch (Value.Type)
            {
                case DataType.Nil:
                case DataType.Void:
                case DataType.Boolean:
                case DataType.Number:
                case DataType.String:
                case DataType.Tuple:
                    t.Set("value", Value);
                    break;
                case DataType.Table:
                    if (Value.Table.OwnerScript == null)
                    {
                        t.Set("value", Value);
                    }
                    else
                    {
                        t.Set(
                            "error",
                            DynValue.NewString(
                                "Wiring of non-prime table value members not supported."
                            )
                        );
                    }

                    break;
                case DataType.UserData:
                    if (Value.UserData.Object == null)
                    {
                        t.Set("type", DynValue.NewString("userdata"));
                        t.Set(
                            "staticType",
                            DynValue.NewString(Value.UserData.Descriptor.Type.FullName)
                        );
                        t.Set(
                            "visibility",
                            DynValue.NewString(Value.UserData.Descriptor.Type.GetClrVisibility())
                        );
                    }
                    else
                    {
                        t.Set(
                            "error",
                            DynValue.NewString(
                                "Wiring of non-static userdata value members not supported."
                            )
                        );
                    }
                    break;
                default:
                    t.Set(
                        "error",
                        DynValue.NewString(
                            ZString.Concat(
                                "Wiring of '",
                                Value.Type.ToErrorTypeString(),
                                "' value members not supported."
                            )
                        )
                    );
                    break;
            }
        }
    }
}
