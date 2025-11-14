// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using StringLib;

    /// <summary>
    /// Class implementing string Lua functions
    /// </summary>
    [NovaSharpModule(Namespace = "string")]
public class StringModule
    {
        public const string BASE64_DUMP_HEADER = "NovaSharp_dump_b64::";

        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            Table stringMetatable = new(globalTable.OwnerScript);
            stringMetatable.Set("__index", DynValue.NewTable(stringTable));
            globalTable.OwnerScript.SetTypeMetatable(DataType.String, stringMetatable);
        }

        [NovaSharpModuleMethod(Name = "dump")]
        public static DynValue Dump(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue fn = args.AsType(0, "dump", DataType.Function, false);

            try
            {
                byte[] bytes;
                using (MemoryStream ms = new())
                {
                    executionContext.GetScript().Dump(fn, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    bytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(bytes);
                return DynValue.NewString(BASE64_DUMP_HEADER + base64);
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        [NovaSharpModuleMethod(Name = "char")]
        public static DynValue Char(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            StringBuilder sb = new(args.Count);

            for (int i = 0; i < args.Count; i++)
            {
                DynValue v = args[i];
                double d = 0d;

                if (v.Type == DataType.String)
                {
                    double? nd = v.CastToNumber();
                    if (nd == null)
                    {
                        args.AsType(i, "char", DataType.Number, false);
                    }
                    else
                    {
                        d = nd.Value;
                    }
                }
                else
                {
                    args.AsType(i, "char", DataType.Number, false);
                    d = v.Number;
                }

                int normalized = NormalizeByte(Math.Floor(d));
                sb.Append((char)normalized);
            }

            return DynValue.NewString(sb.ToString());
        }

        [NovaSharpModuleMethod(Name = "byte")]
        public static DynValue Byte(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue vs = args.AsType(0, "byte", DataType.String, false);
            DynValue vi = args.AsType(1, "byte", DataType.Number, true);
            DynValue vj = args.AsType(2, "byte", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, i => NormalizeByte(i));
        }

        [NovaSharpModuleMethod(Name = "unicode")]
        public static DynValue Unicode(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vs = args.AsType(0, "unicode", DataType.String, false);
            DynValue vi = args.AsType(1, "unicode", DataType.Number, true);
            DynValue vj = args.AsType(2, "unicode", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, i => i);
        }

        private static DynValue PerformByteLike(
            DynValue vs,
            DynValue vi,
            DynValue vj,
            Func<int, int> filter
        )
        {
            StringRange range = StringRange.FromLuaRange(vi, vj, null);
            string s = range.ApplyToString(vs.String);

            int length = s.Length;
            DynValue[] rets = new DynValue[length];

            if (length == 0)
            {
                return DynValue.Void;
            }

            for (int i = 0; i < length; ++i)
            {
                rets[i] = DynValue.NewNumber(filter((int)s[i]));
            }

            return DynValue.NewTuple(rets);
        }

        private static int NormalizeByte(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            long truncated = (long)Math.Floor(value);
            int normalized = (int)(truncated % 256);
            if (normalized < 0)
            {
                normalized += 256;
            }

            return normalized;
        }

        private static int? AdjustIndex(string s, DynValue vi, int defval)
        {
            if (vi.IsNil())
            {
                return defval;
            }

            int i = (int)Math.Round(vi.Number, 0);

            if (i == 0)
            {
                return null;
            }

            if (i > 0)
            {
                return i - 1;
            }

            return s.Length - i;
        }

        [NovaSharpModuleMethod(Name = "len")]
        public static DynValue Len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue vs = args.AsType(0, "len", DataType.String, false);
            return DynValue.NewNumber(vs.String.Length);
        }

        [NovaSharpModuleMethod(Name = "match")]
        public static DynValue Match(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return executionContext.EmulateClassicCall(args, "match", KopiLuaStringLib.str_match);
        }

        [NovaSharpModuleMethod(Name = "gmatch")]
        public static DynValue Gmatch(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return executionContext.EmulateClassicCall(args, "gmatch", KopiLuaStringLib.str_gmatch);
        }

        [NovaSharpModuleMethod(Name = "gsub")]
        public static DynValue Gsub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "gsub", KopiLuaStringLib.str_gsub);
        }

        [NovaSharpModuleMethod(Name = "find")]
        public static DynValue Find(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "find", KopiLuaStringLib.str_find);
        }

        [NovaSharpModuleMethod(Name = "lower")]
        public static DynValue Lower(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS = args.AsType(0, "lower", DataType.String, false);
            return DynValue.NewString(argS.String.ToLower());
        }

        [NovaSharpModuleMethod(Name = "upper")]
        public static DynValue Upper(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS = args.AsType(0, "upper", DataType.String, false);
            return DynValue.NewString(argS.String.ToUpper());
        }

        [NovaSharpModuleMethod(Name = "rep")]
        public static DynValue Rep(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue argS = args.AsType(0, "rep", DataType.String, false);
            DynValue argN = args.AsType(1, "rep", DataType.Number, false);
            DynValue argSep = args.AsType(2, "rep", DataType.String, true);

            if (String.IsNullOrEmpty(argS.String) || (argN.Number < 1))
            {
                return DynValue.NewString("");
            }

            string sep = (argSep.IsNotNil()) ? argSep.String : null;

            int count = (int)argN.Number;
            StringBuilder result = new(argS.String.Length * count);

            for (int i = 0; i < count; ++i)
            {
                if (i != 0 && sep != null)
                {
                    result.Append(sep);
                }

                result.Append(argS.String);
            }

            return DynValue.NewString(result.ToString());
        }

        [NovaSharpModuleMethod(Name = "format")]
        public static DynValue Format(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return executionContext.EmulateClassicCall(args, "format", KopiLuaStringLib.str_format);
        }

        [NovaSharpModuleMethod(Name = "reverse")]
        public static DynValue Reverse(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS = args.AsType(0, "reverse", DataType.String, false);

            if (String.IsNullOrEmpty(argS.String))
            {
                return DynValue.NewString("");
            }

            char[] elements = argS.String.ToCharArray();
            Array.Reverse(elements);

            return DynValue.NewString(new String(elements));
        }

        [NovaSharpModuleMethod(Name = "sub")]
        public static DynValue Sub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue argS = args.AsType(0, "sub", DataType.String, false);
            DynValue argI = args.AsType(1, "sub", DataType.Number, true);
            DynValue argJ = args.AsType(2, "sub", DataType.Number, true);

            StringRange range = StringRange.FromLuaRange(argI, argJ, -1);
            string s = range.ApplyToString(argS.String);

            return DynValue.NewString(s);
        }

        [NovaSharpModuleMethod(Name = "startswith")]
        public static DynValue StartsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS1 = args.AsType(0, "startsWith", DataType.String, true);
            DynValue argS2 = args.AsType(1, "startsWith", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(argS1.String.StartsWith(argS2.String));
        }

        [NovaSharpModuleMethod(Name = "endswith")]
        public static DynValue EndsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS1 = args.AsType(0, "endsWith", DataType.String, true);
            DynValue argS2 = args.AsType(1, "endsWith", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(argS1.String.EndsWith(argS2.String));
        }

        [NovaSharpModuleMethod(Name = "contains")]
        public static DynValue Contains(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue argS1 = args.AsType(0, "contains", DataType.String, true);
            DynValue argS2 = args.AsType(1, "contains", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(argS1.String.Contains(argS2.String));
        }
    }
}
