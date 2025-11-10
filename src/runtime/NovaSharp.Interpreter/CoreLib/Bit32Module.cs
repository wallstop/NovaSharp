// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Class implementing bit32 Lua functions
    /// </summary>
    [NovaSharpModule(Namespace = "bit32")]
    public class Bit32Module
    {
        private static readonly uint[] Masks = new uint[]
        {
            0x1,
            0x3,
            0x7,
            0xF,
            0x1F,
            0x3F,
            0x7F,
            0xFF,
            0x1FF,
            0x3FF,
            0x7FF,
            0xFFF,
            0x1FFF,
            0x3FFF,
            0x7FFF,
            0xFFFF,
            0x1FFFF,
            0x3FFFF,
            0x7FFFF,
            0xFFFFF,
            0x1FFFFF,
            0x3FFFFF,
            0x7FFFFF,
            0xFFFFFF,
            0x1FFFFFF,
            0x3FFFFFF,
            0x7FFFFFF,
            0xFFFFFFF,
            0x1FFFFFFF,
            0x3FFFFFFF,
            0x7FFFFFFF,
            0xFFFFFFFF,
        };

        private static uint ToUInt32(DynValue v)
        {
            double d = v.Number;
            d = Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
            return (uint)d;
        }

        private static int ToInt32(DynValue v)
        {
            double d = v.Number;
            d = Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
            return (int)d;
        }

        private static uint NBitMask(int bits)
        {
            if (bits <= 0)
            {
                return 0;
            }

            if (bits >= 32)
            {
                return Masks[31];
            }

            return Masks[bits - 1];
        }

        public static uint Bitwise(
            string funcName,
            CallbackArguments args,
            Func<uint, uint, uint> accumFunc
        )
        {
            uint accum = ToUInt32(args.AsType(0, funcName, DataType.Number, false));

            for (int i = 1; i < args.Count; i++)
            {
                uint vv = ToUInt32(args.AsType(i, funcName, DataType.Number, false));
                accum = accumFunc(accum, vv);
            }

            return accum;
        }

        [NovaSharpModuleMethod(Name = "extract")]
        public static DynValue Extract(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "extract", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vPos = args.AsType(1, "extract", DataType.Number);
            DynValue vWidth = args.AsType(2, "extract", DataType.Number, true);

            int pos = (int)vPos.Number;
            int width = (vWidth).IsNilOrNan() ? 1 : (int)vWidth.Number;

            ValidatePosWidth("extract", 2, pos, width);

            uint res = (v >> pos) & NBitMask(width);
            return DynValue.NewNumber(res);
        }

        [NovaSharpModuleMethod(Name = "replace")]
        public static DynValue Replace(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "replace", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vU = args.AsType(1, "replace", DataType.Number);
            uint u = ToUInt32(vU);
            DynValue vPos = args.AsType(2, "replace", DataType.Number);
            DynValue vWidth = args.AsType(3, "replace", DataType.Number, true);

            int pos = (int)vPos.Number;
            int width = (vWidth).IsNilOrNan() ? 1 : (int)vWidth.Number;

            ValidatePosWidth("replace", 3, pos, width);

            uint mask = NBitMask(width) << pos;
            v = v & (~mask);
            u = u & (mask);
            v = v | u;

            return DynValue.NewNumber(v);
        }

        private static void ValidatePosWidth(string func, int argPos, int pos, int width)
        {
            if (pos > 31 || (pos + width) > 31)
            {
                throw new ScriptRuntimeException("trying to access non-existent bits");
            }

            if (pos < 0)
            {
                throw new ScriptRuntimeException(
                    "bad argument #{1} to '{0}' (field cannot be negative)",
                    func,
                    argPos
                );
            }

            if (width <= 0)
            {
                throw new ScriptRuntimeException(
                    "bad argument #{1} to '{0}' (width must be positive)",
                    func,
                    argPos + 1
                );
            }
        }

        [NovaSharpModuleMethod(Name = "arshift")]
        public static DynValue Arshift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "arshift", DataType.Number);
            int v = ToInt32(vV);

            DynValue vA = args.AsType(1, "arshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v << -a;
            }
            else
            {
                v = v >> a;
            }

            return DynValue.NewNumber(v);
        }

        [NovaSharpModuleMethod(Name = "rshift")]
        public static DynValue Rshift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "rshift", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "rshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v << -a;
            }
            else
            {
                v = v >> a;
            }

            return DynValue.NewNumber(v);
        }

        [NovaSharpModuleMethod(Name = "lshift")]
        public static DynValue Lshift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "lshift", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "lshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v >> -a;
            }
            else
            {
                v = v << a;
            }

            return DynValue.NewNumber(v);
        }

        [NovaSharpModuleMethod(Name = "band")]
        public static DynValue Band(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return DynValue.NewNumber(Bitwise("band", args, (x, y) => x & y));
        }

        [NovaSharpModuleMethod(Name = "btest")]
        public static DynValue Btest(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return DynValue.NewBoolean(0 != Bitwise("btest", args, (x, y) => x & y));
        }

        [NovaSharpModuleMethod(Name = "bor")]
        public static DynValue Bor(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return DynValue.NewNumber(Bitwise("bor", args, (x, y) => x | y));
        }

        [NovaSharpModuleMethod(Name = "bnot")]
        public static DynValue Bnot(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue vV = args.AsType(0, "bnot", DataType.Number);
            uint v = ToUInt32(vV);
            return DynValue.NewNumber(~v);
        }

        [NovaSharpModuleMethod(Name = "bxor")]
        public static DynValue Bxor(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return DynValue.NewNumber(Bitwise("bxor", args, (x, y) => x ^ y));
        }

        [NovaSharpModuleMethod(Name = "lrotate")]
        public static DynValue Lrotate(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "lrotate", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "lrotate", DataType.Number);

            int a = ((int)vA.Number) % 32;

            if (a < 0)
            {
                v = (v >> (-a)) | (v << (32 + a));
            }
            else
            {
                v = (v << a) | (v >> (32 - a));
            }

            return DynValue.NewNumber(v);
        }

        [NovaSharpModuleMethod(Name = "rrotate")]
        public static DynValue Rrotate(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vV = args.AsType(0, "rrotate", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "rrotate", DataType.Number);

            int a = ((int)vA.Number) % 32;

            if (a < 0)
            {
                v = (v << (-a)) | (v >> (32 + a));
            }
            else
            {
                v = (v >> a) | (v << (32 - a));
            }

            return DynValue.NewNumber(v);
        }
    }
}
