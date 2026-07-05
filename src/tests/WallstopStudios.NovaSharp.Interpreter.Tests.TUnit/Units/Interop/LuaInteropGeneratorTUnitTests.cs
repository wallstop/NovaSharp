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
    using WallstopStudios.NovaSharp.Interop.Generator;

    public sealed class LuaInteropGeneratorTUnitTests
    {
        private static readonly Lazy<MetadataReference[]> CachedMetadataReferences =
            new Lazy<MetadataReference[]>(CreateMetadataReferences);

        [Test]
        public async Task GeneratorEmitsGoldenSourceForLuaObjectShape()
        {
            await AssertGeneratedSourceMatchesGolden(
                    @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject(""player"")]
    public partial class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }

        [LuaMember(""move"")]
        public void Move(float x, float y) { }

        [LuaMember]
        public Team Team { get; set; }

        [LuaIgnore]
        [LuaMember(""hidden"")]
        public int Hidden { get; set; }

        [LuaMetamethod(LuaMetamethodKind.ToString)]
        public string Describe()
        {
            return ""player"";
        }
    }

    public enum Team
    {
        Red = 1,
        Blue = 2,
    }
}
",
                    "Fixtures.PlayerApi.NovaSharpLuaInterop.g.cs",
                    "PlayerApi.g.cs.txt"
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratorEmitsGoldenSourceForLuaObjectStructShape()
        {
            await AssertGeneratedSourceMatchesGolden(
                    @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial struct CounterApi
    {
        [LuaMember]
        public int Count { get; set; }
    }
}
",
                    "Fixtures.CounterApi.NovaSharpLuaInterop.g.cs",
                    "CounterApi.g.cs.txt"
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratorEmitsGoldenSourceForLuaObjectRecordShape()
        {
            await AssertGeneratedSourceMatchesGolden(
                    @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial record SaveApi
    {
        [LuaMember]
        public string Name { get; init; }
    }
}
",
                    "Fixtures.SaveApi.NovaSharpLuaInterop.g.cs",
                    "SaveApi.g.cs.txt"
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratorEmitsGoldenSourceForLuaObjectRecordStructShape()
        {
            await AssertGeneratedSourceMatchesGolden(
                    @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial record struct CounterRecordApi
    {
        [LuaMember]
        public int Count { get; init; }
    }
}
",
                    "Fixtures.CounterRecordApi.NovaSharpLuaInterop.g.cs",
                    "CounterRecordApi.g.cs.txt"
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratorSkipsInvalidNonPartialLuaObjectShapes()
        {
            GeneratedSourceResult[] generatedSources = RunGenerator(
                @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public class PlayerApi
    {
        [LuaMember]
        public int Health { get; set; }
    }
}
"
            );

            await Assert.That(generatedSources.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        private static async Task AssertGeneratedSourceMatchesGolden(
            string source,
            string expectedHintName,
            string goldenFileName
        )
        {
            GeneratedSourceResult[] generatedSources = RunGenerator(source);

            await Assert.That(generatedSources.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(generatedSources[0].HintName)
                .IsEqualTo(expectedHintName)
                .ConfigureAwait(false);

            string actual = NormalizeLineEndings(generatedSources[0].SourceText.ToString());
            string expected = LoadGoldenSource(goldenFileName);
            await Assert.That(actual).IsEqualTo(expected).ConfigureAwait(false);
        }

        private static GeneratedSourceResult[] RunGenerator(string source)
        {
            CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.CSharp10
            );
            CSharpCompilation compilation = CreateCompilation(source, parseOptions);
            ThrowIfCompilationHasErrors(compilation.GetDiagnostics());

            ISourceGenerator generator = new LuaInteropSourceGenerator().AsSourceGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { generator },
                parseOptions: parseOptions
            );
            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out Compilation outputCompilation,
                out ImmutableArray<Diagnostic> generatorDiagnostics
            );
            ThrowIfCompilationHasErrors(generatorDiagnostics);
            ThrowIfCompilationHasErrors(outputCompilation.GetDiagnostics());

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            if (runResult.Results.Length == 0)
            {
                return Array.Empty<GeneratedSourceResult>();
            }

            ImmutableArray<GeneratedSourceResult> generatedSources = runResult
                .Results[0]
                .GeneratedSources;
            GeneratedSourceResult[] results = new GeneratedSourceResult[generatedSources.Length];
            generatedSources.CopyTo(results);
            return results;
        }

        private static CSharpCompilation CreateCompilation(
            string source,
            CSharpParseOptions parseOptions
        )
        {
            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true
            );
            return CSharpCompilation.Create(
                "LuaInteropGeneratorFixture",
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                GetMetadataReferences(),
                compilationOptions
            );
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
                "Generator fixture failed to compile:"
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
                    "Generator tests require a .NET test host that exposes TRUSTED_PLATFORM_ASSEMBLIES."
                );
            }

            return trustedPlatformAssemblies;
        }

        private static string LoadGoldenSource(string fileName)
        {
            string relativePath = Path.Combine("GoldenSources", "LuaInteropGenerator", fileName);
            string path = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    string.Concat("Missing Lua interop generator golden source: ", relativePath)
                );
            }

            return NormalizeLineEndings(File.ReadAllText(path));
        }

        private static string NormalizeLineEndings(string value)
        {
            return value
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);
        }
    }
}
