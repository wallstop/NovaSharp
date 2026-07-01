namespace WallstopStudios.NovaSharp.Interpreter.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;

    /// <summary>
    /// Class managing modules (mostly as extension methods)
    /// </summary>
    public static class ModuleRegister
    {
        private static readonly ConditionalWeakTable<
            Type,
            ModuleRegistrationDescriptor
        > ModuleRegistrationDescriptors = new();

        private enum ModuleRegistrationActionKind
        {
            Callback,
            Init,
        }

        private sealed class ModuleRegistrationDescriptor
        {
            /// <summary>
            /// Creates immutable registration metadata for a module type.
            /// </summary>
            /// <param name="moduleNamespace">The Lua namespace table name, or null for globals.</param>
            /// <param name="methodActions">Ordered method and init actions from the module type.</param>
            /// <param name="scriptFields">Script-backed function fields from the module type.</param>
            /// <param name="constants">Constant fields from the module type.</param>
            public ModuleRegistrationDescriptor(
                string moduleNamespace,
                ModuleMethodRegistration[] methodActions,
                ModuleFieldRegistration[] scriptFields,
                ModuleConstantRegistration[] constants
            )
            {
                ModuleNamespace = moduleNamespace;
                MethodActions = methodActions;
                ScriptFields = scriptFields;
                Constants = constants;
            }

            /// <summary>
            /// Gets the Lua namespace table name, or null for globals.
            /// </summary>
            public string ModuleNamespace { get; }

            /// <summary>
            /// Gets ordered method callback and init actions.
            /// </summary>
            public ModuleMethodRegistration[] MethodActions { get; }

            /// <summary>
            /// Gets script-backed function fields.
            /// </summary>
            public ModuleFieldRegistration[] ScriptFields { get; }

            /// <summary>
            /// Gets constant fields.
            /// </summary>
            public ModuleConstantRegistration[] Constants { get; }
        }

        private sealed class ModuleMethodRegistration
        {
            /// <summary>
            /// Creates immutable metadata for a method callback or init action.
            /// </summary>
            /// <param name="kind">The registration action kind.</param>
            /// <param name="method">The reflected method.</param>
            /// <param name="compatibility">Optional Lua compatibility gate.</param>
            /// <param name="names">Precomputed Lua aliases for callback methods.</param>
            /// <param name="legacyCallback">Cached legacy callback delegate, when applicable.</param>
            /// <param name="argumentViewCallback">Cached argument-view callback delegate, when applicable.</param>
            /// <param name="argumentViewNoContextCallback">Cached contextless argument-view callback delegate, when applicable.</param>
            /// <param name="init">Cached module init delegate, when applicable.</param>
            public ModuleMethodRegistration(
                ModuleRegistrationActionKind kind,
                MethodInfo method,
                LuaCompatibilityAttribute compatibility,
                string[] names,
                Func<ScriptExecutionContext, CallbackArguments, DynValue> legacyCallback,
                ScriptFunctionCallbackView argumentViewCallback,
                ScriptFunctionCallbackViewNoContext argumentViewNoContextCallback,
                Action<Table, Table> init
            )
            {
                Kind = kind;
                Method = method;
                Compatibility = compatibility;
                Names = names;
                LegacyCallback = legacyCallback;
                ArgumentViewCallback = argumentViewCallback;
                ArgumentViewNoContextCallback = argumentViewNoContextCallback;
                Init = init;
            }

            /// <summary>
            /// Gets the registration action kind.
            /// </summary>
            public ModuleRegistrationActionKind Kind { get; }

            /// <summary>
            /// Gets the reflected method.
            /// </summary>
            public MethodInfo Method { get; }

            /// <summary>
            /// Gets the optional Lua compatibility gate.
            /// </summary>
            public LuaCompatibilityAttribute Compatibility { get; }

            /// <summary>
            /// Gets precomputed Lua aliases for callback methods.
            /// </summary>
            public string[] Names { get; }

            /// <summary>
            /// Gets the cached legacy callback delegate, when applicable.
            /// </summary>
            public Func<ScriptExecutionContext, CallbackArguments, DynValue> LegacyCallback { get; }

            /// <summary>
            /// Gets the cached argument-view callback delegate, when applicable.
            /// </summary>
            public ScriptFunctionCallbackView ArgumentViewCallback { get; }

            /// <summary>
            /// Gets the cached contextless argument-view callback delegate, when applicable.
            /// </summary>
            public ScriptFunctionCallbackViewNoContext ArgumentViewNoContextCallback { get; }

            /// <summary>
            /// Gets the cached module init delegate, when applicable.
            /// </summary>
            public Action<Table, Table> Init { get; }
        }

        private sealed class ModuleFieldRegistration
        {
            /// <summary>
            /// Creates immutable metadata for a script-backed function field.
            /// </summary>
            /// <param name="field">The reflected field.</param>
            /// <param name="compatibility">Optional Lua compatibility gate.</param>
            /// <param name="memberName">The CLR field name.</param>
            /// <param name="primaryName">The debugger-facing primary Lua name.</param>
            /// <param name="names">Precomputed Lua aliases.</param>
            public ModuleFieldRegistration(
                FieldInfo field,
                LuaCompatibilityAttribute compatibility,
                string memberName,
                string primaryName,
                string[] names
            )
            {
                Field = field;
                Compatibility = compatibility;
                MemberName = memberName;
                PrimaryName = primaryName;
                Names = names;
            }

            /// <summary>
            /// Gets the reflected field.
            /// </summary>
            public FieldInfo Field { get; }

            /// <summary>
            /// Gets the optional Lua compatibility gate.
            /// </summary>
            public LuaCompatibilityAttribute Compatibility { get; }

            /// <summary>
            /// Gets the CLR field name.
            /// </summary>
            public string MemberName { get; }

            /// <summary>
            /// Gets the debugger-facing primary Lua name.
            /// </summary>
            public string PrimaryName { get; }

            /// <summary>
            /// Gets precomputed Lua aliases.
            /// </summary>
            public string[] Names { get; }
        }

        private sealed class ModuleConstantRegistration
        {
            /// <summary>
            /// Creates immutable metadata for a module constant field.
            /// </summary>
            /// <param name="field">The reflected field.</param>
            /// <param name="compatibility">Optional Lua compatibility gate.</param>
            /// <param name="name">The primary Lua constant name.</param>
            /// <param name="names">Precomputed Lua aliases.</param>
            public ModuleConstantRegistration(
                FieldInfo field,
                LuaCompatibilityAttribute compatibility,
                string name,
                string[] names
            )
            {
                Field = field;
                Compatibility = compatibility;
                Name = name;
                Names = names;
            }

            /// <summary>
            /// Gets the reflected field.
            /// </summary>
            public FieldInfo Field { get; }

            /// <summary>
            /// Gets the optional Lua compatibility gate.
            /// </summary>
            public LuaCompatibilityAttribute Compatibility { get; }

            /// <summary>
            /// Gets the primary Lua constant name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets precomputed Lua aliases.
            /// </summary>
            public string[] Names { get; }
        }

        /// <summary>
        /// Register the core modules to a table
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="modules">The modules.</param>
        /// <returns></returns>
        public static Table RegisterCoreModules(this Table table, CoreModules modules)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            modules = Script.GlobalOptions.Platform.FilterSupportedCoreModules(modules);
            LuaCompatibilityProfile profile = GetCompatibilityProfile(table.OwnerScript);

            if (modules.Has(CoreModules.GlobalConsts))
            {
                RegisterConstants(table);
            }

            if (modules.Has(CoreModules.TableIterators))
            {
                RegisterModuleType(table, typeof(TableIteratorsModule));
            }

            if (modules.Has(CoreModules.Basic))
            {
                RegisterModuleType(table, typeof(BasicModule));

                if (!profile.SupportsWarnFunction)
                {
                    RemoveGlobalFunction(table, "warn");
                }
            }

            if (modules.Has(CoreModules.Metatables))
            {
                RegisterModuleType(table, typeof(MetaTableModule));
            }

            if (modules.Has(CoreModules.StringLib))
            {
                RegisterModuleType(table, typeof(StringModule));
                RegisterModuleType(table, typeof(CoreLib.StringLib.StringPackModule));

                if (profile.SupportsUtf8Library)
                {
                    RegisterModuleType(table, typeof(Utf8Module));
                }
            }

            if (modules.Has(CoreModules.LoadMethods))
            {
                RegisterModuleType(table, typeof(LoadModule));
            }

            if (modules.Has(CoreModules.Table))
            {
                RegisterModuleType(table, typeof(TableModule));

                if (!profile.SupportsTableMove)
                {
                    RemoveTableFunction(table, "move");
                }
            }

            if (modules.Has(CoreModules.Table))
            {
                RegisterModuleType(table, typeof(TableModuleGlobals));
            }

            if (modules.Has(CoreModules.ErrorHandling))
            {
                RegisterModuleType(table, typeof(ErrorHandlingModule));
            }

            if (modules.Has(CoreModules.Math))
            {
                RegisterModuleType(table, typeof(MathModule));
            }

            if (modules.Has(CoreModules.Coroutine))
            {
                RegisterModuleType(table, typeof(CoroutineModule));
            }

            if (modules.Has(CoreModules.Bit32) && profile.SupportsBit32Library)
            {
                RegisterModuleType(table, typeof(Bit32Module));
            }

            if (modules.Has(CoreModules.Dynamic))
            {
                RegisterModuleType(table, typeof(DynamicModule));
            }

            if (modules.Has(CoreModules.OsSystem))
            {
                RegisterModuleType(table, typeof(OsSystemModule));
            }

            if (modules.Has(CoreModules.OsTime))
            {
                RegisterModuleType(table, typeof(OsTimeModule));
            }

            if (modules.Has(CoreModules.Io))
            {
                RegisterModuleType(table, typeof(IoModule));
            }

            if (modules.Has(CoreModules.Debug))
            {
                RegisterModuleType(table, typeof(DebugModule));
            }

            if (modules.Has(CoreModules.Json))
            {
                RegisterModuleType(table, typeof(JsonModule));
            }

            return table;
        }

        /// <summary>
        /// Registers the standard constants (_G, _VERSION, _NovaSharp) to a table
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        public static Table RegisterConstants(this Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            Script ownerScript = table.OwnerScript;
            DynValue novaSharpTable = DynValue.NewTable(ownerScript);
            Table m = novaSharpTable.Table;
            LuaCompatibilityProfile profile =
                ownerScript != null
                    ? ownerScript.CompatibilityProfile
                    : LuaCompatibilityProfile.ForVersion(Script.GlobalOptions.CompatibilityVersion);

            table.Set("_G", DynValue.NewTable(table));
            table.Set("_VERSION", DynValue.NewString(profile.DisplayName));
            table.Set("_NovaSharp", novaSharpTable);

            m.Set("version", DynValue.NewString(Script.VERSION));
            m.Set("luacompat", DynValue.NewString(profile.DisplayName));
            m.Set("platform", DynValue.NewString(Script.GlobalOptions.Platform.GetPlatformName()));
            m.Set("is_aot", DynValue.NewBoolean(Script.GlobalOptions.Platform.IsRunningOnAOT()));
            m.Set("is_unity", DynValue.NewBoolean(PlatformAutoDetector.IsRunningOnUnity));
            m.Set("is_mono", DynValue.NewBoolean(PlatformAutoDetector.IsRunningOnMono));
            m.Set("is_clr4", DynValue.NewBoolean(PlatformAutoDetector.IsRunningOnClr4));
            m.Set("is_pcl", DynValue.NewBoolean(PlatformAutoDetector.IsPortableFramework));
            m.Set("banner", DynValue.NewString(Script.GetBanner()));

            return table;
        }

        /// <summary>
        /// Registers a module type to the specified table
        /// </summary>
        /// <param name="gtable">The table.</param>
        /// <param name="t">The type</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">If the module contains some incompatibility</exception>
        public static Table RegisterModuleType(this Table gtable, Type t)
        {
            if (gtable == null)
            {
                throw new ArgumentNullException(nameof(gtable));
            }

            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            ModuleRegistrationDescriptor descriptor = ModuleRegistrationDescriptors.GetValue(
                t,
                CreateModuleRegistrationDescriptor
            );
            Table table = CreateModuleNamespace(gtable, descriptor.ModuleNamespace);
            Script ownerScript = table.OwnerScript;
            LuaCompatibilityVersion scriptVersion =
                ownerScript?.CompatibilityVersion ?? Script.GlobalOptions.CompatibilityVersion;

            ModuleMethodRegistration[] methodActions = descriptor.MethodActions;
            for (int i = 0; i < methodActions.Length; i++)
            {
                ModuleMethodRegistration action = methodActions[i];

                if (action.Kind == ModuleRegistrationActionKind.Init)
                {
                    action.Init(gtable, table);
                    continue;
                }

                if (!IsMemberCompatible(action.Compatibility, scriptVersion))
                {
                    continue;
                }

                if (
                    action.ArgumentViewCallback == null
                    && action.ArgumentViewNoContextCallback == null
                    && action.LegacyCallback == null
                )
                {
                    throw new ArgumentException(
                        ZString.Concat(
                            "Method ",
                            action.Method.Name,
                            " does not have the right signature."
                        )
                    );
                }

                string[] names = action.Names;
                if (action.ArgumentViewNoContextCallback != null)
                {
                    for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                    {
                        string name = names[nameIndex];
                        table.Set(
                            name,
                            DynValue.NewCallbackView(action.ArgumentViewNoContextCallback, name)
                        );
                    }
                    continue;
                }

                if (action.ArgumentViewCallback != null)
                {
                    for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                    {
                        string name = names[nameIndex];
                        table.Set(
                            name,
                            DynValue.NewCallbackView(action.ArgumentViewCallback, name)
                        );
                    }
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];
                    table.Set(name, DynValue.NewCallback(action.LegacyCallback, name));
                }
            }

            ModuleFieldRegistration[] scriptFields = descriptor.ScriptFields;
            for (int i = 0; i < scriptFields.Length; i++)
            {
                ModuleFieldRegistration scriptField = scriptFields[i];
                if (!IsMemberCompatible(scriptField.Compatibility, scriptVersion))
                {
                    continue;
                }

                RegisterScriptField(scriptField, null, table);
            }

            ModuleConstantRegistration[] constants = descriptor.Constants;
            for (int i = 0; i < constants.Length; i++)
            {
                ModuleConstantRegistration constant = constants[i];
                if (!IsMemberCompatible(constant.Compatibility, scriptVersion))
                {
                    continue;
                }

                RegisterScriptFieldAsConst(constant, null, table);
            }

            return gtable;
        }

        private static ModuleRegistrationDescriptor CreateModuleRegistrationDescriptor(Type t)
        {
            Attribute[] moduleAttributes = Framework.Do.GetCustomAttributes(
                t,
                typeof(NovaSharpModuleAttribute),
                inherit: false
            );
            NovaSharpModuleAttribute moduleAttribute = (NovaSharpModuleAttribute)
                moduleAttributes[0];

            List<ModuleMethodRegistration> methodActions = new();
            MethodInfo[] methods = Framework.Do.GetMethods(t);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo mi = methods[i];
                if (!mi.IsStatic)
                {
                    continue;
                }

                object[] methodAttributes = mi.GetCustomAttributes(
                    typeof(NovaSharpModuleMethodAttribute),
                    inherit: false
                );

                if (methodAttributes.Length > 0)
                {
                    NovaSharpModuleMethodAttribute attr = (NovaSharpModuleMethodAttribute)
                        methodAttributes[0];
                    LuaCompatibilityAttribute compatibility =
                        mi.GetCustomAttribute<LuaCompatibilityAttribute>();
                    bool hasArgumentViewNoContextSignature =
                        CallbackFunction.CheckArgumentViewNoContextCallbackSignature(mi, true);
                    bool hasArgumentViewSignature =
                        CallbackFunction.CheckArgumentViewCallbackSignature(mi, true);
                    bool hasLegacySignature = CallbackFunction.CheckLegacyCallbackSignature(
                        mi,
                        true
                    );
                    ScriptFunctionCallbackView viewFunc = null;
                    ScriptFunctionCallbackViewNoContext viewNoContextFunc = null;
                    Func<ScriptExecutionContext, CallbackArguments, DynValue> func = null;

                    if (hasArgumentViewNoContextSignature)
                    {
                        viewNoContextFunc = CreateArgumentViewNoContextCallback(mi);
                    }
                    else if (hasArgumentViewSignature)
                    {
                        viewFunc = CreateArgumentViewCallback(mi);
                    }
                    else if (hasLegacySignature)
                    {
                        func = CreateLegacyCallback(mi);
                    }

                    methodActions.Add(
                        new ModuleMethodRegistration(
                            ModuleRegistrationActionKind.Callback,
                            mi,
                            compatibility,
                            GetModuleNameVariants(attr.Name, mi.Name),
                            func,
                            viewFunc,
                            viewNoContextFunc,
                            null
                        )
                    );
                    continue;
                }

                if (mi.Name == "NovaSharpInit")
                {
                    methodActions.Add(
                        new ModuleMethodRegistration(
                            ModuleRegistrationActionKind.Init,
                            mi,
                            null,
                            Array.Empty<string>(),
                            null,
                            null,
                            null,
                            CreateModuleInit(mi)
                        )
                    );
                }
            }

            List<ModuleFieldRegistration> scriptFields = new();
            List<ModuleConstantRegistration> constants = new();
            FieldInfo[] fields = Framework.Do.GetFields(t);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo fi = fields[i];
                if (!fi.IsStatic)
                {
                    continue;
                }

                object[] methodAttributes = fi.GetCustomAttributes(
                    typeof(NovaSharpModuleMethodAttribute),
                    inherit: false
                );

                if (methodAttributes.Length == 0)
                {
                    continue;
                }

                NovaSharpModuleMethodAttribute attr = (NovaSharpModuleMethodAttribute)
                    methodAttributes[0];
                string primaryName = !string.IsNullOrEmpty(attr.Name) ? attr.Name : fi.Name;
                scriptFields.Add(
                    new ModuleFieldRegistration(
                        fi,
                        fi.GetCustomAttribute<LuaCompatibilityAttribute>(),
                        fi.Name,
                        primaryName,
                        GetModuleNameVariants(attr.Name, fi.Name)
                    )
                );
            }

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo fi = fields[i];
                if (!fi.IsStatic)
                {
                    continue;
                }

                object[] constantAttributes = fi.GetCustomAttributes(
                    typeof(NovaSharpModuleConstantAttribute),
                    inherit: false
                );

                if (constantAttributes.Length == 0)
                {
                    continue;
                }

                NovaSharpModuleConstantAttribute attr = (NovaSharpModuleConstantAttribute)
                    constantAttributes[0];
                string name = string.IsNullOrEmpty(attr.Name) ? fi.Name : attr.Name;

                constants.Add(
                    new ModuleConstantRegistration(
                        fi,
                        fi.GetCustomAttribute<LuaCompatibilityAttribute>(),
                        name,
                        GetModuleNameVariants(name, fi.Name)
                    )
                );
            }

            return new ModuleRegistrationDescriptor(
                moduleAttribute.Namespace,
                methodActions.ToArray(),
                scriptFields.ToArray(),
                constants.ToArray()
            );
        }

        private static ScriptFunctionCallbackView CreateArgumentViewCallback(MethodInfo mi)
        {
#if NETFX_CORE
            Delegate viewDeleg = mi.CreateDelegate(typeof(ScriptFunctionCallbackView));
#else
            Delegate viewDeleg = Delegate.CreateDelegate(typeof(ScriptFunctionCallbackView), mi);
#endif

            return (ScriptFunctionCallbackView)viewDeleg;
        }

        private static ScriptFunctionCallbackViewNoContext CreateArgumentViewNoContextCallback(
            MethodInfo mi
        )
        {
#if NETFX_CORE
            Delegate viewDeleg = mi.CreateDelegate(typeof(ScriptFunctionCallbackViewNoContext));
#else
            Delegate viewDeleg = Delegate.CreateDelegate(
                typeof(ScriptFunctionCallbackViewNoContext),
                mi
            );
#endif

            return (ScriptFunctionCallbackViewNoContext)viewDeleg;
        }

        private static Func<
            ScriptExecutionContext,
            CallbackArguments,
            DynValue
        > CreateLegacyCallback(MethodInfo mi)
        {
#if NETFX_CORE
            Delegate deleg = mi.CreateDelegate(
                typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>)
            );
