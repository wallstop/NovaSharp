namespace WallstopStudios.NovaSharp.Interpreter.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
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
            table.Set("_VERSION", DynValue.NewString($"NovaSharp {Script.VERSION}"));
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

            Table table = CreateModuleNamespace(gtable, t);
            Script ownerScript = table.OwnerScript;
            LuaCompatibilityVersion scriptVersion =
                ownerScript?.CompatibilityVersion ?? Script.GlobalOptions.CompatibilityVersion;

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

                    if (!IsMemberCompatible(mi, scriptVersion))
                    {
                        continue;
                    }

                    if (!CallbackFunction.CheckCallbackSignature(mi, true))
                    {
                        throw new ArgumentException(
                            $"Method {mi.Name} does not have the right signature."
                        );
                    }

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

                    Func<ScriptExecutionContext, CallbackArguments, DynValue> func =
                        (Func<ScriptExecutionContext, CallbackArguments, DynValue>)deleg;

                    foreach (string name in GetModuleNameVariants(attr.Name, mi.Name))
                    {
                        table.Set(name, DynValue.NewCallback(func, name));
                    }
                    continue;
                }

                if (mi.Name == "NovaSharpInit")
                {
                    object[] args = new object[2] { gtable, table };
                    mi.Invoke(null, args);
                }
            }

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

                if (!IsMemberCompatible(fi, scriptVersion))
                {
                    continue;
                }

                NovaSharpModuleMethodAttribute attr = (NovaSharpModuleMethodAttribute)
                    methodAttributes[0];
                RegisterScriptField(fi, null, table, t, attr.Name, fi.Name);
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

                if (!IsMemberCompatible(fi, scriptVersion))
                {
                    continue;
                }

                NovaSharpModuleConstantAttribute attr = (NovaSharpModuleConstantAttribute)
                    constantAttributes[0];
                string name = string.IsNullOrEmpty(attr.Name) ? fi.Name : attr.Name;

                RegisterScriptFieldAsConst(fi, null, table, t, name);
            }

            return gtable;
        }

        private static void RegisterScriptFieldAsConst(
            FieldInfo fi,
            object o,
            Table table,
            Type t,
            string name
        )
        {
            DynValue constant;

            if (fi.FieldType == typeof(string))
            {
                constant = DynValue.NewString(fi.GetValue(o) as string);
            }
            else if (fi.FieldType == typeof(double))
            {
                constant = DynValue.NewNumber((double)fi.GetValue(o));
            }
            else
            {
                throw new ArgumentException(
                    $"Field {name} does not have the right type - it must be string or double."
                );
            }

            foreach (string alias in GetModuleNameVariants(name, fi.Name))
            {
                table.Set(alias, constant);
            }
        }

        private static void RegisterScriptField(
            FieldInfo fi,
            object o,
            Table table,
            Type t,
            string explicitName,
            string memberName
        )
        {
            if (fi.FieldType != typeof(string))
            {
                throw new ArgumentException(
                    $"Field {memberName} does not have the right type - it must be string."
                );
            }

            string val = fi.GetValue(o) as string;

            string primaryName = !string.IsNullOrEmpty(explicitName) ? explicitName : memberName;

            DynValue fn = table.OwnerScript.LoadFunction(val, table, primaryName);

            foreach (string alias in GetModuleNameVariants(explicitName, memberName))
            {
                table.Set(alias, fn);
            }
        }

        private static Table CreateModuleNamespace(Table gtable, Type t)
        {
            Attribute[] moduleAttributes = Framework.Do.GetCustomAttributes(
                t,
                typeof(NovaSharpModuleAttribute),
                inherit: false
            );
            NovaSharpModuleAttribute attr = (NovaSharpModuleAttribute)moduleAttributes[0];

            if (string.IsNullOrEmpty(attr.Namespace))
            {
                return gtable;
            }
            else
            {
                Table table = null;

                DynValue found = gtable.Get(attr.Namespace);

                if (found.Type == DataType.Table)
                {
                    table = found.Table;
                }
                else
                {
                    table = new Table(gtable.OwnerScript);
                    gtable.Set(attr.Namespace, DynValue.NewTable(table));
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

                loaded.Table.Set(attr.Namespace, DynValue.NewTable(table));

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

        private static HashSet<string> GetModuleNameVariants(string explicitName, string memberName)
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

            return names;
        }

        private static bool IsMemberCompatible(
            MemberInfo member,
            LuaCompatibilityVersion scriptVersion
        )
        {
            LuaCompatibilityAttribute attr = member.GetCustomAttribute<LuaCompatibilityAttribute>();
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
