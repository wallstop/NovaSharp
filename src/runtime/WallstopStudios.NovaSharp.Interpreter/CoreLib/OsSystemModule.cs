namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.IO;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing system related Lua functions from the 'os' module.
    /// Proper support requires a compatible IPlatformAccessor
    /// </summary>
    [NovaSharpModule(Namespace = "os")]
    public static class OsSystemModule
    {
        /// <summary>
        /// Implements Lua `os.execute`, delegating to the host platform to run a shell command and
        /// returning the Lua 5.4 status tuple `(true|nil, "exit"|"signal", code)` (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing an optional command string.</param>
        /// <returns>
        /// <see cref="DynValue.True"/> when no command is provided, `(nil,"exit",code)` on success,
        /// or <c>nil</c> when the host cannot execute commands.
        /// </returns>
        [NovaSharpModuleMethod(Name = "execute")]
        public static DynValue Execute(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args.AsType(0, "execute", DataType.String, true);

            if (v.IsNil())
            {
                // Lua returns true when the command processor exists; NovaSharp assumes success.
                return DynValue.NewBoolean(true);
            }

            try
            {
                int exitCode = Script.GlobalOptions.Platform.ExecuteCommand(v.String);
                bool exitedViaSignal = exitCode < 0;
                string terminationType = exitedViaSignal ? "signal" : "exit";
                int normalizedCode = exitedViaSignal ? -exitCode : exitCode;
                bool success = normalizedCode == 0 && !exitedViaSignal;

                return DynValue.NewTuple(
                    success ? DynValue.True : DynValue.Nil,
                    DynValue.NewString(terminationType),
                    DynValue.NewNumber(normalizedCode)
                );
            }
            catch (PlatformNotSupportedException ex)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.Message));
            }
        }

        /// <summary>
        /// Implements Lua `os.exit`, terminating the process via the host accessor. This call never
        /// returns (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing an optional numeric exit code.</param>
        /// <returns>Never returns; throws if the host does not exit.</returns>
        [NovaSharpModuleMethod(Name = "exit")]
        public static DynValue Exit(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vExitCode = args.AsType(0, "exit", DataType.Number, true);
            int exitCode = 0;

            if (vExitCode.IsNotNil())
            {
                exitCode = (int)vExitCode.Number;
            }

            Script.GlobalOptions.Platform.ExitFast(exitCode);

            throw new InvalidOperationException("Unreachable code.. reached.");
        }

        /// <summary>
        /// Implements Lua `os.getenv`, querying the host environment for a variable (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the variable name.</param>
        /// <returns>String value or <see cref="DynValue.Nil"/> when unset.</returns>
        [NovaSharpModuleMethod(Name = "getenv")]
        public static DynValue GetEnv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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

        /// <summary>
        /// Implements Lua `os.remove`, deleting a file via the platform accessor (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the path to delete.</param>
        /// <returns>
        /// <see cref="DynValue.True"/> when deleted; otherwise `(nil, message, -1)` on failure.
        /// </returns>
        [NovaSharpModuleMethod(Name = "remove")]
        public static DynValue Remove(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
            catch (IOException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        /// <summary>
        /// Implements Lua `os.rename`, renaming/moving a file via the host accessor (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing old and new file paths.</param>
        /// <returns>
        /// <see cref="DynValue.True"/> on success; `(nil, message, -1)` when the operation fails.
        /// </returns>
        [NovaSharpModuleMethod(Name = "rename")]
        public static DynValue Rename(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
            catch (IOException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        /// <summary>
        /// Implements Lua `os.setlocale`. NovaSharp does not support locale switching, so this stub
        /// always returns `"n/a"` to advertise the limitation (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused arguments.</param>
        /// <returns>String `"n/a"`.</returns>
        [NovaSharpModuleMethod(Name = "setlocale")]
        public static DynValue SetLocale(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return DynValue.NewString("n/a");
        }

        /// <summary>
        /// Implements Lua `os.tmpname`, returning a host-generated temporary file path (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused arguments.</param>
        /// <returns>String path generated by the platform accessor.</returns>
        [NovaSharpModuleMethod(Name = "tmpname")]
        public static DynValue TmpName(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return DynValue.NewString(Script.GlobalOptions.Platform.GetTempFileName());
        }
    }
}
