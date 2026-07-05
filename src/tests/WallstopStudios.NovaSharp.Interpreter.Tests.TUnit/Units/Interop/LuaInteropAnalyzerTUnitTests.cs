namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using WallstopStudios.NovaSharp.Interop.Generator;

    public sealed class LuaInteropAnalyzerTUnitTests
    {
        private static readonly Lazy<MetadataReference[]> CachedMetadataReferences =
            new Lazy<MetadataReference[]>(CreateMetadataReferences);

        [Test]
        public async Task AnalyzerAcceptsValidPartialLuaObjectContract()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }

        [LuaMember(""move"")]
        public void Move(float x, float y) { }

        [LuaMember]
        public Team Team { get; set; }

        [LuaMetamethod(LuaMetamethodKind.ToString)]
        public string Describe()
        {
            return ""player"";
        }

        [LuaIgnore]
        public string Hidden { get; set; }
    }

    public enum Team
    {
        Red,
        Blue,
    }
}
"
                )
                .ConfigureAwait(false);

            await Assert.That(diagnostics.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsLuaObjectTypesThatAreNotPartial()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0001").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsLuaMemberAttributesOutsideLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    public class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0006").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAliasedLuaMemberAttributesOutsideLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using LuaExpose = NovaSharp.LuaMemberAttribute;

namespace Fixtures
{
    public class PlayerApi
    {
        [LuaExpose]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0006").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerDoesNotReportStandaloneLuaIgnoreOutsideLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;
using LuaSkip = NovaSharp.LuaIgnoreAttribute;

namespace Fixtures
{
    public class PlayerApi
    {
        [LuaIgnore]
        public int Hidden { get; set; }

