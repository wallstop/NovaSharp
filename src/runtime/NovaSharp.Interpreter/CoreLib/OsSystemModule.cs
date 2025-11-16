// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing system related Lua functions from the 'os' module.
    /// Proper support requires a compatible IPlatformAccessor
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "os")]
    public class OsSystemModule
    {
        [NovaSharpModuleMethod(Name = "execute")]
        public static DynValue Execute(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue v = args.AsType(0, "execute", DataType.String, true);

            if (v.IsNil())
            {
                return DynValue.NewBoolean(true);
            }
            else
            {
                try
                {
                    int exitCode = Script.GlobalOptions.Platform.ExecuteCommand(v.String);

                    return DynValue.NewTuple(
                        DynValue.Nil,
                        DynValue.NewString("exit"),
                        DynValue.NewNumber(exitCode)
                    );
                }
                catch (Exception)
                {
                    // +++ bad to swallow..
                    return DynValue.Nil;
                }
            }
        }

        [NovaSharpModuleMethod(Name = "exit")]
        public static DynValue Exit(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue vExitCode = args.AsType(0, "exit", DataType.Number, true);
            int exitCode = 0;

            if (vExitCode.IsNotNil())
            {
                exitCode = (int)vExitCode.Number;
            }

            Script.GlobalOptions.Platform.ExitFast(exitCode);

            throw new InvalidOperationException("Unreachable code.. reached.");
        }

        [NovaSharpModuleMethod(Name = "getenv")]
        public static DynValue Getenv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue varName = args.AsType(0, "getenv", DataType.String, false);

            string val = Script.GlobalOptions.Platform.GetEnvironmentVariable(varName.String);

            if (val == null)
            {
                return DynValue.Nil;
            }
            else
            {
                return DynValue.NewString(val);
            }
        }

        [NovaSharpModuleMethod(Name = "remove")]
        public static DynValue Remove(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            string fileName = args.AsType(0, "remove", DataType.String, false).String;

            try
            {
                if (Script.GlobalOptions.Platform.FileExists(fileName))
                {
                    Script.GlobalOptions.Platform.DeleteFile(fileName);
                    return DynValue.True;
                }
                else
                {
                    return DynValue.NewTuple(
                        DynValue.Nil,
                        DynValue.NewString("{0}: No such file or directory.", fileName),
                        DynValue.NewNumber(-1)
                    );
                }
            }
            catch (Exception ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        [NovaSharpModuleMethod(Name = "rename")]
        public static DynValue Rename(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            string fileNameOld = args.AsType(0, "rename", DataType.String, false).String;
            string fileNameNew = args.AsType(1, "rename", DataType.String, false).String;

            try
            {
                if (!Script.GlobalOptions.Platform.FileExists(fileNameOld))
                {
                    return DynValue.NewTuple(
                        DynValue.Nil,
                        DynValue.NewString("{0}: No such file or directory.", fileNameOld),
                        DynValue.NewNumber(-1)
                    );
                }

                Script.GlobalOptions.Platform.MoveFile(fileNameOld, fileNameNew);
                return DynValue.True;
            }
            catch (Exception ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        [NovaSharpModuleMethod(Name = "setlocale")]
        public static DynValue Setlocale(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return DynValue.NewString("n/a");
        }

        [NovaSharpModuleMethod(Name = "tmpname")]
        public static DynValue Tmpname(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return DynValue.NewString(Script.GlobalOptions.Platform.GetTempFileName());
        }
    }
}
