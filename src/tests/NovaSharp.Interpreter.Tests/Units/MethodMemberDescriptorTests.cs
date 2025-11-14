namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MethodMemberDescriptorTests
    {
        [OneTimeSetUp]
        public void RegisterUserData()
        {
            UserData.RegisterType<MethodMemberDescriptorHost>();
            UserData.RegisterType<int[,]>();
        }

        [Test]
        public void Execute_LazyOptimizedInstanceActionUsesCompiledDelegate()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(nameof(MethodMemberDescriptorHost.SetName));
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewString("Lua"));

            DynValue result = descriptor.Execute(script, host, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(host.LastName, Is.EqualTo("Lua"));
                Assert.That(result.IsVoid(), Is.True);
            });
        }

        [Test]
        public void Execute_LazyOptimizedStaticFunctionReturnsDynValue()
        {
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.Sum),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(4),
                DynValue.NewNumber(5)
            );

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.That(result.Number, Is.EqualTo(9));
        }

        [Test]
        public void Execute_ReflectionModeInvokesMethodInfo()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(nameof(MethodMemberDescriptorHost.Multiply));
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(6),
                DynValue.NewNumber(7)
            );

            DynValue result = descriptor.Execute(script, host, context, args);

            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void Execute_ArrayConstructorCreatesExpectedArray()
        {
            ConstructorInfo ctor = typeof(int[,]).GetConstructor(new[] { typeof(int), typeof(int) });
            Assert.That(ctor, Is.Not.Null);
            MethodMemberDescriptor descriptor = new(ctor, InteropAccessMode.Reflection);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.UserData));
                int[,] array = (int[,])result.UserData.Object;
                Assert.That(array.GetLength(0), Is.EqualTo(2));
                Assert.That(array.GetLength(1), Is.EqualTo(3));
            });
        }

        [Test]
        public void Execute_ExtensionMethodBindsInstance()
        {
            MethodMemberDescriptorHost host = new();
            host.SetName("Nova");

            MethodInfo extension = typeof(MethodMemberDescriptorTestExtensions).GetMethod(
                nameof(MethodMemberDescriptorTestExtensions.Decorate),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(extension, InteropAccessMode.Reflection);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewString("Sharp"));

            DynValue result = descriptor.Execute(script, host, context, args);

            Assert.That(result.String, Is.EqualTo("NovaSharp"));
        }

        [Test]
        public void Execute_OutParametersReturnTuple()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.TryDouble)
            );
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(21));

            DynValue result = descriptor.Execute(script, host, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple.Length, Is.EqualTo(2));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void Constructor_DefaultAccessModeUsesGlobalDefault()
        {
            InteropAccessMode original = UserData.DefaultAccessMode;
            try
            {
                UserData.DefaultAccessMode = InteropAccessMode.Reflection;
                MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(
                    nameof(MethodMemberDescriptorHost.Sum),
                    BindingFlags.Public | BindingFlags.Static
                );

                MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Default);

                Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Reflection));
            }
            finally
            {
                UserData.DefaultAccessMode = original;
            }
        }

        [Test]
        public void Constructor_WithByRefParametersEnforcesReflectionAccessMode()
        {
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.TryDouble)
            );

            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Reflection));
        }

        [Test]
        public void Constructor_OnAotPlatformForcesReflectionAccessMode()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;
            try
            {
                Script.GlobalOptions.Platform = new AotStubPlatformAccessor();
                MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(
                    nameof(MethodMemberDescriptorHost.Sum),
                    BindingFlags.Public | BindingFlags.Static
                );

                MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

                Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Reflection));
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        [Test]
        public void TryCreateIfVisibleHonorsVisibility()
        {
            MethodInfo hidden = typeof(MethodMemberDescriptorHost).GetMethod(
                "HiddenHelper",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(hidden, Is.Not.Null);

            MethodMemberDescriptor invisible = MethodMemberDescriptor.TryCreateIfVisible(
                hidden,
                InteropAccessMode.Reflection
            );

            MethodMemberDescriptor forced = MethodMemberDescriptor.TryCreateIfVisible(
                hidden,
                InteropAccessMode.Reflection,
                forceVisibility: true
            );

            Assert.Multiple(() =>
            {
                Assert.That(invisible, Is.Null);
                Assert.That(forced, Is.Not.Null);
            });
        }

        [Test]
        public void CheckMethodIsCompatibleRejectsOpenGenericDefinitions()
        {
            MethodInfo method = typeof(GenericMethodHost)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(GenericMethodHost.Identity));

            Assert.Multiple(() =>
            {
                Assert.That(
                    MethodMemberDescriptor.CheckMethodIsCompatible(method, false),
                    Is.False
                );
                Assert.That(
                    () => MethodMemberDescriptor.CheckMethodIsCompatible(method, true),
                    Throws.TypeOf<ArgumentException>()
                        .With.Message.Contains("unresolved generic parameters")
                );
            });
        }

        [Test]
        public void CheckMethodIsCompatibleRejectsPointerParameters()
        {
            MethodInfo pointerMethod = typeof(Buffer)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "MemoryCopy" && m.GetParameters().Any(p => p.ParameterType.IsPointer));

            Assert.That(
                MethodMemberDescriptor.CheckMethodIsCompatible(pointerMethod, false),
                Is.False
            );
        }

        [Test]
        public void PrepareForWiringPopulatesDescriptorMetadata()
        {
            MethodInfo method = typeof(MethodMemberDescriptorHost).GetMethod(nameof(MethodMemberDescriptorHost.SetName));
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new Script();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            Assert.Multiple(() =>
            {
                Assert.That(table.Get("class").String, Contains.Substring("MethodMemberDescriptor"));
                Assert.That(table.Get("name").String, Is.EqualTo("SetName"));
                Assert.That(table.Get("ctor").Boolean, Is.False);
                Assert.That(table.Get("decltype").String, Is.EqualTo(typeof(MethodMemberDescriptorHost).FullName));
                Assert.That(table.Get("static").Boolean, Is.False);
            });
        }

        [Test]
        public void PrepareForWiringArrayConstructorIncludesArrayMetadata()
        {
            ConstructorInfo ctor = typeof(int[,]).GetConstructor(new[] { typeof(int), typeof(int) });
            MethodMemberDescriptor descriptor = new(ctor, InteropAccessMode.Reflection);

            Script script = new Script();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            Assert.Multiple(() =>
            {
                Assert.That(table.Get("ctor").Boolean, Is.True);
                Assert.That(table.Get("arraytype").String, Is.EqualTo(typeof(int).FullName));
            });
        }
    }
 
    internal sealed class MethodMemberDescriptorHost
    {
        public string LastName { get; private set; } = string.Empty;
 
        public MethodMemberDescriptorHost()
        {
        }
 
        public MethodMemberDescriptorHost(string name)
        {
            LastName = name;
        }
 
        public void SetName(string name)
        {
            LastName = name;
        }
 
        public int Multiply(int left, int right)
        {
            return left * right;
        }
 
        public bool TryDouble(int value, out int doubled)
        {
            doubled = value * 2;
            return true;
        }
 
        private void HiddenHelper()
        {
        }
 
        public static int Sum(int left, int right)
        {
            return left + right;
        }
    }
 
    internal static class MethodMemberDescriptorTestExtensions
    {
        public static string Decorate(this MethodMemberDescriptorHost host, string suffix)
        {
            return host.LastName + suffix;
        }
    }
 
    internal static class GenericMethodHost
    {
        public static T Identity<T>(T value)
        {
            return value;
        }
    }
 
    internal sealed class AotStubPlatformAccessor : IPlatformAccessor
    {
        public CoreModules FilterSupportedCoreModules(CoreModules module)
        {
            return module;
        }
 
        public string GetEnvironmentVariable(string envvarname)
        {
            return null;
        }
 
        public bool IsRunningOnAOT()
        {
            return true;
        }
 
        public string GetPlatformName()
        {
            return "stub.aot";
        }
 
        public void DefaultPrint(string content)
        {
        }
 
        public string DefaultInput(string prompt)
        {
            return null;
        }
 
        public Stream OpenFile(Script script, string filename, Encoding encoding, string mode)
        {
            throw new NotSupportedException();
        }
 
        public Stream GetStandardStream(StandardFileType type)
        {
            return Stream.Null;
        }
 
        public string GetTempFileName()
        {
            return Path.GetTempFileName();
        }
 
        public void ExitFast(int exitCode)
        {
            throw new NotSupportedException();
        }
 
        public bool FileExists(string file)
        {
            return false;
        }
 
        public void DeleteFile(string file)
        {
            throw new NotSupportedException();
        }
 
        public void MoveFile(string src, string dst)
        {
            throw new NotSupportedException();
        }
 
        public int ExecuteCommand(string cmdline)
        {
            return 0;
        }
    }
}
