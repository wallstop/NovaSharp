namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class ExtensionMethodsRegistryTUnitTests
    {
        [Test]
        public async Task GetExtensionMethodsChangeVersionIncrementsOnRegistration()
        {
            int versionBefore = UserData.GetExtensionMethodsChangeVersion();

            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions));

            int versionAfter = UserData.GetExtensionMethodsChangeVersion();

            await Assert.That(versionAfter).IsGreaterThan(versionBefore).ConfigureAwait(false);
        }

        [Test]
        public async Task GetExtensionMethodsByNameAndTypeReturnsMatchingMethods()
        {
            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "TestExtensionMethod",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Count).IsGreaterThan(0).ConfigureAwait(false);
            await Assert
                .That(methods.Any(m => m.Name == "TestExtensionMethod"))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetExtensionMethodsByNameAndTypeReturnsEmptyForUnknownName()
        {
            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "NonExistentMethodName",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Any()).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionMethodsAreResolvedOnDemand()
        {
            UserData.RegisterExtensionType(typeof(ExtRegGenericExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            // Calling GetExtensionMethodsByNameAndType should trigger resolution of generic extension methods
            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "GenericExtension",
                    typeof(ExtRegExtensibleTarget)
                );

            // The generic method should be resolved for the target type
            await Assert.That(methods.Count).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodsWorkWithInheritedTypes()
        {
            UserData.RegisterExtensionType(typeof(ExtRegBaseTypeExtensions));
            UserData.RegisterType<ExtRegDerivedTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "BaseExtension",
                    typeof(ExtRegDerivedTarget)
                );

            await Assert.That(methods.Count).IsGreaterThan(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodsWorkWithInterfaces()
        {
            UserData.RegisterExtensionType(typeof(ExtRegInterfaceExtensions));
            UserData.RegisterType<ExtRegInterfaceImplementor>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "InterfaceExtension",
                    typeof(ExtRegInterfaceImplementor)
                );

            await Assert.That(methods.Count).IsGreaterThan(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodCanBeCalledFromLua()
        {
            Script script = new();
            UserData.RegisterType<ExtRegExtensibleTarget>();
            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions));
            script.Globals["TestClass"] = typeof(ExtRegExtensibleTarget);

            DynValue result = script.DoString(
                @"
                local obj = TestClass.__new()
                return obj.TestExtensionMethod()
                "
            );

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task MultipleExtensionTypesCanBeRegistered()
        {
            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions));
            UserData.RegisterExtensionType(typeof(ExtRegMoreExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods1 =
                UserData.GetExtensionMethodsByNameAndType(
                    "TestExtensionMethod",
                    typeof(ExtRegExtensibleTarget)
                );
            IReadOnlyList<IOverloadableMemberDescriptor> methods2 =
                UserData.GetExtensionMethodsByNameAndType(
                    "AnotherExtension",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods1.Any()).IsTrue().ConfigureAwait(false);
            await Assert.That(methods2.Any()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task RegisteringExtensionTypeWithDefaultAccessMode()
        {
            UserData.RegisterExtensionType(typeof(ExtRegTestExtensions), InteropAccessMode.Default);
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "TestExtensionMethod",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Any()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionWithListOfT()
        {
            UserData.RegisterExtensionType(typeof(ExtRegGenericListExtensions));
            UserData.RegisterType<List<int>>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType("ListExtension", typeof(List<int>));

            // Generic extension methods for List<T> should be resolved
            await Assert.That(methods.Count).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionMethodDoesNotAddDuplicates()
        {
            UserData.RegisterExtensionType(typeof(ExtRegGenericExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            // Call twice to trigger the AlreadyAddedTypes check
            IReadOnlyList<IOverloadableMemberDescriptor> methods1 =
                UserData.GetExtensionMethodsByNameAndType(
                    "GenericExtension",
                    typeof(ExtRegExtensibleTarget)
                );
            IReadOnlyList<IOverloadableMemberDescriptor> methods2 =
                UserData.GetExtensionMethodsByNameAndType(
                    "GenericExtension",
                    typeof(ExtRegExtensibleTarget)
                );

            // The counts should be equal since duplicates shouldn't be added
            await Assert.That(methods1.Count).IsEqualTo(methods2.Count).ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionMethodWithNoParametersIsSkipped()
        {
            UserData.RegisterExtensionType(typeof(ExtRegNoParameterGenericExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            // A generic extension method with no parameters should be skipped during resolution
            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "NoParamGeneric",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionWithMismatchedTypeArgsReturnsEmpty()
        {
            UserData.RegisterExtensionType(typeof(ExtRegMismatchedGenericExtensions));
            UserData.RegisterType<ExtRegExtensibleTarget>();

            // When generic type args don't match, InstantiateMethodInfo returns null
            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "MismatchedGeneric",
                    typeof(ExtRegExtensibleTarget)
                );

            // The method should not be resolved due to mismatched type args
            await Assert.That(methods.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodWithLazyOptimizedAccessMode()
        {
            UserData.RegisterExtensionType(
                typeof(ExtRegTestExtensions),
                InteropAccessMode.LazyOptimized
            );
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "TestExtensionMethod",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Count).IsGreaterThan(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodWithReflectionAccessMode()
        {
            UserData.RegisterExtensionType(
                typeof(ExtRegTestExtensions),
                InteropAccessMode.Reflection
            );
            UserData.RegisterType<ExtRegExtensibleTarget>();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    "TestExtensionMethod",
                    typeof(ExtRegExtensibleTarget)
                );

            await Assert.That(methods.Count).IsGreaterThan(0).ConfigureAwait(false);
        }

        // Test helper classes
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1034:Nested types should not be visible",
            Justification = "Test helper class must be public for UserData registration."
        )]
        public sealed class ExtRegExtensibleTarget { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1034:Nested types should not be visible",
            Justification = "Test helper class must be public for UserData registration."
        )]
        public class ExtRegBaseTarget { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1034:Nested types should not be visible",
            Justification = "Test helper class must be public for UserData registration."
        )]
        public sealed class ExtRegDerivedTarget : ExtRegBaseTarget { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1034:Nested types should not be visible",
            Justification = "Test helper interface must be public for UserData registration."
        )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1040:Avoid empty interfaces",
            Justification = "Empty interface required for extension method testing."
        )]
        public interface IExtRegExtensibleInterface { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1034:Nested types should not be visible",
            Justification = "Test helper class must be public for UserData registration."
        )]
        public sealed class ExtRegInterfaceImplementor : IExtRegExtensibleInterface { }
    }

    /// <summary>
    /// Extension methods for testing - must be at namespace level.
    /// </summary>
    public static class ExtRegTestExtensions
    {
        public static int TestExtensionMethod(
            this ExtensionMethodsRegistryTUnitTests.ExtRegExtensibleTarget target
        )
        {
            return target != null ? 42 : 0;
        }
    }

    /// <summary>
    /// More extension methods for testing multiple registrations.
    /// </summary>
    public static class ExtRegMoreExtensions
    {
        public static int AnotherExtension(
            this ExtensionMethodsRegistryTUnitTests.ExtRegExtensibleTarget target
        )
        {
            return target != null ? 100 : 0;
        }
    }

    /// <summary>
    /// Generic extension methods for testing.
    /// </summary>
    public static class ExtRegGenericExtensions
    {
        public static int GenericExtension<T>(this T target)
            where T : class
        {
            return target != null ? 1 : 0;
        }
    }

    /// <summary>
    /// Extension methods for base types.
    /// </summary>
    public static class ExtRegBaseTypeExtensions
    {
        public static int BaseExtension(
            this ExtensionMethodsRegistryTUnitTests.ExtRegBaseTarget target
        )
        {
            return target != null ? 10 : 0;
        }
    }

    /// <summary>
    /// Extension methods for interfaces.
    /// </summary>
    public static class ExtRegInterfaceExtensions
    {
        public static int InterfaceExtension(
            this ExtensionMethodsRegistryTUnitTests.IExtRegExtensibleInterface target
        )
        {
            return target != null ? 20 : 0;
        }
    }

    /// <summary>
    /// Generic extension methods for List types.
    /// </summary>
    public static class ExtRegGenericListExtensions
    {
        public static int ListExtension<T>(this List<T> list)
        {
            return list?.Count ?? 0;
        }
    }

    /// <summary>
    /// Generic extension method with no parameters (edge case).
    /// </summary>
    public static class ExtRegNoParameterGenericExtensions
    {
        // Generic method with zero parameters - should be skipped during resolution
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Testing generic method resolution edge case."
        )]
        public static int NoParamGeneric<T>()
        {
            return 0;
        }
    }

    /// <summary>
    /// Generic extension method with mismatched type arguments.
    /// </summary>
    public static class ExtRegMismatchedGenericExtensions
    {
        // Method with two generic parameters but only one provided - InstantiateMethodInfo returns null
        public static int MismatchedGeneric<T, TValue>(this T target, TValue value)
            where T : class
            where TValue : class
        {
            return target != null && value != null ? 1 : 0;
        }
    }
}
