namespace NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    [Flags]
    public enum SymbolRefAttributes
    {
        [Obsolete("Prefer specifying explicit SymbolRefAttributes flags.", false)]
        None = 0,
        Const = 1 << 0,
        ToBeClosed = 1 << 1,
    }

    /// <summary>
    /// This class stores a possible l-value (that is a potential target of an assignment)
    /// </summary>
    public class SymbolRef
    {
        private static readonly SymbolRef DefaultEnvSymbol = new()
        {
            SymbolType = SymbolRefType.DefaultEnv,
        };

        // Fields are internal - direct access by the executor was a 10% improvement at profiling here!
        internal SymbolRefType SymbolType;
        internal SymbolRef EnvironmentRef;
        internal int IndexValue;
        internal string NameValue;
        internal SymbolRefAttributes SymbolAttributes;

        /// <summary>
        /// Gets the type of this symbol reference
        /// </summary>
        public SymbolRefType Type
        {
            get { return SymbolType; }
        }

        /// <summary>
        /// Gets the index of this symbol in its scope context
        /// </summary>
        public int Index
        {
            get { return IndexValue; }
        }

        /// <summary>
        /// Gets the name of this symbol
        /// </summary>
        public string Name
        {
            get { return NameValue; }
        }

        public SymbolRefAttributes Attributes
        {
            get { return SymbolAttributes; }
        }

        public bool IsConst => (SymbolAttributes & SymbolRefAttributes.Const) != 0;

        public bool IsToBeClosed => (SymbolAttributes & SymbolRefAttributes.ToBeClosed) != 0;

        /// <summary>
        /// Gets the environment this symbol refers to (for global symbols only)
        /// </summary>
        public SymbolRef Environment
        {
            get { return EnvironmentRef; }
        }

        /// <summary>
        /// Gets the default _ENV.
        /// </summary>
        public static SymbolRef DefaultEnv
        {
            get { return DefaultEnvSymbol; }
        }

        /// <summary>
        /// Creates a new symbol reference pointing to a global var
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="envSymbol">The _ENV symbol.</param>
        /// <returns></returns>
        public static SymbolRef Global(string name, SymbolRef envSymbol)
        {
            return new SymbolRef()
            {
                IndexValue = -1,
                SymbolType = SymbolRefType.Global,
                EnvironmentRef = envSymbol,
                NameValue = name,
                SymbolAttributes = default,
            };
        }

        /// <summary>
        /// Creates a new symbol reference pointing to a local var
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="index">The index of the var in local scope.</param>
        /// <returns></returns>
        internal static SymbolRef Local(
            string name,
            int index,
            SymbolRefAttributes attributes = default
        )
        {
            //Debug.Assert(index >= 0, "Symbol Index < 0");
            return new SymbolRef()
            {
                IndexValue = index,
                SymbolType = SymbolRefType.Local,
                NameValue = name,
                SymbolAttributes = attributes,
            };
        }

        /// <summary>
        /// Creates a new symbol reference pointing to an upvalue var
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="index">The index of the var in closure scope.</param>
        /// <returns></returns>
        internal static SymbolRef Upvalue(string name, int index)
        {
            //Debug.Assert(index >= 0, "Symbol Index < 0");
            return new SymbolRef()
            {
                IndexValue = index,
                SymbolType = SymbolRefType.Upvalue,
                NameValue = name,
                SymbolAttributes = default,
            };
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (SymbolType == SymbolRefType.DefaultEnv)
            {
                return "(default _ENV)";
            }

            if (SymbolType == SymbolRefType.Global)
            {
                return FormattableString.Invariant(
                    $"{NameValue} : {SymbolType} / {EnvironmentRef}"
                );
            }

            return FormattableString.Invariant(
                $"{NameValue} : {SymbolType}[{IndexValue.ToString(CultureInfo.InvariantCulture)}]"
            );
        }

        /// <summary>
        /// Writes this instance to a binary stream
        /// </summary>
        internal void WriteBinary(BinaryWriter bw)
        {
            bw.Write((byte)SymbolType);
            bw.Write(IndexValue);
            bw.Write(NameValue);
            bw.Write((int)SymbolAttributes);
        }

        /// <summary>
        /// Reads a symbolref from a binary stream
        /// </summary>
        internal static SymbolRef ReadBinary(BinaryReader br)
        {
            SymbolRef that = new()
            {
                SymbolType = (SymbolRefType)br.ReadByte(),
                IndexValue = br.ReadInt32(),
                NameValue = br.ReadString(),
                SymbolAttributes = (SymbolRefAttributes)br.ReadInt32(),
            };
            return that;
        }

        internal void WriteBinaryEnv(BinaryWriter bw, Dictionary<SymbolRef, int> symbolMap)
        {
            if (EnvironmentRef != null)
            {
                bw.Write(symbolMap[EnvironmentRef]);
            }
            else
            {
                bw.Write(-1);
            }
        }

        internal void ReadBinaryEnv(BinaryReader br, SymbolRef[] symbolRefs)
        {
            int idx = br.ReadInt32();

            if (idx >= 0)
            {
                EnvironmentRef = symbolRefs[idx];
            }
        }
    }
}
