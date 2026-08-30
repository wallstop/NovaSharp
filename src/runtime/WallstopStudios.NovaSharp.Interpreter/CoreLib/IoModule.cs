namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using System.Text;
    using global::NovaSharp;
    using Cysharp.Text;
    using IO;
    using Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing io Lua functions. Proper support requires a compatible IPlatformAccessor
    /// </summary>
    [NovaSharpModule(Namespace = "io")]
    public static class IoModule
    {
        /// <summary>
        /// Initializes Lua's <c>io</c> module (§6.8) by registering the file userdata, wiring the
        /// module metatable, and binding the host standard streams.
        /// </summary>
        /// <param name="globalTable">The global table that will expose the <c>io</c> helpers.</param>
        /// <param name="ioTable">The table representing the <c>io</c> namespace.</param>
        public static void NovaSharpInit(Table globalTable, Table ioTable)
        {
            globalTable = ModuleArgumentValidation.RequireTable(globalTable, nameof(globalTable));
            ioTable = ModuleArgumentValidation.RequireTable(ioTable, nameof(ioTable));

            StandardUserDataDescriptor baseDescriptor = new(
                typeof(FileUserDataBase),
                InteropAccessMode.Default,
                "file"
            );
            UserData.RegisterType<FileUserDataBase>(new FileUserDataDescriptor(baseDescriptor));

            Table meta = new(ioTable.OwnerScript);
            LuaValue index = LuaValue.NewCallback(
                CallbackFunction.FromArgumentView(
                    ioTable.OwnerScript,
                    __index_callback,
                    "__index_callback"
                )
            );
            meta.Set(Metamethods.Index, index);
            ioTable.MetaTable = meta;

            SetStandardFile(
                globalTable.OwnerScript,
                StandardFileType.StdIn,
                globalTable.OwnerScript.Options.Stdin
            );
            SetStandardFile(
                globalTable.OwnerScript,
                StandardFileType.StdOut,
                globalTable.OwnerScript.Options.Stdout
            );
            SetStandardFile(
                globalTable.OwnerScript,
                StandardFileType.StdErr,
                globalTable.OwnerScript.Options.Stderr
            );
        }

        private static LuaValue __index_callback(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            string name = args[1].CastToString();

            if (name == "stdin")
            {
                return GetStandardFile(executionContext.Script, StandardFileType.StdIn);
            }
            else if (name == "stdout")
            {
                return GetStandardFile(executionContext.Script, StandardFileType.StdOut);
            }
            else if (name == "stderr")
            {
                return GetStandardFile(executionContext.Script, StandardFileType.StdErr);
            }
            else
            {
                return LuaValue.Nil;
            }
        }

        private static LuaValue GetStandardFile(Script s, StandardFileType file)
        {
            s = ModuleArgumentValidation.RequireScript(s, nameof(s));
            Table r = s.Registry;

            LuaValue ff = r.Get("853BEAAF298648839E2C99D005E1DF94_STD_" + file.ToString());
            return ff;
        }

        private static void SetStandardFile(Script s, StandardFileType file, Stream optionsStream)
        {
            s = ModuleArgumentValidation.RequireScript(s, nameof(s));
            Table r = s.Registry;

            optionsStream = optionsStream ?? Script.GlobalOptions.Platform.GetStandardStream(file);
            optionsStream ??= Stream.Null;

            FileUserDataBase udb = null;

            if (file == StandardFileType.StdIn)
            {
                udb = StandardIoFileUserDataBase.CreateInputStream(optionsStream);
            }
            else
            {
                udb = StandardIoFileUserDataBase.CreateOutputStream(optionsStream);
            }

            if (!UserData.TryCreate(s, udb, out LuaValue handle))
            {
                throw new InvalidOperationException("Failed to create standard IO userdata.");
            }

            r.Set("853BEAAF298648839E2C99D005E1DF94_STD_" + file.ToString(), handle);
        }

        private static FileUserDataBase GetDefaultFile(
            ScriptExecutionContext executionContext,
            StandardFileType file
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            Table r = executionContext.Script.Registry;

            LuaValue ff = r.Get("853BEAAF298648839E2C99D005E1DF94_" + file.ToString());

            if (ff.IsNil)
            {
                ff = GetStandardFile(executionContext.Script, file);
            }

            return ff.CheckUserDataType<FileUserDataBase>(
                "getdefaultfile(" + file.ToString() + ")"
            );
        }

        /// <summary>
        /// Stores the provided file handle as the default stream for the specified standard slot.
        /// </summary>
        /// <param name="executionContext">Execution context providing the owning script.</param>
        /// <param name="file">Which default stream (stdin/stdout/stderr) to update.</param>
        /// <param name="fileHandle">Userdata that becomes the new default handle.</param>
        private static void SetDefaultFile(
            ScriptExecutionContext executionContext,
            StandardFileType file,
            FileUserDataBase fileHandle
        )
        {
            SetDefaultFile(executionContext.Script, file, fileHandle);
        }

        /// <summary>
        /// Writes the provided userdata into the registry entry that tracks the active default stream.
        /// </summary>
        /// <param name="script">Script whose registry should be updated.</param>
        /// <param name="file">Target default stream slot.</param>
        /// <param name="fileHandle">Userdata representing the new default stream.</param>
        internal static void SetDefaultFile(
            Script script,
            StandardFileType file,
            FileUserDataBase fileHandle
        )
        {
            script = ModuleArgumentValidation.RequireScript(script, nameof(script));
            Table r = script.Registry;
            if (!UserData.TryCreate(script, fileHandle, out LuaValue handle))
            {
                throw new InvalidOperationException("Failed to create standard IO userdata.");
            }

            r.Set("853BEAAF298648839E2C99D005E1DF94_" + file.ToString(), handle);
        }

        /// <summary>
        /// Replaces one of the default <c>io</c> streams with a host <see cref="Stream"/>, wrapping it
        /// in a Lua-accessible <see cref="FileUserDataBase"/>.
        /// </summary>
        /// <param name="script">Script whose default stream should be overridden.</param>
        /// <param name="file">The standard stream slot to update.</param>
        /// <param name="stream">Host stream exposed to Lua.</param>
        public static void SetDefaultFile(Script script, StandardFileType file, Stream stream)
        {
            script = ModuleArgumentValidation.RequireScript(script, nameof(script));
            if (file == StandardFileType.StdIn)
            {
                SetDefaultFile(script, file, StandardIoFileUserDataBase.CreateInputStream(stream));
            }
            else
            {
                SetDefaultFile(script, file, StandardIoFileUserDataBase.CreateOutputStream(stream));
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.close</c> (§6.8) by closing the provided handle or the default stdout stream.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying the current script.</param>
        /// <param name="args">Optional userdata argument naming the file to close.</param>
        /// <returns><c>true</c> on success or <c>(nil, message, code)</c> for recoverable IO errors.</returns>
        [NovaSharpModuleMethod(Name = "close")]
        public static LuaValue Close(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Close(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "close")]
        private static LuaValue Close(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            FileUserDataBase outp =
                args[0]
                    .CheckUserDataType<FileUserDataBase>("close", 0, TypeValidationOptions.AllowNil)
                ?? GetDefaultFile(executionContext, StandardFileType.StdOut);
            return outp.Close(executionContext, args);
        }

        /// <summary>
        /// Implements Lua's <c>io.flush</c> by flushing the default stdout stream or a supplied handle.
        /// </summary>
        /// <param name="executionContext">Runtime context for the current script.</param>
        /// <param name="args">Optional userdata identifying which file to flush.</param>
        /// <returns>Lua boolean true when the flush succeeds.</returns>
        [NovaSharpModuleMethod(Name = "flush")]
        public static LuaValue Flush(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Flush(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "flush")]
        private static LuaValue Flush(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            FileUserDataBase outp =
                args[0]
                    .CheckUserDataType<FileUserDataBase>("close", 0, TypeValidationOptions.AllowNil)
                ?? GetDefaultFile(executionContext, StandardFileType.StdOut);
            outp.Flush();
            return LuaValue.True;
        }

        /// <summary>
        /// Implements Lua's <c>io.input</c>, returning the current default stdin or rebinding it.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying registry access.</param>
        /// <param name="args">Optional filename or userdata specifying the new default input handle.</param>
        /// <returns>The active stdin handle.</returns>
        [NovaSharpModuleMethod(Name = "input")]
        public static LuaValue Input(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Input(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "input")]
        private static LuaValue Input(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return HandleDefaultStreamSetter(executionContext, args, StandardFileType.StdIn);
        }

        /// <summary>
        /// Implements Lua's <c>io.output</c>, returning the current default stdout or rebinding it.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying registry access.</param>
        /// <param name="args">Optional filename or userdata specifying the new default output handle.</param>
        /// <returns>The active stdout handle.</returns>
        [NovaSharpModuleMethod(Name = "output")]
        public static LuaValue Output(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Output(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "output")]
        private static LuaValue Output(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return HandleDefaultStreamSetter(executionContext, args, StandardFileType.StdOut);
        }

        private static LuaValue HandleDefaultStreamSetter(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args,
            StandardFileType defaultFiles
        )
        {
            if (args.Count == 0 || args[0].IsNil)
            {
                FileUserDataBase file = GetDefaultFile(executionContext, defaultFiles);
                return CreateFileUserData(executionContext.Script, file);
            }

            FileUserDataBase inp = null;

            if (args[0].Type == DataType.String || args[0].Type == DataType.Number)
            {
                string fileName = args[0].CastToString();
                bool isInput = defaultFiles == StandardFileType.StdIn;
                inp = Open(
                    executionContext,
                    fileName,
                    GetUtf8Encoding(),
                    isInput ? "r" : "w",
                    isBinaryMode: false
                );
            }
            else
            {
                inp = args[0]
                    .CheckUserDataType<FileUserDataBase>(
                        defaultFiles == StandardFileType.StdIn ? "input" : "output",
                        0,
                        default
                    );
            }

            SetDefaultFile(executionContext, defaultFiles, inp);

            return CreateFileUserData(executionContext.Script, inp);
        }

        private static UTF8Encoding GetUtf8Encoding()
        {
            return new UTF8Encoding(false);
        }

        /// <summary>
        /// Implements Lua's <c>io.lines</c> iterator (§6.8) by streaming a host file line-by-line.
        /// </summary>
        /// <param name="executionContext">Runtime context owning the script and platform accessor.</param>
        /// <param name="args">Argument zero is the path to read.</param>
        /// <returns>
        /// In Lua 5.1-5.3: An iterator triple <c>(iterator, nil, nil)</c>.
        /// In Lua 5.4+: A quadruple <c>(iterator, nil, nil, file_handle)</c> where the file handle
        /// can be used with to-be-closed variables or manual cleanup.
        /// </returns>
        [NovaSharpModuleMethod(Name = "lines")]
        public static LuaValue Lines(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Lines(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "lines")]
        private static LuaValue Lines(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args.Count == 0)
            {
                FileUserDataBase defaultInput = GetDefaultFile(
                    executionContext,
                    StandardFileType.StdIn
                );
                return defaultInput.Lines(executionContext, args);
            }

            string filename = args.AsType(0, "lines", DataType.String, false).String;

            try
            {
                LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                    executionContext.Script.CompatibilityVersion
                );

                // Open the file for reading - keep it open for lazy iteration
                FileUserData fileHandle = Open(
                    executionContext,
                    filename,
                    GetUtf8Encoding(),
                    "r",
                    false
                );

                // Create an iterator that reads lines lazily from the file
                LuaValue iterator = EnumerableWrapper.ConvertIterator(
                    executionContext.Script,
                    CreateLazyLineIterator(fileHandle)
                );

                // Lua 5.4+ returns 4 values: (iterator, nil, nil, file_handle)
                // This allows use with to-be-closed variables
                if (version >= LuaCompatibilityVersion.Lua54)
                {
                    return LuaValue.NewTuple(
                        iterator.Tuple[0], // iterator function
                        LuaValue.Nil, // state
                        LuaValue.Nil, // initial value
                        CreateFileUserData(executionContext.Script, fileHandle) // file handle for to-be-closed
                    );
                }

                // Lua 5.1-5.3: return just the iterator triple (iterator, nil, nil)
                return iterator;
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(IoExceptionToLuaMessage(ex, filename));
            }
        }

        /// <summary>
        /// Creates a lazy line iterator for a file that yields lines one at a time.
        /// </summary>
        private static IEnumerator<LuaValue> CreateLazyLineIterator(FileUserData fileHandle)
        {
            while (true)
            {
                string line = fileHandle.ReadLineInternal();
                if (line == null)
                {
                    yield return LuaValue.Nil;
                    yield break;
                }

                yield return LuaValue.NewString(line);
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.open</c>, returning a userdata that wraps the requested file/mode/encoding.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying the platform accessor.</param>
        /// <param name="args">Filename, mode, and encoding arguments from Lua.</param>
        /// <returns>The opened file userdata or <c>(nil, message)</c> on recoverable failure.</returns>
        [NovaSharpModuleMethod(Name = "open")]
        public static LuaValue Open(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Open(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "open")]
        private static LuaValue Open(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            string filename = args.AsType(0, "open", DataType.String, false).String;
            LuaValue vmode = args.AsType(1, "open", DataType.String, true);
            LuaValue vencoding = args.AsType(2, "open", DataType.String, true);

            string mode = vmode.IsNil ? "r" : vmode.String;

            // Version-specific handling for invalid mode:
            // Lua 5.1: Returns (nil, error_message) for invalid mode
            // Lua 5.2+: Throws "bad argument #2 to 'open' (invalid mode)"
            if (ContainsInvalidModeCharacters(mode))
            {
                LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                    executionContext.Script.CompatibilityVersion
                );
                if (version == LuaCompatibilityVersion.Lua51)
                {
                    return LuaValue.NewTuple(
                        LuaValue.Nil,
                        LuaValue.NewString(filename + ": invalid mode")
                    );
                }

                throw new ScriptRuntimeException("bad argument #2 to 'open' (invalid mode)");
            }

            try
            {
                string encoding = vencoding.IsNil ? null : vencoding.String;

                // list of codes: http://msdn.microsoft.com/en-us/library/vstudio/system.text.encoding%28v=vs.90%29.aspx.
                // In addition, "binary" is available.
                Encoding e = null;
                bool isBinary = Framework.Do.StringContainsChar(mode, 'b');

                if (encoding == "binary")
                {
                    isBinary = true;
                    e = new BinaryEncoding();
                }
                else if (encoding == null)
                {
                    if (!isBinary)
                    {
                        e = GetUtf8Encoding();
                    }
                    else
                    {
                        e = new BinaryEncoding();
                    }
                }
                else
                {
                    if (isBinary)
                    {
                        throw new ScriptRuntimeException(
                            "Can't specify encodings other than nil or 'binary' for binary streams."
                        );
                    }

                    e = Encoding.GetEncoding(encoding);
                }

                return CreateFileUserData(
                    executionContext.Script,
                    Open(executionContext, filename, e, mode, isBinary)
                );
            }
            catch (Exception ex) when (IsRecoverableIoOpenException(ex))
            {
                return CreateIoOpenFailure(ex, filename);
            }
        }

        /// <summary>
        /// Converts a host exception into a Lua-facing IO error string that mirrors the reference interpreter.
        /// </summary>
        /// <param name="ex">Exception raised during IO.</param>
        /// <param name="filename">Filename involved in the operation.</param>
        /// <returns>A normalized message suitable for tuples returned by <c>io</c> APIs.</returns>
        public static string IoExceptionToLuaMessage(Exception ex, string filename)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            if (ex is FileNotFoundException)
            {
                using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                sb.Append(filename);
                sb.Append(": No such file or directory");
                return sb.ToString();
            }
            else
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.type</c>, classifying userdata handles as <c>"file"</c>, <c>"closed file"</c>, or <c>nil</c>.
        /// </summary>
        /// <param name="executionContext">Runtime context used for validation.</param>
        /// <param name="args">Arguments supplied from Lua (the value to classify).</param>
        /// <returns>A string dynvalue or <c>nil</c> when the value is not a file userdata.</returns>
        [NovaSharpModuleMethod(Name = "type")]
        public static LuaValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Type(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "type")]
        private static LuaValue Type(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args[0].Type != DataType.UserData)
            {
                return LuaValue.Nil;
            }

            if (args[0].UserData.Object is not FileUserDataBase file)
            {
                return LuaValue.Nil;
            }
            else if (file.IsOpen())
            {
                return LuaValue.NewString("file");
            }
            else
            {
                return LuaValue.NewString("closed file");
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.read</c>, delegating to the default stdin handle.
        /// </summary>
        /// <param name="executionContext">Runtime context used to locate stdin.</param>
        /// <param name="args">Format specifiers or byte counts passed from Lua.</param>
        /// <returns>The values produced by <see cref="FileUserDataBase.Read(ScriptExecutionContext, CallbackArguments)"/>.</returns>
        [NovaSharpModuleMethod(Name = "read")]
        public static LuaValue Read(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Read(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "read")]
        private static LuaValue Read(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            FileUserDataBase file = GetDefaultFile(executionContext, StandardFileType.StdIn);
            return file.Read(executionContext, args);
        }

        /// <summary>
        /// Implements Lua's <c>io.write</c>, delegating to the default stdout handle.
        /// </summary>
        /// <param name="executionContext">Runtime context used to locate stdout.</param>
        /// <param name="args">Values to write.</param>
        /// <returns>The stdout userdata for chaining.</returns>
        [NovaSharpModuleMethod(Name = "write")]
        public static LuaValue Write(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Write(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "write")]
        private static LuaValue Write(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            FileUserDataBase file = GetDefaultFile(executionContext, StandardFileType.StdOut);
            return file.Write(executionContext, args);
        }

        /// <summary>
        /// Implements Lua's <c>io.tmpfile</c> by creating an anonymous read/write file owned by the host platform.
        /// </summary>
        /// <param name="executionContext">Runtime context providing platform access.</param>
        /// <param name="args">Unused arguments; present for signature compatibility.</param>
        /// <returns>The userdata representing the temporary file.</returns>
        [NovaSharpModuleMethod(Name = "tmpfile")]
        public static LuaValue TmpFile(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return TmpFile(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "tmpfile")]
        private static LuaValue TmpFile(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            string tmpfilename = Script.GlobalOptions.Platform.GetTempFileName();
            FileUserDataBase file = Open(
                executionContext,
                tmpfilename,
                GetUtf8Encoding(),
                "w+",
                isBinaryMode: true
            );
            return CreateFileUserData(executionContext.Script, file);
        }

        private static LuaValue CreateFileUserData(Script script, FileUserDataBase file)
        {
            if (!UserData.TryCreate(script, file, out LuaValue value))
            {
                throw new InvalidOperationException("Failed to create standard IO userdata.");
            }

            return value;
        }

        /// <summary>
        /// Lua `io.popen` is intentionally unsupported for security/sandboxing reasons. Calling this
        /// helper mirrors the behaviour of the TAP suites by raising a descriptive error so callers
        /// can fall back to other mechanisms (§6.8).
        /// </summary>
        /// <param name="executionContext">Runtime context (unused).</param>
        /// <param name="args">Command/mode arguments (validated for signature compatibility).</param>
        /// <returns>Never returns—always throws.</returns>
        [NovaSharpModuleMethod(Name = "popen")]
        public static LuaValue Popen(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Popen(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "popen")]
        private static LuaValue Popen(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            throw new ScriptRuntimeException("io.popen is not supported on this platform.");
        }

        private static FileUserData Open(
            ScriptExecutionContext executionContext,
            string filename,
            Encoding encoding,
            string mode,
            bool isBinaryMode
        )
        {
            return new FileUserData(
                executionContext.Script,
                filename,
                encoding,
                mode,
                isBinaryMode
            );
        }

        private static bool ContainsInvalidModeCharacters(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                return true;
            }

            foreach (char candidate in mode)
            {
                if (!IsAllowedModeCharacter(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAllowedModeCharacter(char candidate)
        {
            return candidate switch
            {
                'r' => true,
                'a' => true,
                'w' => true,
                'b' => true,
                't' => true,
                '+' => true,
                _ => false,
            };
        }

        private static LuaValue CreateIoOpenFailure(Exception exception, string filename)
        {
            return LuaValue.NewTuple(
                LuaValue.Nil,
                LuaValue.NewString(IoExceptionToLuaMessage(exception, filename))
            );
        }

        private static bool IsRecoverableIoOpenException(Exception exception)
        {
            if (exception is null)
            {
                return false;
            }

            return exception
                is IOException
                    or UnauthorizedAccessException
                    or SecurityException
                    or NotSupportedException
                    or InvalidOperationException
                    or ArgumentException
                    or ScriptRuntimeException;
        }
    }
}