        [LuaSkip]
        public int AlsoHidden { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await Assert.That(diagnostics.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerDoesNotReportIgnoredLuaMembersOutsideLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;
using LuaExpose = NovaSharp.LuaMemberAttribute;
using LuaSkip = NovaSharp.LuaIgnoreAttribute;

namespace Fixtures
{
    public class PlayerApi
    {
        [LuaIgnore]
        [LuaMember]
        public int Hidden { get; set; }

        [LuaSkip]
        [LuaExpose]
        public int AlsoHidden { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await Assert.That(diagnostics.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsLuaMemberOperatorsOutsideLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    public sealed class NumberBox
    {
        public NumberBox(int value)
        {
            Value = value;
        }

        public int Value { get; }

        [LuaMember]
        public static NumberBox operator +(NumberBox left, NumberBox right)
        {
            return left;
        }

        [LuaMember]
        public static explicit operator int(NumberBox value)
        {
            return value.Value;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertDiagnosticCountAsync(diagnostics, "NS0006", 2).ConfigureAwait(false);
            string combinedMessage =
                diagnostics[0].GetMessage(CultureInfo.InvariantCulture)
                + Environment.NewLine
                + diagnostics[1].GetMessage(CultureInfo.InvariantCulture);
            await Assert.That(combinedMessage).Contains("operator +").ConfigureAwait(false);
            await Assert.That(combinedMessage).Contains("operator int").ConfigureAwait(false);
            await Assert.That(combinedMessage).Contains("Fixtures.NumberBox").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsDuplicateLuaVisibleNames()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""value"")]
        public int Health { get; set; }

        [LuaMember(""value"")]
        public int Score { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0004").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsInvalidLuaObjectNames()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject("""")]
    public partial class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0007").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("LuaObjectAttribute")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsInvalidLuaMemberNames()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""   "")]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0007").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("LuaMemberAttribute")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsCollisionsUsingInvalidMemberNameFallbacks()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember("""")]
        public int Health { get; set; }

        [LuaMember(""Health"")]
        public int Score { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertDiagnosticIdsAsync(diagnostics, "NS0004", "NS0007").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsSignatureDiagnosticsWhenLuaNameIsInvalid()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember("""")]
        public DateTime Timestamp { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertDiagnosticIdsAsync(diagnostics, "NS0007", "NS0002").ConfigureAwait(false);
            string unsupportedTypeMessage = null;
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Id == "NS0002")
                {
                    unsupportedTypeMessage = diagnostic.GetMessage(CultureInfo.InvariantCulture);
                }
            }

            await Assert
                .That(unsupportedTypeMessage)
                .Contains("Lua binding 'Timestamp'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsInvalidLuaMetamethodNames()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMetamethod("""")]
        public string Describe()
        {
            return ""player"";
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0007").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("LuaMetamethodAttribute")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsInvalidLuaMetamethodKinds()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMetamethod(LuaMetamethodKind.Custom)]
        public string Describe()
        {
            return ""player"";
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0007").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Custom")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerIgnoresMembersMarkedLuaIgnoreEvenWithLuaMember()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaIgnore]
        [LuaMember(""timestamp"")]
        public DateTime Timestamp { get; set; }

        [LuaIgnore]
        [LuaMember("""")]
        public DateTime IgnoredInvalidName { get; set; }

        [LuaIgnore]
        [LuaMetamethod("""")]
        public DateTime IgnoredInvalidMetamethod()
        {
            return DateTime.UtcNow;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await Assert.That(diagnostics.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsUnsupportedExposedTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""timestamp"")]
        public DateTime Timestamp { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0002").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua binding 'timestamp'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAllMetamethodNamesForSharedMemberDiagnostics()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMetamethod(LuaMetamethodKind.Add)]
        [LuaMetamethod(LuaMetamethodKind.Subtract)]
        public DateTime Combine()
        {
            return DateTime.UtcNow;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0002").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("__add")
                .ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("__sub")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsUnsupportedIndexerParameterTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""byDate"")]
        public int this[DateTime timestamp]
        {
            get
            {
                return 0;
            }
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0002").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua binding 'byDate'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsRefParameters()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""update"")]
        public void Update(ref int value) { }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua binding 'update'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsRefReturns()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        private static int Value;

        [LuaMember(""current"")]
        public ref int Current()
        {
            return ref Value;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua binding 'current'")
                .ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("a ref return")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsRefReturnMethodsAndStillChecksParameters()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        private static int Value;

        [LuaMember(""current"")]
        public unsafe ref int Current(int* value)
        {
            return ref Value;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertDiagnosticCountAsync(diagnostics, "NS0003", 2).ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsRefProperties()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        private static int Value;

        [LuaMember(""current"")]
        public ref int Current
        {
            get
            {
                return ref Value;
            }
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua binding 'current'")
                .ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("a ref return")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsRefReturnIndexersAndStillChecksParameters()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        private static int Value;

        [LuaMember(""current"")]
        public ref int this[DateTime key]
        {
            get
            {
                return ref Value;
            }
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertDiagnosticIdsAsync(diagnostics, "NS0002", "NS0003").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsPointerParameters()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember]
        public unsafe void Update(int* value) { }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsOpenGenericLuaObjectTypes()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi<T>
    {
        [LuaMember]
        public int Health { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAsyncReturnsUntilAdapterPackageExists()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System.Threading.Tasks;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMetamethod(LuaMetamethodKind.ToString)]
        public Task<int> LoadAsync()
        {
            return Task.FromResult(42);
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0005").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua member '__tostring'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAsyncVoidUntilAdapterPackageExists()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System.Threading.Tasks;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""tick"")]
        public async void Tick()
        {
            await Task.CompletedTask;
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0005").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua member 'tick'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAsyncPropertiesUntilAdapterPackageExists()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System.Threading.Tasks;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""load"")]
        public Task<int> Load
        {
            get
            {
                return Task.FromResult(42);
            }
        }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0005").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua member 'load'")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AnalyzerReportsAsyncFieldsUntilAdapterPackageExists()
        {
            Diagnostic[] diagnostics = await AnalyzeAsync(
                    @"
using System.Threading.Tasks;
using NovaSharp;

namespace Fixtures
{
    [LuaObject]
    public partial class PlayerApi
    {
        [LuaMember(""pending"")]
        public Task<int> pending;
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0005").ConfigureAwait(false);
            await Assert
                .That(diagnostics[0].GetMessage(CultureInfo.InvariantCulture))
                .Contains("Lua member 'pending'")
                .ConfigureAwait(false);
        }

        private static async Task<Diagnostic[]> AnalyzeAsync(string source)
        {
            CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.CSharp9
            );
            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true
            );
            CSharpCompilation compilation = CSharpCompilation.Create(
                "LuaInteropAnalyzerFixture",
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                GetMetadataReferences(),
                compilationOptions
            );
            ThrowIfCompilationHasErrors(compilation.GetDiagnostics());

            ImmutableArray<DiagnosticAnalyzer> analyzers =
                ImmutableArray.Create<DiagnosticAnalyzer>(new LuaInteropDiagnosticAnalyzer());
            CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
                analyzers
            );
            ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);
            List<Diagnostic> novaSharpDiagnostics = new List<Diagnostic>();
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Id.StartsWith("NS", StringComparison.Ordinal))
                {
                    novaSharpDiagnostics.Add(diagnostic);
                }
            }

            return novaSharpDiagnostics.ToArray();
        }

        private static void ThrowIfCompilationHasErrors(ImmutableArray<Diagnostic> diagnostics)
        {
            List<Diagnostic> compilerErrors = new List<Diagnostic>();
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    compilerErrors.Add(diagnostic);
                }
            }

            if (compilerErrors.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "Analyzer fixture failed to compile:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, compilerErrors)
            );
        }

        private static MetadataReference[] GetMetadataReferences()
        {
            return CachedMetadataReferences.Value;
        }

        private static MetadataReference[] CreateMetadataReferences()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            string trustedPlatformAssemblies = GetTrustedPlatformAssemblies();
            foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }

            references.Add(
                MetadataReference.CreateFromFile(typeof(LuaObjectAttribute).Assembly.Location)
            );
            return references.ToArray();
        }

        private static string GetTrustedPlatformAssemblies()
        {
            string trustedPlatformAssemblies =
                AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(trustedPlatformAssemblies))
            {
                throw new InvalidOperationException(
                    "Analyzer tests require a .NET test host that exposes TRUSTED_PLATFORM_ASSEMBLIES."
                );
            }

            return trustedPlatformAssemblies;
        }

        private static async Task AssertSingleDiagnosticAsync(
            Diagnostic[] diagnostics,
            string expectedId
        )
        {
            await Assert.That(diagnostics.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(diagnostics[0].Id).IsEqualTo(expectedId).ConfigureAwait(false);
        }

        private static async Task AssertDiagnosticCountAsync(
            Diagnostic[] diagnostics,
            string expectedId,
            int expectedCount
        )
        {
            await Assert.That(diagnostics.Length).IsEqualTo(expectedCount).ConfigureAwait(false);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                await Assert.That(diagnostic.Id).IsEqualTo(expectedId).ConfigureAwait(false);
            }
        }

        private static async Task AssertDiagnosticIdsAsync(
            Diagnostic[] diagnostics,
            params string[] expectedIds
        )
        {
            await Assert
                .That(diagnostics.Length)
                .IsEqualTo(expectedIds.Length)
                .ConfigureAwait(false);
            foreach (string expectedId in expectedIds)
            {
                int count = 0;
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    if (diagnostic.Id == expectedId)
                    {
                        count++;
                    }
                }

                await Assert.That(count).IsEqualTo(1).ConfigureAwait(false);
            }
        }
    }
}
