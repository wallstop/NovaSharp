namespace WallstopStudios.NovaSharp.Interop.Generator
{
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Diagnostic descriptors emitted by the NovaSharp generated interop analyzer.
    /// </summary>
    internal static class LuaInteropDiagnostics
    {
        public static readonly DiagnosticDescriptor LuaObjectMustBePartial =
            new DiagnosticDescriptor(
                "NS0001",
                "Lua object type must be partial",
                "Lua object type '{0}' must be partial for generated interop",
                Category,
                DiagnosticSeverity.Error,
                true,
                "Generated interop emits companion partial type members and cannot extend a non-partial type."
            );

        public static readonly DiagnosticDescriptor UnsupportedType = new DiagnosticDescriptor(
            "NS0002",
            "Unsupported Lua interop type",
            "Lua binding '{0}' uses unsupported type '{1}'",
            Category,
            DiagnosticSeverity.Error,
            true,
            "Generated interop supports Lua facade types, primitives, enums, and other LuaObject types."
        );

        public static readonly DiagnosticDescriptor UnsupportedSignatureShape =
            new DiagnosticDescriptor(
                "NS0003",
                "Unsupported Lua interop signature shape",
                "Lua binding '{0}' uses {1}, which generated interop does not support",
                Category,
                DiagnosticSeverity.Error,
                true,
                "Generated interop cannot bind ref/out/in parameters, pointers, or open generic signatures."
            );

        public static readonly DiagnosticDescriptor NameCollision = new DiagnosticDescriptor(
            "NS0004",
            "Lua member name collision",
            "Lua object '{0}' exposes Lua name '{1}' more than once",
            Category,
            DiagnosticSeverity.Error,
            true,
            "Generated interop dispatches by Lua-visible member name, so each exposed name must be unique."
        );

        public static readonly DiagnosticDescriptor AsyncReturnRequiresAdapter =
            new DiagnosticDescriptor(
                "NS0005",
                "Async Lua member requires adapter package",
                "Lua member '{0}' returns async type '{1}', which requires an async adapter package",
                Category,
                DiagnosticSeverity.Error,
                true,
                "Async host members need the future async interop adapter before they can be generated safely."
            );

        public static readonly DiagnosticDescriptor MemberRequiresLuaObject =
            new DiagnosticDescriptor(
                "NS0006",
                "Lua interop attribute requires LuaObject type",
                "Lua interop attribute on '{0}' is declared on '{1}', which is not marked with LuaObjectAttribute",
                Category,
                DiagnosticSeverity.Error,
                true,
                "Generated interop only scans members declared inside a LuaObject type."
            );

        public static readonly DiagnosticDescriptor InvalidAttributeArgument =
            new DiagnosticDescriptor(
                "NS0007",
                "Invalid Lua interop attribute argument",
                "Lua interop attribute '{0}' uses invalid {1} '{2}'",
                Category,
                DiagnosticSeverity.Error,
                true,
                "Generated interop mirrors runtime attribute constructor validation for Lua names and metamethod kinds."
            );

        private const string Category = "NovaSharp.Interop.Generator";
    }
}
