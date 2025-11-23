namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using IO;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using Platforms;

    /// <summary>
    /// Class implementing io Lua functions. Proper support requires a compatible IPlatformAccessor
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "io")]
    public class IoModule
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

            UserData.RegisterType<FileUserDataBase>(InteropAccessMode.Default, "file");

            Table meta = new(ioTable.OwnerScript);
            DynValue index = DynValue.NewCallback(
                new CallbackFunction(__index_callback, "__index_callback")
            );
            meta.Set("__index", index);
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

        private static DynValue __index_callback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            string name = args[1].CastToString();

            if (name == "stdin")
            {
                return GetStandardFile(executionContext.GetScript(), StandardFileType.StdIn);
            }
            else if (name == "stdout")
            {
                return GetStandardFile(executionContext.GetScript(), StandardFileType.StdOut);
            }
            else if (name == "stderr")
            {
                return GetStandardFile(executionContext.GetScript(), StandardFileType.StdErr);
            }
            else
            {
                return DynValue.Nil;
            }
        }

        private static DynValue GetStandardFile(Script s, StandardFileType file)
        {
            s = ModuleArgumentValidation.RequireScript(s, nameof(s));
            Table r = s.Registry;

            DynValue ff = r.Get("853BEAAF298648839E2C99D005E1DF94_STD_" + file.ToString());
            return ff;
        }

        private static void SetStandardFile(Script s, StandardFileType file, Stream optionsStream)
        {
            s = ModuleArgumentValidation.RequireScript(s, nameof(s));
            Table r = s.Registry;

            optionsStream = optionsStream ?? Script.GlobalOptions.Platform.GetStandardStream(file);

            FileUserDataBase udb = null;

            if (file == StandardFileType.StdIn)
            {
                udb = StandardIoFileUserDataBase.CreateInputStream(optionsStream);
            }
            else
            {
                udb = StandardIoFileUserDataBase.CreateOutputStream(optionsStream);
            }

            r.Set("853BEAAF298648839E2C99D005E1DF94_STD_" + file.ToString(), UserData.Create(udb));
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
            Table r = executionContext.GetScript().Registry;

            DynValue ff = r.Get("853BEAAF298648839E2C99D005E1DF94_" + file.ToString());

            if (ff.IsNil())
            {
                ff = GetStandardFile(executionContext.GetScript(), file);
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
            SetDefaultFile(executionContext.GetScript(), file, fileHandle);
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
            r.Set(
                "853BEAAF298648839E2C99D005E1DF94_" + file.ToString(),
                UserData.Create(fileHandle)
            );
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
        public static DynValue Close(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            FileUserDataBase outp =
                args.AsUserData<FileUserDataBase>(0, "close", true)
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
        public static DynValue Flush(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            FileUserDataBase outp =
                args.AsUserData<FileUserDataBase>(0, "close", true)
                ?? GetDefaultFile(executionContext, StandardFileType.StdOut);
            outp.Flush();
            return DynValue.True;
        }

        /// <summary>
        /// Implements Lua's <c>io.input</c>, returning the current default stdin or rebinding it.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying registry access.</param>
        /// <param name="args">Optional filename or userdata specifying the new default input handle.</param>
        /// <returns>The active stdin handle.</returns>
        [NovaSharpModuleMethod(Name = "input")]
        public static DynValue Input(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return HandleDefaultStreamSetter(executionContext, args, StandardFileType.StdIn);
        }

        /// <summary>
        /// Implements Lua's <c>io.output</c>, returning the current default stdout or rebinding it.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying registry access.</param>
        /// <param name="args">Optional filename or userdata specifying the new default output handle.</param>
        /// <returns>The active stdout handle.</returns>
        [NovaSharpModuleMethod(Name = "output")]
        public static DynValue Output(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return HandleDefaultStreamSetter(executionContext, args, StandardFileType.StdOut);
        }

        private static DynValue HandleDefaultStreamSetter(
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            StandardFileType defaultFiles
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args.Count == 0 || args[0].IsNil())
            {
                FileUserDataBase file = GetDefaultFile(executionContext, defaultFiles);
                return UserData.Create(file);
            }

            FileUserDataBase inp = null;

            if (args[0].Type == DataType.String || args[0].Type == DataType.Number)
            {
                string fileName = args[0].CastToString();
                inp = Open(
                    executionContext,
                    fileName,
                    GetUtf8Encoding(),
                    defaultFiles == StandardFileType.StdIn ? "r" : "w"
                );
            }
            else
            {
                inp = args.AsUserData<FileUserDataBase>(
                    0,
                    defaultFiles == StandardFileType.StdIn ? "input" : "output",
                    false
                );
            }

            SetDefaultFile(executionContext, defaultFiles, inp);

            return UserData.Create(inp);
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
        /// <returns>A tuple of strings terminated by <c>nil</c>, mirroring Lua semantics.</returns>
        [NovaSharpModuleMethod(Name = "lines")]
        public static DynValue Lines(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            string filename = args.AsType(0, "lines", DataType.String, false).String;

            try
            {
                List<DynValue> readLines = new();

                using (
                    Stream stream = Script.GlobalOptions.Platform.OpenFile(
                        executionContext.GetScript(),
                        filename,
                        null,
                        "r"
                    )
                )
                {
                    using (StreamReader reader = new(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            readLines.Add(DynValue.NewString(line));
                        }
                    }
                }

                readLines.Add(DynValue.Nil);

                return DynValue.FromObject(executionContext.GetScript(), readLines.Select(s => s));
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(IoExceptionToLuaMessage(ex, filename));
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.open</c>, returning a userdata that wraps the requested file/mode/encoding.
        /// </summary>
        /// <param name="executionContext">Runtime context supplying the platform accessor.</param>
        /// <param name="args">Filename, mode, and encoding arguments from Lua.</param>
        /// <returns>The opened file userdata or <c>(nil, message)</c> on recoverable failure.</returns>
        [NovaSharpModuleMethod(Name = "open")]
        public static DynValue Open(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            string filename = args.AsType(0, "open", DataType.String, false).String;
            DynValue vmode = args.AsType(1, "open", DataType.String, true);
            DynValue vencoding = args.AsType(2, "open", DataType.String, true);

            string mode = vmode.IsNil() ? "r" : vmode.String;

            if (ContainsInvalidModeCharacters(mode))
            {
                throw ScriptRuntimeException.BadArgument(1, "open", "invalid mode");
            }

            try
            {
                string encoding = vencoding.IsNil() ? null : vencoding.String;

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

                return UserData.Create(Open(executionContext, filename, e, mode));
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
                return $"{filename}: No such file or directory";
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
        public static DynValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args[0].Type != DataType.UserData)
            {
                return DynValue.Nil;
            }

            if (args[0].UserData.Object is not FileUserDataBase file)
            {
                return DynValue.Nil;
            }
            else if (file.IsOpen())
            {
                return DynValue.NewString("file");
            }
            else
            {
                return DynValue.NewString("closed file");
            }
        }

        /// <summary>
        /// Implements Lua's <c>io.read</c>, delegating to the default stdin handle.
        /// </summary>
        /// <param name="executionContext">Runtime context used to locate stdin.</param>
        /// <param name="args">Format specifiers or byte counts passed from Lua.</param>
        /// <returns>The values produced by <see cref="FileUserDataBase.Read(ScriptExecutionContext, CallbackArguments)"/>.</returns>
        [NovaSharpModuleMethod(Name = "read")]
        public static DynValue Read(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
        public static DynValue Write(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            FileUserDataBase file = GetDefaultFile(executionContext, StandardFileType.StdOut);
            return file.Write(executionContext, args);
        }

        /// <summary>
        /// Implements Lua's <c>io.tmpfile</c> by creating an anonymous writable file owned by the host platform.
        /// </summary>
        /// <param name="executionContext">Runtime context providing platform access.</param>
        /// <param name="args">Unused arguments; present for signature compatibility.</param>
        /// <returns>The userdata representing the temporary file.</returns>
        [NovaSharpModuleMethod(Name = "tmpfile")]
        public static DynValue TmpFile(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            string tmpfilename = Script.GlobalOptions.Platform.GetTempFileName();
            FileUserDataBase file = Open(executionContext, tmpfilename, GetUtf8Encoding(), "w");
            return UserData.Create(file);
        }

        private static FileUserData Open(
            ScriptExecutionContext executionContext,
            string filename,
            Encoding encoding,
            string mode
        )
        {
            return new FileUserData(executionContext.GetScript(), filename, encoding, mode);
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

        private static DynValue CreateIoOpenFailure(Exception exception, string filename)
        {
            return DynValue.NewTuple(
                DynValue.Nil,
                DynValue.NewString(IoExceptionToLuaMessage(exception, filename))
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
