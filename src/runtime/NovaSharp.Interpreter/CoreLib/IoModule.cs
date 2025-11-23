// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

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

        private static void SetDefaultFile(
            ScriptExecutionContext executionContext,
            StandardFileType file,
            FileUserDataBase fileHandle
        )
        {
            SetDefaultFile(executionContext.GetScript(), file, fileHandle);
        }

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
