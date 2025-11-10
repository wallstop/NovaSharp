namespace NovaSharp.Interpreter.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Platforms;

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
            modules = Script.GlobalOptions.Platform.FilterSupportedCoreModules(modules);

            if (modules.Has(CoreModules.GlobalConsts))
            {
                RegisterConstants(table);
            }

            if (modules.Has(CoreModules.TableIterators))
            {
                RegisterModuleType<TableIteratorsModule>(table);
            }

            if (modules.Has(CoreModules.Basic))
            {
                RegisterModuleType<BasicModule>(table);
            }

            if (modules.Has(CoreModules.Metatables))
            {
                RegisterModuleType<MetaTableModule>(table);
            }

            if (modules.Has(CoreModules.String))
            {
                RegisterModuleType<StringModule>(table);
            }

            if (modules.Has(CoreModules.LoadMethods))
            {
                RegisterModuleType<LoadModule>(table);
            }

            if (modules.Has(CoreModules.Table))
            {
                RegisterModuleType<TableModule>(table);
            }

            if (modules.Has(CoreModules.Table))
            {
                RegisterModuleType<TableModuleGlobals>(table);
            }

            if (modules.Has(CoreModules.ErrorHandling))
            {
                RegisterModuleType<ErrorHandlingModule>(table);
            }

            if (modules.Has(CoreModules.Math))
            {
                RegisterModuleType<MathModule>(table);
            }

            if (modules.Has(CoreModules.Coroutine))
            {
                RegisterModuleType<CoroutineModule>(table);
            }

            if (modules.Has(CoreModules.Bit32))
            {
                RegisterModuleType<Bit32Module>(table);
            }

            if (modules.Has(CoreModules.Dynamic))
            {
                RegisterModuleType<DynamicModule>(table);
            }

            if (modules.Has(CoreModules.OsSystem))
            {
                RegisterModuleType<OsSystemModule>(table);
            }

            if (modules.Has(CoreModules.OsTime))
            {
                RegisterModuleType<OsTimeModule>(table);
            }

            if (modules.Has(CoreModules.Io))
            {
                RegisterModuleType<IoModule>(table);
            }

            if (modules.Has(CoreModules.Debug))
            {
                RegisterModuleType<DebugModule>(table);
            }

            if (modules.Has(CoreModules.Json))
            {
                RegisterModuleType<JsonModule>(table);
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
            DynValue novaSharpTable = DynValue.NewTable(table.OwnerScript);
            Table m = novaSharpTable.Table;

            table.Set("_G", DynValue.NewTable(table));
            table.Set("_VERSION", DynValue.NewString($"NovaSharp {Script.VERSION}"));
            table.Set("_NovaSharp", novaSharpTable);

            m.Set("version", DynValue.NewString(Script.VERSION));
            m.Set("luacompat", DynValue.NewString(Script.LuaVersion));
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
            Table table = CreateModuleNamespace(gtable, t);

            foreach (MethodInfo mi in Framework.Do.GetMethods(t).Where(mi => mi.IsStatic))
            {
                if (
                    mi.GetCustomAttributes(typeof(NovaSharpModuleMethodAttribute), false)
                        .ToArray()
                        .Length > 0
                )
                {
                    NovaSharpModuleMethodAttribute attr = (NovaSharpModuleMethodAttribute)
                        mi.GetCustomAttributes(typeof(NovaSharpModuleMethodAttribute), false)
                            .First();

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
                }
                else if (mi.Name == "NovaSharpInit")
                {
                    object[] args = new object[2] { gtable, table };
                    mi.Invoke(null, args);
                }
            }

            foreach (
                FieldInfo fi in Framework
                    .Do.GetFields(t)
                    .Where(mi =>
                        mi.IsStatic
                        && mi.GetCustomAttributes(typeof(NovaSharpModuleMethodAttribute), false)
                            .ToArray()
                            .Length > 0
                    )
            )
            {
                NovaSharpModuleMethodAttribute attr = (NovaSharpModuleMethodAttribute)
                    fi.GetCustomAttributes(typeof(NovaSharpModuleMethodAttribute), false).First();
                RegisterScriptField(fi, null, table, t, attr.Name, fi.Name);
            }

            foreach (
                FieldInfo fi in Framework
                    .Do.GetFields(t)
                    .Where(mi =>
                        mi.IsStatic
                        && mi.GetCustomAttributes(typeof(NovaSharpModuleConstantAttribute), false)
                            .ToArray()
                            .Length > 0
                    )
            )
            {
                NovaSharpModuleConstantAttribute attr = (NovaSharpModuleConstantAttribute)
                    fi.GetCustomAttributes(typeof(NovaSharpModuleConstantAttribute), false).First();
                string name = (!string.IsNullOrEmpty(attr.Name)) ? attr.Name : fi.Name;

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
            NovaSharpModuleAttribute attr = (NovaSharpModuleAttribute)(
                Framework.Do.GetCustomAttributes(t, typeof(NovaSharpModuleAttribute), false).First()
            );

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
            return RegisterModuleType(table, typeof(T));
        }

        private static IEnumerable<string> GetModuleNameVariants(
            string explicitName,
            string memberName
        )
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

                AddCandidate(memberName.ToLowerInvariant());
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
    }
}