#else
            Delegate deleg = Delegate.CreateDelegate(
                typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>),
                mi
            );
#endif

            return (Func<ScriptExecutionContext, CallbackArguments, DynValue>)deleg;
        }

        private static Action<Table, Table> CreateModuleInit(MethodInfo mi)
        {
#if NETFX_CORE
            Delegate init = mi.CreateDelegate(typeof(Action<Table, Table>));
#else
            Delegate init = Delegate.CreateDelegate(typeof(Action<Table, Table>), mi);
#endif

            return (Action<Table, Table>)init;
        }

        private static void RegisterScriptFieldAsConst(
            ModuleConstantRegistration constant,
            object o,
            Table table
        )
        {
            FieldInfo fi = constant.Field;
            DynValue constantValue;

            if (fi.FieldType == typeof(string))
            {
                constantValue = DynValue.NewString(fi.GetValue(o) as string);
            }
            else if (fi.FieldType == typeof(double))
            {
                constantValue = DynValue.NewNumber((double)fi.GetValue(o));
            }
            else if (fi.FieldType == typeof(long))
            {
                // Lua 5.3+ integer constants (math.maxinteger, math.mininteger)
                constantValue = DynValue.NewInteger((long)fi.GetValue(o));
            }
            else
            {
                throw new ArgumentException(
                    ZString.Concat(
                        "Field ",
                        constant.Name,
                        " does not have the right type - it must be string, double, or long."
                    )
                );
            }

            string[] names = constant.Names;
            for (int i = 0; i < names.Length; i++)
            {
                table.Set(names[i], constantValue);
            }
        }

        private static void RegisterScriptField(
            ModuleFieldRegistration scriptField,
            object o,
            Table table
        )
        {
            FieldInfo fi = scriptField.Field;
            if (fi.FieldType != typeof(string))
            {
                throw new ArgumentException(
                    ZString.Concat(
                        "Field ",
                        scriptField.MemberName,
                        " does not have the right type - it must be string."
                    )
                );
            }

            string val = fi.GetValue(o) as string;

            DynValue fn = table.OwnerScript.LoadFunction(val, table, scriptField.PrimaryName);

            string[] names = scriptField.Names;
            for (int i = 0; i < names.Length; i++)
            {
                table.Set(names[i], fn);
            }
        }

        private static Table CreateModuleNamespace(Table gtable, string moduleNamespace)
        {
            if (string.IsNullOrEmpty(moduleNamespace))
            {
                return gtable;
            }
            else
            {
                Table table = null;

                DynValue found = gtable.Get(moduleNamespace);

                if (found.Type == DataType.Table)
                {
                    table = found.Table;
                }
                else
                {
                    table = new Table(gtable.OwnerScript);
                    gtable.Set(moduleNamespace, DynValue.NewTable(table));
                }

                DynValue package = gtable.RawGet("package");

                if (package == null || package.Type != DataType.Table)
                {
                    gtable.Set("package", package = DynValue.NewTable(gtable.OwnerScript));
                }

                DynValue loaded = package.Table.RawGet("loaded");

                if (loaded == null || loaded.Type != DataType.Table)
                {
                    package.Table.Set("loaded", loaded = DynValue.NewTable(gtable.OwnerScript));
                }

                loaded.Table.Set(moduleNamespace, DynValue.NewTable(table));

                return table;
            }
        }

        /// <summary>
        /// Registers a module type to the specified table
        /// </summary>
        /// <typeparam name="T">The module type</typeparam>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">If the module contains some incompatibility</exception>
        public static Table RegisterModuleType<T>(this Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            return RegisterModuleType(table, typeof(T));
        }

        private static string[] GetModuleNameVariants(string explicitName, string memberName)
        {
            HashSet<string> names = new(StringComparer.Ordinal);

            void AddCandidate(string candidate)
            {
                if (!string.IsNullOrEmpty(candidate))
                {
                    names.Add(candidate);
                }
            }

            AddCandidate(explicitName);
            AddCandidate(memberName);

            if (!string.IsNullOrEmpty(memberName))
            {
                string normalized = DescriptorHelpers.NormalizeUppercaseRuns(memberName);
                AddCandidate(normalized);

                if (memberName.Length > 0)
                {
                    string lowerFirst =
                        char.ToLowerInvariant(memberName[0]) + memberName.Substring(1);
                    AddCandidate(lowerFirst);

                    string normalizedLowerFirst =
                        char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);
                    AddCandidate(normalizedLowerFirst);
                }

                AddCandidate(InvariantString.ToLowerInvariantIfNeeded(memberName));
            }

            if (!string.IsNullOrEmpty(explicitName) && explicitName != memberName)
            {
                string normalizedExplicit = DescriptorHelpers.NormalizeUppercaseRuns(explicitName);
                AddCandidate(normalizedExplicit);

                if (explicitName.Length > 0)
                {
                    string lowerFirstExplicit =
                        char.ToLowerInvariant(explicitName[0]) + explicitName.Substring(1);
                    AddCandidate(lowerFirstExplicit);
                }
            }

            string[] result = new string[names.Count];
            names.CopyTo(result);
            return result;
        }

        private static bool IsMemberCompatible(
            LuaCompatibilityAttribute attr,
            LuaCompatibilityVersion scriptVersion
        )
        {
            return attr == null || attr.IsSupported(scriptVersion);
        }

        private static LuaCompatibilityProfile GetCompatibilityProfile(Script script)
        {
            return script != null
                ? script.CompatibilityProfile
                : LuaCompatibilityProfile.ForVersion(Script.GlobalOptions.CompatibilityVersion);
        }

        private static void RemoveGlobalFunction(Table globals, string functionName)
        {
            if (globals == null || string.IsNullOrEmpty(functionName))
            {
                return;
            }

            globals.Set(functionName, DynValue.Nil);
        }

        private static void RemoveTableFunction(Table globals, string memberName)
        {
            if (globals == null || string.IsNullOrEmpty(memberName))
            {
                return;
            }

            DynValue tableNamespace = globals.RawGet("table");

            if (tableNamespace != null && tableNamespace.Type == DataType.Table)
            {
                tableNamespace.Table.Set(memberName, DynValue.Nil);
            }
        }
    }
}
