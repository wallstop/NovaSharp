namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Flags describing how a symbol reference should behave (constness, to-be-closed variables, etc.).
    /// </summary>
    [Flags]
    public enum SymbolRefAttributes
    {
        /// <summary>
        /// Default behaviour (mutable, not to-be-closed).
        /// </summary>
        [Obsolete("Prefer specifying explicit SymbolRefAttributes flags.", false)]
        None = 0,

        /// <summary>
        /// Symbol represents a Lua &lt;const&gt; local.
        /// </summary>
        Const = 1 << 0,

        /// <summary>
        /// Symbol must be closed when leaving scope (`<close>`).
        /// </summary>
        ToBeClosed = 1 << 1,
    }

    /// <summary>
    /// This class stores a possible l-value (that is a potential target of an assignment)
    /// </summary>
    public class SymbolRef
    {
        private static readonly SymbolRef DefaultEnvSymbol = new()
        {
            _symbolType = SymbolRefType.DefaultEnv,
        };

        // Fields are internal - direct access by the executor was a 10% improvement at profiling here!
        internal SymbolRefType _symbolType;
        internal SymbolRef _environmentRef;
        internal int _indexValue;
        internal string _nameValue;
        internal SymbolRefAttributes _symbolAttributes;

        internal SymbolRefType SymbolType
        {
            get => _symbolType;
            set => _symbolType = value;
        }

        internal SymbolRef EnvironmentRef
        {
            get => _environmentRef;
            set => _environmentRef = value;
        }

        internal int IndexValue
        {
            get => _indexValue;
            set => _indexValue = value;
        }

        internal string NameValue
        {
            get => _nameValue;
            set => _nameValue = value;
        }

        internal SymbolRefAttributes SymbolAttributes
        {
            get => _symbolAttributes;
            set => _symbolAttributes = value;
        }

        /// <summary>
        /// Gets the type of this symbol reference
        /// </summary>
        public SymbolRefType Type
        {
            get { return _symbolType; }
        }

        /// <summary>
        /// Gets the index of this symbol in its scope context
        /// </summary>
        public int Index
        {
            get { return _indexValue; }
        }

        /// <summary>
        /// Gets the name of this symbol
        /// </summary>
        public string Name
        {
            get { return _nameValue; }
        }

        /// <summary>
        /// Gets the full attribute set applied to the symbol.
        /// </summary>
        public SymbolRefAttributes Attributes
        {
            get { return _symbolAttributes; }
        }

        /// <summary>
        /// Gets a value indicating whether the symbol is immutable.
        /// </summary>
        public bool IsConst => (_symbolAttributes & SymbolRefAttributes.Const) != 0;

        /// <summary>
        /// Gets a value indicating whether the symbol participates in the to-be-closed cleanup flow.
        /// </summary>
        public bool IsToBeClosed => (_symbolAttributes & SymbolRefAttributes.ToBeClosed) != 0;

        /// <summary>
        /// Gets the environment this symbol refers to (for global symbols only)
        /// </summary>
        public SymbolRef Environment
        {
            get { return _environmentRef; }
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
                _indexValue = -1,
                _symbolType = SymbolRefType.Global,
                _environmentRef = envSymbol,
                _nameValue = name,
                _symbolAttributes = default,
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
                _indexValue = index,
                _symbolType = SymbolRefType.Local,
                _nameValue = name,
                _symbolAttributes = attributes,
            };
        }

        /// <summary>
        /// Creates a new symbol reference pointing to an upvalue var
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="index">The index of the var in closure scope.</param>
        /// <returns></returns>
        internal static SymbolRef UpValue(string name, int index)
        {
            //Debug.Assert(index >= 0, "Symbol Index < 0");
            return new SymbolRef()
            {
                _indexValue = index,
                _symbolType = SymbolRefType.UpValue,
                _nameValue = name,
                _symbolAttributes = default,
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
            if (_symbolType == SymbolRefType.DefaultEnv)
            {
                return "(default _ENV)";
            }

            if (_symbolType == SymbolRefType.Global)
            {
                return FormattableString.Invariant(
                    $"{_nameValue} : {_symbolType} / {_environmentRef}"
                );
            }

            return FormattableString.Invariant(
                $"{_nameValue} : {_symbolType}[{_indexValue.ToString(CultureInfo.InvariantCulture)}]"
            );
        }

        /// <summary>
        /// Writes this instance to a binary stream
        /// </summary>
        internal void WriteBinary(BinaryWriter bw)
        {
            bw.Write((byte)_symbolType);
            bw.Write(_indexValue);
            bw.Write(_nameValue);
            bw.Write((int)_symbolAttributes);
        }

        /// <summary>
        /// Reads a symbolref from a binary stream
        /// </summary>
        internal static SymbolRef ReadBinary(BinaryReader br)
        {
            SymbolRef that = new()
            {
                _symbolType = (SymbolRefType)br.ReadByte(),
                _indexValue = br.ReadInt32(),
                _nameValue = br.ReadString(),
                _symbolAttributes = (SymbolRefAttributes)br.ReadInt32(),
            };
            return that;
        }

        /// <summary>
        /// Serializes the environment reference index into the binary writer.
        /// </summary>
        internal void WriteBinaryEnv(BinaryWriter bw, Dictionary<SymbolRef, int> symbolMap)
        {
            if (_environmentRef != null)
            {
                bw.Write(symbolMap[_environmentRef]);
            }
            else
            {
                bw.Write(-1);
            }
        }

        /// <summary>
        /// Reads the environment reference index from the binary stream and resolves it against the symbol array.
        /// </summary>
        internal void ReadBinaryEnv(BinaryReader br, SymbolRef[] symbolRefs)
        {
            int idx = br.ReadInt32();

            if (idx >= 0)
            {
                _environmentRef = symbolRefs[idx];
            }
        }
    }
}
