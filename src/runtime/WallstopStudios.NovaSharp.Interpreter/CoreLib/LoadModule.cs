namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System.IO;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Implements Lua's core loading APIs (load, loadfile, dofile, require) per Lua 5.4 §4.6 so
    /// NovaSharp scripts can compile chunks from strings, files, and reader callbacks.
    /// </summary>
    [NovaSharpModule]
    public static class LoadModule
    {
        /// <summary>
        /// Initializes the `package` table with platform-specific defaults (notably `package.config`)
        /// so the Lua loader pipeline behaves consistently across hosts.
        /// </summary>
        /// <param name="globalTable">Lua global table exposed to scripts.</param>
        /// <param name="ioTable">IO table used by the bootstrapper (validated for completeness).</param>
        public static void NovaSharpInit(Table globalTable, Table ioTable)
        {
            globalTable = ModuleArgumentValidation.RequireTable(globalTable, nameof(globalTable));
            ioTable = ModuleArgumentValidation.RequireTable(ioTable, nameof(ioTable));

            DynValue package = globalTable.Get("package");

            if (package.IsNil())
            {
                package = DynValue.NewTable(globalTable.OwnerScript);
                globalTable["package"] = package;
            }
            else if (package.Type != DataType.Table)
            {
                throw new InternalErrorException(
                    "'package' global variable was found and it is not a table"
                );
            }

#if PCL || ENABLE_DOTNET || NETFX_CORE
            string cfg = "\\\n;\n?\n!\n-\n";
#else
            string cfg = System.IO.Path.DirectorySeparatorChar + "\n;\n?\n!\n-\n";
#endif

            package.Table.Set("config", DynValue.NewString(cfg));

            DynValue loaded = package.Table.RawGet("loaded");

            if (loaded == null || loaded.Type != DataType.Table)
            {
                loaded = DynValue.NewTable(globalTable.OwnerScript);
                package.Table.Set("loaded", loaded);
            }

            Table registry = globalTable.OwnerScript?.Registry;
            registry?.Set("_LOADED", loaded);
        }

        /// <summary>
        /// Checks whether a function is restricted by the script's sandbox settings.
        /// Throws <see cref="SandboxViolationException"/> if access is denied.
        /// </summary>
        /// <param name="script">Script whose sandbox settings should be checked.</param>
        /// <param name="functionName">The function name to check.</param>
        private static void CheckFunctionAccess(Script script, string functionName)
        {
            if (script == null)
            {
                return;
            }

            SandboxOptions sandbox = script.Options.Sandbox;
            if (sandbox == null || !sandbox.IsFunctionRestricted(functionName))
            {
                return;
            }

            System.Func<Script, string, bool> callback = sandbox.OnFunctionAccessDenied;
            if (callback == null || !callback(script, functionName))
            {
                throw new SandboxViolationException(
                    SandboxViolationType.FunctionAccessDenied,
                    functionName
                );
            }
        }

        /// <summary>
        /// Checks whether a module is restricted by the script's sandbox settings.
        /// Throws <see cref="SandboxViolationException"/> if access is denied.
        /// </summary>
        /// <param name="script">Script whose sandbox settings should be checked.</param>
        /// <param name="moduleName">The module name to check.</param>
        private static void CheckModuleAccess(Script script, string moduleName)
        {
            if (script == null)
            {
                return;
            }

            SandboxOptions sandbox = script.Options.Sandbox;
            if (sandbox == null || !sandbox.IsModuleRestricted(moduleName))
            {
                return;
            }

            System.Func<Script, string, bool> callback = sandbox.OnModuleAccessDenied;
            if (callback == null || !callback(script, moduleName))
            {
                throw new SandboxViolationException(
                    SandboxViolationType.ModuleAccessDenied,
                    moduleName
                );
            }
        }

        // load (ld [, source [, mode [, env]]])
        // ----------------------------------------------------------------
        // Loads a chunk.
        //
        // If ld is a string, the chunk is this string.
        //
        // If there are no syntactic errors, returns the compiled chunk as a function;
        // otherwise, returns nil plus the error message.
        //
        // source is used as the source of the chunk for error messages and debug
        // information (see §4.9). When absent, it defaults to ld, if ld is a string,
        // or to "=(load)" otherwise.
        //
        // The string mode is ignored, and assumed to be "t";
        /// <summary>
        /// Lua `load` implementation that compiles a chunk from a string or reader function and
        /// returns the resulting function or an error tuple.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In Lua 5.1, <c>load</c> only accepts a reader function. Strings must be loaded via
        /// <c>loadstring</c>. In Lua 5.2+, <c>load</c> accepts both strings and functions,
        /// and <c>loadstring</c> was removed.
        /// </para>
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// Callback arguments following the Lua signature (<c>ld, source, mode, env</c>).
        /// </param>
        /// <returns>
        /// A <see cref="DynValue"/> representing the compiled chunk or <c>(nil, errorMessage)</c> on
        /// failure.
        /// </returns>
        [NovaSharpModuleMethod(Name = "load")]
        public static DynValue Load(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            CheckFunctionAccess(executionContext.Script, "load");

            // In Lua 5.1, load() only accepts functions, not strings (use loadstring for strings)
            // In Lua 5.2+, load() accepts both strings and functions
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool allowStrings = version >= LuaCompatibilityVersion.Lua52;

            return LoadCore(executionContext, args, null, allowStrings);
        }

        // loadsafe (ld [, source [, mode [, env]]])
        // ----------------------------------------------------------------
        // Same as load, except that "env" defaults to the current environment of the function
        // calling load, instead of the actual global environment.
        /// <summary>
        /// Variant of <see cref="Load"/> that defaults the environment parameter to the caller's
        /// current environment, mirroring Lua's <c>load</c> helper pattern.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments matching the Lua <c>load</c> signature.</param>
        /// <returns>Compiled chunk or <c>(nil, errorMessage)</c> tuple.</returns>
        [NovaSharpModuleMethod(Name = "loadsafe")]
        public static DynValue LoadSafe(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            // In Lua 5.1, load() only accepts functions, not strings (use loadstring for strings)
            // In Lua 5.2+, load() accepts both strings and functions
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool allowStrings = version >= LuaCompatibilityVersion.Lua52;

            return LoadCore(
                executionContext,
                args,
                GetSafeDefaultEnv(executionContext),
                allowStrings
            );
        }

        /// <summary>
        /// Shared loader implementation used by both `load` and `loadsafe`.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Loader arguments.</param>
        /// <param name="defaultEnv">Optional default environment when callers omit `env`.</param>
        /// <param name="allowStrings">Whether to accept strings as chunk sources (false for Lua 5.1 load).</param>
        /// <returns>Compiled chunk or <c>(nil, errorMessage)</c>.</returns>
        public static DynValue LoadCore(
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            Table defaultEnv,
            bool allowStrings = true
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            try
            {
                Script s = executionContext.Script;
                DynValue ld = args[0];
                string script = "";

                if (ld.Type == DataType.Function)
                {
                    while (true)
                    {
                        DynValue ret = executionContext.Script.Call(ld);
                        if (ret.Type == DataType.String && ret.String.Length > 0)
                        {
                            script += ret.String;
                        }
                        else if (ret.IsNil())
                        {
                            break;
                        }
                        else
                        {
                            return DynValue.NewTuple(
                                DynValue.Nil,
                                DynValue.NewString("reader function must return a string")
                            );
                        }
                    }
                }
                else if (ld.Type == DataType.String)
                {
                    if (!allowStrings)
                    {
                        // Lua 5.1's load() only accepts functions, not strings
                        // Use loadstring() for strings in Lua 5.1
                        args.AsType(0, "load", DataType.Function, false);
                    }
                    script = ld.String;
                }
                else if (ld.Type == DataType.Number && allowStrings)
                {
                    // Lua 5.2+: load() accepts numbers and converts them to strings
                    // Reference Lua behavior: load(123) tries to parse "123" as Lua code
                    script = ld.CastToString();
                }
                else
                {
                    args.AsType(0, "load", DataType.Function, false);
                }

                DynValue source = args.AsType(1, "load", DataType.String, true);
                DynValue env = args.AsType(3, "load", DataType.Table, true);

                DynValue fn = s.LoadString(
                    script,
                    !env.IsNil() ? env.Table : defaultEnv,
                    !source.IsNil() ? source.String : "=(load)"
                );

                return fn;
            }
            catch (SyntaxErrorException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(GetSyntaxErrorMessage(ex))
                );
            }
        }

        // loadstring (string [, chunkname])
        // ----------------------------------------------------------------
        // Lua 5.1 only: Loads a chunk from a string. In Lua 5.2+, this functionality was
        // merged into load() and loadstring was removed.
        //
        // If there are no syntactic errors, returns the compiled chunk as a function;
        // otherwise, returns nil plus the error message.
        //
        // chunkname is used as the name of the chunk for error messages and debug
        // information. When absent, it defaults to "=(loadstring)".
        /// <summary>
        /// Lua 5.1's <c>loadstring</c> implementation that compiles a chunk from a string and
        /// returns the resulting function or an error tuple.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function is only available in Lua 5.1. In Lua 5.2+, use <c>load</c> instead,
        /// which accepts both strings and reader functions.
        /// </para>
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// Callback arguments following the Lua 5.1 signature (<c>string, chunkname</c>).
        /// </param>
        /// <returns>
        /// A <see cref="DynValue"/> representing the compiled chunk or <c>(nil, errorMessage)</c> on
        /// failure.
        /// </returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "loadstring")]
        public static DynValue LoadString(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            CheckFunctionAccess(executionContext.Script, "loadstring");

            try
            {
                Script s = executionContext.Script;
                DynValue stringArg = args.AsType(0, "loadstring", DataType.String, false);
                DynValue chunkname = args.AsType(1, "loadstring", DataType.String, true);

                DynValue fn = s.LoadString(
                    stringArg.String,
                    null,
                    !chunkname.IsNil() ? chunkname.String : "=(loadstring)"
                );

                return fn;
            }
            catch (SyntaxErrorException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(GetSyntaxErrorMessage(ex))
                );
            }
        }

        // loadfile ([filename [, mode [, env]]])
        // ----------------------------------------------------------------
        // Similar to load, but gets the chunk from file filename or from the standard input,
        // if no file name is given. INCOMPAT: stdin not supported, mode ignored
        /// <summary>
        /// Compiles and returns a chunk loaded from the specified file path (Lua `loadfile`).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments (<c>filename, mode, env</c>).</param>
        /// <returns>Compiled chunk or <c>(nil, errorMessage)</c>.</returns>
        [NovaSharpModuleMethod(Name = "loadfile")]
        public static DynValue LoadFile(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            CheckFunctionAccess(executionContext.Script, "loadfile");

            return LoadFileImpl(executionContext, args, null);
        }

        // loadfile ([filename [, mode [, env]]])
        // ----------------------------------------------------------------
        // Same as loadfile, except that "env" defaults to the current environment of the function
        // calling load, instead of the actual global environment.
        /// <summary>
        /// Variant of <see cref="LoadFile"/> that defaults the environment to the caller's current
        /// environment when not explicitly provided.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments (<c>filename, mode, env</c>).</param>
        /// <returns>Compiled chunk or <c>(nil, errorMessage)</c>.</returns>
        [NovaSharpModuleMethod(Name = "loadfilesafe")]
        public static DynValue LoadFileSafe(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return LoadFileImpl(executionContext, args, GetSafeDefaultEnv(executionContext));
        }

        /// <summary>
        /// Shared implementation for `loadfile` and `loadfilesafe`.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Caller-supplied arguments.</param>
        /// <param name="defaultEnv">Optional environment to use when callers omit one.</param>
        /// <returns>Compiled chunk or <c>(nil, errorMessage)</c>.</returns>
        private static DynValue LoadFileImpl(
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            Table defaultEnv
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            try
            {
                Script s = executionContext.Script;
                DynValue env = args.AsType(2, "loadfile", DataType.Table, true);
                Table resolvedEnv = env.IsNil() ? defaultEnv : env.Table;

                if (args.Count == 0 || args[0].IsNil())
                {
                    return LoadFromStandardInput(s, resolvedEnv);
                }

                DynValue filename = args.AsType(0, "loadfile", DataType.String, false);
                DynValue fn = s.LoadFile(filename.String, resolvedEnv);

                return fn;
            }
            catch (SyntaxErrorException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(GetSyntaxErrorMessage(ex))
                );
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Per Lua spec: loadfile returns (nil, error_message) when file cannot be opened
                string errorMessage;
                if (ex.FileName != null)
                {
                    using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                    sb.Append("cannot open ");
                    sb.Append(ex.FileName);
                    sb.Append(": No such file or directory");
                    errorMessage = sb.ToString();
                }
                else
                {
                    errorMessage = ex.Message;
                }
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(errorMessage));
            }
            catch (System.IO.IOException ex)
            {
                // Per Lua spec: loadfile returns (nil, error_message) for IO errors
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.Message));
            }
            catch (System.UnauthorizedAccessException ex)
            {
                // Per Lua spec: loadfile returns (nil, error_message) for permission errors
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.Message));
            }
        }

        /// <summary>
        /// Extracts a human-friendly syntax error message from the provided exception.
        /// </summary>
        /// <param name="ex">Syntax exception thrown during compilation.</param>
        /// <returns>Decorated error message suitable for returning to Lua.</returns>
        internal static string GetSyntaxErrorMessage(SyntaxErrorException ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            return ex.DecoratedMessage ?? ex.Message;
        }

        /// <summary>
        /// Returns the caller's current environment table, throwing if it cannot be determined.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <returns>The active global environment.</returns>
        private static Table GetSafeDefaultEnv(ScriptExecutionContext executionContext)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            Table env = executionContext.CurrentGlobalEnv;

            if (env == null)
            {
                throw new ScriptRuntimeException("current environment cannot be backtracked.");
            }

            return env;
        }

        //dofile ([filename])
        //--------------------------------------------------------------------------------------------------------------
        //Opens the named file and executes its contents as a Lua chunk. When called without arguments,
        //dofile executes the contents of the standard input (stdin). Returns all values returned by the chunk.
        //In case of errors, dofile propagates the error to its caller (that is, dofile does not run in protected mode).
        /// <summary>
        /// Executes a Lua chunk loaded from disk immediately (Lua `dofile`).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the file name.</param>
        /// <returns>Tail-call request that executes the loaded chunk.</returns>
        /// <exception cref="ScriptRuntimeException">Propagates syntax errors to the caller.</exception>
        [NovaSharpModuleMethod(Name = "dofile")]
        public static DynValue DoFile(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            CheckFunctionAccess(executionContext.Script, "dofile");

            try
            {
                Script script = executionContext.Script;
                if (args.Count == 0 || args[0].IsNil())
                {
                    DynValue stdinChunk = LoadFromStandardInput(script, null);
                    return DynValue.NewTailCallReq(stdinChunk);
                }

                DynValue fileArgument = args.AsType(0, "dofile", DataType.String, false);
                DynValue fn = script.LoadFile(fileArgument.String);
                return DynValue.NewTailCallReq(fn); // tail call to dofile
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Per Lua spec: dofile propagates errors - convert .NET exception to Lua error
                string errorMessage;
                if (ex.FileName != null)
                {
                    using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                    sb.Append("cannot open ");
                    sb.Append(ex.FileName);
                    sb.Append(": No such file or directory");
                    errorMessage = sb.ToString();
                }
                else
                {
                    errorMessage = ex.Message;
                }
                throw new ScriptRuntimeException(errorMessage);
            }
            catch (System.IO.IOException ex)
            {
                // Per Lua spec: dofile propagates errors - convert .NET exception to Lua error
                throw new ScriptRuntimeException(ex.Message);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                // Per Lua spec: dofile propagates errors - convert .NET exception to Lua error
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        //require (modname)
        //----------------------------------------------------------------------------------------------------------------
        //Loads the given module. The function starts by looking into the package.loaded table to determine whether
        //modname is already loaded. If it is, then require returns the value stored at package.loaded[modname].
        //Otherwise, it tries to find a loader for the module.
        //
        //To find a loader, require is guided by the package.loaders array. By changing this array, we can change
        //how require looks for a module. The following explanation is based on the default configuration for package.loaders.
        //
        //First require queries package.preload[modname]. If it has a value, this value (which should be a function)
        //is the loader. Otherwise require searches for a Lua loader using the path stored in package.path.
        //If that also fails, it searches for a C loader using the path stored in package.cpath. If that also fails,
        //it tries an all-in-one loader (see package.loaders).
        //
        //Once a loader is found, require calls the loader with a single argument, modname. If the loader returns any value,
        //require assigns the returned value to package.loaded[modname]. If the loader returns no value and has not assigned
        //any value to package.loaded[modname], then require assigns true to this entry. In any case, require returns the
        //final value of package.loaded[modname].
        //
        //If there is any error loading or running the module, or if it cannot find any loader for the module, then require
        //signals an error.
        /// <summary>
        /// NovaSharp's host-side require entry point; it resolves modules through the script loader
        /// and returns the compiled chunk. Lua's `require` wrapper in <see cref="REQUIRE"/> calls
        /// into this helper.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the module name.</param>
        /// <returns>Compiled module chunk.</returns>
        [NovaSharpModuleMethod(Name = "__require_clr_impl")]
        public static DynValue RequireClrCore(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            Script s = executionContext.Script;
            DynValue v = args.AsType(0, "__require_clr_impl", DataType.String, false);

            // Check module access restrictions
            CheckModuleAccess(s, v.String);

            DynValue fn = s.RequireModule(v.String);

            return fn; // tail call to dofile
        }

        private static DynValue LoadFromStandardInput(Script script, Table globalContext)
        {
            Stream stdin = script.Options.Stdin;

            if (stdin == null)
            {
                stdin = Script.GlobalOptions.Platform.GetStandardStream(
                    Platforms.StandardFileType.StdIn
                );
            }

            if (stdin == null)
            {
                throw new ScriptRuntimeException("stdin stream is not available.");
            }

            return script.LoadStream(stdin, globalContext, "stdin");
        }

        /// <summary>
        /// Lua implementation of `require` that defers to <c>__require_clr_impl</c> for actual
        /// module resolution while preserving the standard `package.loaded` semantics.
        /// </summary>
        [NovaSharpModuleMethod(Name = "require")]
        public const string REQUIRE =
            @"
function(modulename)
	if (package == nil) then package = { }; end
	if (package.loaded == nil) then package.loaded = { }; end

	local m = package.loaded[modulename];

	if (m ~= nil) then
		return m;
	end

local func = __require_clr_impl(modulename);

	local res = func(modulename);

	if (res == nil) then
		res = true;
	end

	package.loaded[modulename] = res;

	return res;
end";
    }
}
