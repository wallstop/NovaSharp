namespace WallstopStudios.NovaSharp.Interpreter
{
    using Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// Class containing script global options, that is options which cannot be customized per-script.
    /// <see cref="Script.GlobalOptions"/>
    /// </summary>
    public class ScriptGlobalOptions
    {
        internal ScriptGlobalOptions()
        {
            // Initialize ZString enum formatters for zero-allocation enum appending
            ZStringEnumFormatters.Initialize();

            Platform = PlatformAutoDetector.GetDefaultPlatform();
            CustomConverters = new CustomConverterRegistry();
            FuzzySymbolMatching =
                FuzzySymbolMatchingBehavior.Camelify
                | FuzzySymbolMatchingBehavior.UpperFirstLetter
                | FuzzySymbolMatchingBehavior.PascalCase;
            CompatibilityVersion = LuaCompatibilityVersion.Latest;
        }

        /// <summary>
        /// Gets or sets the custom converters.
        /// </summary>
        public CustomConverterRegistry CustomConverters { get; set; }

        /// <summary>
        /// Gets or sets the platform abstraction to use.
        /// </summary>
        /// <value>
        /// The current platform abstraction.
        /// </value>
        public IPlatformAccessor Platform { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether interpreter exceptions should be
        /// re-thrown as nested exceptions.
        /// </summary>
        public bool RethrowExceptionNested { get; set; }

        /// <summary>
        /// Gets or sets an enum that controls behaviour when a symbol (method, property, userdata) is not found in a userdata's descriptor. For instance,
        /// when this value is <see cref="FuzzySymbolMatchingBehavior.UpperFirstLetter"/> and Lua code calls the non-existent method <c>someuserdata.someMethod()</c>,
        /// <c>someuserdata.SomeMethod()</c> will also be tried.
        /// </summary>
        public FuzzySymbolMatchingBehavior FuzzySymbolMatching { get; set; }

        /// <summary>
        /// Gets or sets the interpreter-wide compatibility target. Defaults to the latest supported version.
        /// </summary>
        public LuaCompatibilityVersion CompatibilityVersion { get; set; }

        /// <summary>
        /// Creates a copy of the current global options so callers can apply changes without affecting the original instance.
        /// </summary>
        /// <returns>A new <see cref="ScriptGlobalOptions"/> with the same option values.</returns>
        internal ScriptGlobalOptions Clone()
        {
            ScriptGlobalOptions clone = new()
            {
                Platform = Platform,
                CustomConverters = CustomConverters?.Clone() ?? new CustomConverterRegistry(),
                RethrowExceptionNested = RethrowExceptionNested,
                FuzzySymbolMatching = FuzzySymbolMatching,
                CompatibilityVersion = CompatibilityVersion,
            };

            return clone;
        }
    }
}
