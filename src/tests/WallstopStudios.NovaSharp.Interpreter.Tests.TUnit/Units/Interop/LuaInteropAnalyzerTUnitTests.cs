namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
        [LuaMember]
        public DateTime Timestamp { get; set; }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0002").ConfigureAwait(false);
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
        [LuaMember]
        public void Update(ref int value) { }
    }
}
"
                )
                .ConfigureAwait(false);

            await AssertSingleDiagnosticAsync(diagnostics, "NS0003").ConfigureAwait(false);
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
        [LuaMember]
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

        private static MetadataReference[] GetMetadataReferences()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            string trustedPlatformAssemblies = (string)
                AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }

            references.Add(
                MetadataReference.CreateFromFile(typeof(LuaObjectAttribute).Assembly.Location)
            );
            return references.ToArray();
        }

        private static async Task AssertSingleDiagnosticAsync(
            Diagnostic[] diagnostics,
            string expectedId
        )
        {
            await Assert.That(diagnostics.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(diagnostics[0].Id).IsEqualTo(expectedId).ConfigureAwait(false);
        }
    }
}
