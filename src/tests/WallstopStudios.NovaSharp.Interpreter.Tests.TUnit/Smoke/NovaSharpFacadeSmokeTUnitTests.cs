namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Smoke
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using global::WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using global::WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "These smoke tests intentionally exercise the sync facade API because it is the B0 hot path contract."
    )]
    public sealed class NovaSharpFacadeSmokeTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public async Task RunEvaluatesExpressionAcrossAllVersions(LuaCompatibilityVersion version)
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);

            LuaValue result = lua.Run("return 40 + 2");

            await Assert.That(result.Kind).IsEqualTo(LuaKind.Integer).ConfigureAwait(false);
            await Assert.That(result.AsInteger()).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CallInvokesLuaFunctionAcrossAllVersions(LuaCompatibilityVersion version)
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaFunction add = lua.Run("return function(a, b) return a + b end").AsFunction();

            LuaValue result = lua.Call(add, 20, 22);

            await Assert.That(result.AsInteger()).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CompileReturnsReusableChunkAcrossAllVersions(
            LuaCompatibilityVersion version
        )
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaChunk chunk = lua.Compile("local value = ...; return value * 2");

            LuaValue first = chunk.Run(21);
            LuaValue second = chunk.Run(12);

            await Assert.That(first.AsInteger()).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(second.AsInteger()).IsEqualTo(24).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ExecutionReturnsFirstResultAcrossAllVersions(
            LuaCompatibilityVersion version
        )
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaFunction function = lua.Run("return function() return 42, 99 end").AsFunction();
            LuaChunk chunk = lua.Compile("return 42, 99");
            LuaFunction countArguments = lua.Run("return function(...) return select('#', ...) end")
                .AsFunction();
            LuaValue noResult = lua.Run("return");

            await Assert
                .That(lua.Run("return 42, 99").AsInteger())
                .IsEqualTo(42)
                .ConfigureAwait(false);
            await Assert.That(lua.Call(function).AsInteger()).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(chunk.Run().AsInteger()).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(noResult.IsNil).IsTrue().ConfigureAwait(false);
            await Assert
                .That(lua.Call(countArguments, noResult).AsInteger())
                .IsEqualTo(1)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GlobalsAndCreatedTableRoundTripAcrossAllVersions(
            LuaCompatibilityVersion version
        )
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaTable table = lua.CreateTable();
            table["speed"] = 7;
            table[1] = "front";
            lua.Globals["config"] = table.ToValue();

            LuaTable returned = lua.Run("config.answer = config.speed * 6; return config")
                .AsTable();

            await Assert.That(returned["answer"].AsInteger()).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(returned["speed"].AsInteger()).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(returned[1].AsString()).IsEqualTo("front").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CoroutineResumesYieldedValuesAcrossAllVersions(
            LuaCompatibilityVersion version
        )
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaFunction function = lua.Run(
                    "return function(value) coroutine.yield(value + 1); return value + 2 end"
                )
                .AsFunction();
            LuaCoroutine coroutine = lua.CreateCoroutine(function);

            LuaValue yielded = coroutine.Resume(40);
            LuaValue completed = coroutine.Resume();

            await Assert.That(yielded.AsInteger()).IsEqualTo(41).ConfigureAwait(false);
            await Assert.That(completed.AsInteger()).IsEqualTo(42).ConfigureAwait(false);
            await Assert
                .That(coroutine.State)
                .IsEqualTo(LuaCoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoroutineClosePreservesErrorTuple(LuaCompatibilityVersion version)
        {
            LuaEngineOptions options = new LuaEngineOptions { Version = ToLuaVersion(version) };
            using LuaEngine lua = LuaEngine.Create(options);
            LuaFunction function = lua.Run(
                    @"
                    local function new_closable()
                        local resource = {}
                        return setmetatable(resource, {
                            __close = function(_, err)
                                error('close failure', 0)
                            end
                        })
                    end

                    return function()
                        local resource <close> = new_closable()
                        coroutine.yield('pause')
                    end
                "
                )
                .AsFunction();
            LuaCoroutine coroutine = lua.CreateCoroutine(function);

            LuaValue yielded = coroutine.Resume();
            LuaValue closeResult = coroutine.Close();
            LuaValue[] closeValues = closeResult.AsTuple();

            await Assert.That(yielded.AsString()).IsEqualTo("pause").ConfigureAwait(false);
            await Assert.That(closeValues[0].AsBoolean()).IsFalse().ConfigureAwait(false);
            await Assert
                .That(closeValues[1].AsString())
                .Contains("close failure")
                .ConfigureAwait(false);
            await Assert.That(closeResult.Owner).IsSameReferenceAs(lua).ConfigureAwait(false);
            await Assert.That(closeValues[0].Owner).IsNull().ConfigureAwait(false);
            await Assert.That(closeValues[1].Owner).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ScalarEqualityUsesLuaValueSemantics()
        {
            await Assert
                .That(LuaValue.FromInteger(256) == LuaValue.FromInteger(256))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(LuaValue.FromString("same") == LuaValue.FromString("same"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(LuaValue.FromInteger(42) == LuaValue.FromNumber(42.0))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScalarResultsDoNotCaptureEngineOwner()
        {
            using LuaEngine lua = LuaEngine.Create();
            LuaValue integer = lua.Run("return 42");
            LuaValue boolean = lua.Run("return true");
            LuaValue text = lua.Run("return 'scalar'");
            LuaTable table = lua.Run("return { value = 42 }").AsTable();
            LuaValue tableScalar = table["value"];
            LuaValue function = lua.Run("return function() return 42 end");
            LuaValue tableValue = table.ToValue();

            await Assert.That(integer.Owner).IsNull().ConfigureAwait(false);
            await Assert.That(boolean.Owner).IsNull().ConfigureAwait(false);
            await Assert.That(text.Owner).IsNull().ConfigureAwait(false);
            await Assert.That(tableScalar.Owner).IsNull().ConfigureAwait(false);
            await Assert.That(function.Owner).IsSameReferenceAs(lua).ConfigureAwait(false);
            await Assert.That(tableValue.Owner).IsSameReferenceAs(lua).ConfigureAwait(false);
        }

        [Test]
        public async Task AsIntegerRejectsFloatSubtype()
        {
            using LuaEngine lua = LuaEngine.Create();
            LuaValue value = lua.Run("return 1.5");
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LuaValue.FromString("not an integer").AsInteger()
            );
            InvalidOperationException numberException = Assert.Throws<InvalidOperationException>(
                () =>
                    LuaValue.FromString("not a number").AsNumber()
            );

            await Assert.That(value.Kind).IsEqualTo(LuaKind.Float).ConfigureAwait(false);
            await Assert
                .That(() => value.AsInteger())
                .Throws<InvalidOperationException>()
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("requires Integer").ConfigureAwait(false);
            await Assert
                .That(numberException.Message)
                .Contains("requires Number")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task TryReadReturnsFalseForConversionFailures()
        {
            bool converted = LuaValue.FromString("not an integer").TryRead(out int value);

            await Assert.That(converted).IsFalse().ConfigureAwait(false);
            await Assert.That(value).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposedEngineRejectsFacadeHandles()
        {
            LuaEngine lua = LuaEngine.Create();
            LuaFunction function = lua.Run("return function() return 1 end").AsFunction();

            lua.Dispose();

            await Assert
                .That(() => function.Call())
                .Throws<ObjectDisposedException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ForeignEngineResourcesAreRejected()
        {
            using LuaEngine first = LuaEngine.Create();
            using LuaEngine second = LuaEngine.Create();
            LuaFunction function = first
                .Run("return function(value) return value end")
                .AsFunction();

            await Assert
                .That(() => second.Call(function))
                .Throws<InvalidOperationException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task PublicFacadeExportsExpectedCoreSurface()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "PublicAPI.Shipped.txt");
            string expected = NormalizeLineEndings(
                await File.ReadAllTextAsync(path).ConfigureAwait(false)
            );
            string actual = string.Join("\n", EnumerateFacadeApi()) + "\n";
            Type[] facadeTypes = EnumerateFacadeTypes();

            await Assert.That(actual).IsEqualTo(expected).ConfigureAwait(false);
            await Assert.That(facadeTypes.Length).IsLessThanOrEqualTo(40).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicApiFormatterDistinguishesRefAndOutParameters()
        {
            MethodInfo method = typeof(RefOutProbe).GetMethod(nameof(RefOutProbe.Use));
            MethodInfo refReturnMethod = typeof(RefOutProbe).GetMethod(nameof(RefOutProbe.Return));
            ParameterInfo[] parameters = method.GetParameters();

            await Assert
                .That(FormatParameter(parameters[0]))
                .IsEqualTo("ref System.Int32 input")
                .ConfigureAwait(false);
            await Assert
                .That(FormatParameter(parameters[1]))
                .IsEqualTo("out System.Int32 output")
                .ConfigureAwait(false);
            await Assert
                .That(FormatTypeName(refReturnMethod.ReturnType))
                .IsEqualTo("ref System.Int32")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task SandboxRestrictionCollectionsAreSnapshots()
        {
            LuaSandboxOptions options = new LuaSandboxOptions()
                .RestrictModule("io")
                .RestrictFunction("load");
            IReadOnlyCollection<string> modules = options.RestrictedModules;
            IReadOnlyCollection<string> functions = options.RestrictedFunctions;

            await Assert.That(modules is HashSet<string>).IsFalse().ConfigureAwait(false);
            await Assert.That(functions is HashSet<string>).IsFalse().ConfigureAwait(false);

            ((string[])modules)[0] = "os";
            ((string[])functions)[0] = "loadfile";

            await Assert.That(options.IsModuleRestricted("io")).IsTrue().ConfigureAwait(false);
            await Assert.That(options.IsModuleRestricted("os")).IsFalse().ConfigureAwait(false);
            await Assert.That(options.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
            await Assert
                .That(options.IsFunctionRestricted("loadfile"))
                .IsFalse()
                .ConfigureAwait(false);
        }

        private static string[] EnumerateFacadeApi()
        {
            return EnumerateFacadeTypes()
                .SelectMany(GetApiLines)
                .OrderBy(static line => line, StringComparer.Ordinal)
                .ToArray();
        }

        private static Type[] EnumerateFacadeTypes()
        {
            return typeof(LuaEngine)
                .Assembly.GetExportedTypes()
                .Where(static type => type.Namespace == "NovaSharp")
                .OrderBy(static type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] GetApiLines(Type type)
        {
            return type.IsEnum ? GetEnumApiLines(type) : GetTypeApiLines(type);
        }

        private static string[] GetEnumApiLines(Type type)
        {
            string[] names = Enum.GetNames(type);
            string[] lines = new string[names.Length + 1];
            lines[0] = type.FullName;
            for (int i = 0; i < names.Length; i++)
            {
                lines[i + 1] = string.Concat(type.FullName, ".", names[i]);
            }

            return lines;
        }

        private static string[] GetTypeApiLines(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
            );
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly
            );
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly
            );
            string[] lines = new string[
                1 + constructors.Length + methods.Length + properties.Length
            ];
            lines[0] = type.FullName;
            int index = 1;
            foreach (ConstructorInfo constructor in constructors)
            {
                lines[index] = FormatConstructor(type, constructor);
                index++;
            }

            foreach (MethodInfo method in methods)
            {
                if (
                    method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                lines[index] = FormatMethod(type, method);
                index++;
            }

            foreach (PropertyInfo property in properties)
            {
                lines[index] = FormatProperty(type, property);
                index++;
            }

            Array.Resize(ref lines, index);
            return lines;
        }

        private static string FormatConstructor(Type type, ConstructorInfo constructor)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            string parameterList = string.Join(", ", parameters.Select(FormatParameter));
            return string.Concat(type.FullName, "..ctor(", parameterList, ")");
        }

        private static string FormatMethod(Type type, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string parameterList = string.Join(", ", parameters.Select(FormatParameter));
            return string.Concat(
                type.FullName,
                ".",
                method.Name,
                method.IsGenericMethodDefinition ? "<T>" : string.Empty,
                "(",
                parameterList,
                ") -> ",
                FormatTypeName(method.ReturnType)
            );
        }

        private static string FormatProperty(Type type, PropertyInfo property)
        {
            ParameterInfo[] indexParameters = property.GetIndexParameters();
            if (indexParameters.Length == 0)
            {
                return string.Concat(
                    type.FullName,
                    ".",
                    property.Name,
                    " -> ",
                    FormatTypeName(property.PropertyType)
                );
            }

            return string.Concat(
                type.FullName,
                ".",
                property.Name,
                "[",
                string.Join(", ", indexParameters.Select(FormatParameterWithoutName)),
                "] -> ",
                FormatTypeName(property.PropertyType)
            );
        }

        private static string FormatParameter(ParameterInfo parameter)
        {
            string line = string.Concat(FormatParameterTypeName(parameter), " ", parameter.Name);
            if (parameter.HasDefaultValue)
            {
                line = string.Concat(line, " = ", FormatDefaultValue(parameter));
            }

            return line;
        }

        private static string FormatParameterWithoutName(ParameterInfo parameter)
        {
            return FormatParameterTypeName(parameter);
        }

        private static string FormatParameterTypeName(ParameterInfo parameter)
        {
            if (!parameter.ParameterType.IsByRef)
            {
                return FormatTypeName(parameter.ParameterType);
            }

            string prefix = parameter.IsOut ? "out " : "ref ";
            return string.Concat(prefix, FormatTypeName(parameter.ParameterType.GetElementType()));
        }

        private static string FormatTypeName(Type type)
        {
            if (type.IsByRef)
            {
                return string.Concat("ref ", FormatTypeName(type.GetElementType()));
            }

            if (!type.IsGenericType)
            {
                return type.FullName ?? type.Name;
            }

            Type genericDefinition = type.GetGenericTypeDefinition();
            string fullName = genericDefinition.FullName;
            string name = fullName.Substring(0, fullName.IndexOf('`', StringComparison.Ordinal));
            return string.Concat(
                name,
                "<",
                string.Join(", ", type.GetGenericArguments().Select(FormatTypeName)),
                ">"
            );
        }

        private static string FormatDefaultValue(ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                return "default";
            }

            if (parameter.DefaultValue == null)
            {
                return "null";
            }

            return Convert.ToString(
                parameter.DefaultValue,
                System.Globalization.CultureInfo.InvariantCulture
            );
        }

        private static string NormalizeLineEndings(string value)
        {
            return value
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);
        }

        private static LuaVersion ToLuaVersion(LuaCompatibilityVersion version)
        {
            switch (version)
            {
                case LuaCompatibilityVersion.Latest:
                    return LuaVersion.Latest;
                case LuaCompatibilityVersion.Lua55:
                    return LuaVersion.Lua55;
                case LuaCompatibilityVersion.Lua54:
                    return LuaVersion.Lua54;
                case LuaCompatibilityVersion.Lua53:
                    return LuaVersion.Lua53;
                case LuaCompatibilityVersion.Lua52:
                    return LuaVersion.Lua52;
                case LuaCompatibilityVersion.Lua51:
                    return LuaVersion.Lua51;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version));
            }
        }

        private static class RefOutProbe
        {
            public static void Use(ref int input, out int output)
            {
                output = input;
            }

            public static ref int Return(ref int input)
            {
                return ref input;
            }
        }
    }
}
