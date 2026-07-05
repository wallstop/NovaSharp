namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using WallstopStudios.NovaSharp.Interop.Generator;

    public sealed class LuaInteropGeneratorTUnitTests
    {
        private static readonly Lazy<MetadataReference[]> CachedMetadataReferences =
            new Lazy<MetadataReference[]>(CreateMetadataReferences);

        [Test]
        public async Task GeneratorEmitsGoldenSourceForLuaObjectShape()
        {
            await AssertGeneratedSourceMatchesGolden(
                    PlayerApiSource,
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

        [Test]
        public async Task GeneratedEnumRegistrarPopulatesStringKeyedTables()
        {
            GeneratorOutput output = RunGeneratorWithCompilation(PlayerApiSource);
            Assembly assembly = EmitAssembly(output.Compilation);
            Type playerApiType = assembly.GetType("Fixtures.PlayerApi", throwOnError: true);
            MethodInfo registerMethod = playerApiType.GetMethod(
                "__NovaSharpGeneratedRegisterEnumTables",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            await Assert.That(registerMethod).IsNotNull().ConfigureAwait(false);

            using LuaEngine engine = LuaEngine.Create();
            LuaTable destination = engine.CreateTable();
            registerMethod.Invoke(null, new object[] { engine, destination });

            await Assert.That(destination.Get("Team").IsNil).IsTrue().ConfigureAwait(false);

            LuaValue teamValue = destination.Get("Fixtures.Team");
            LuaTable teamTable = teamValue.AsTable();

            await Assert.That(teamValue.Kind).IsEqualTo(LuaKind.Table).ConfigureAwait(false);
            await Assert.That(teamTable.Get("Red").AsInteger()).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(teamTable.Get("Blue").AsInteger()).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratedEnumRegistrarSupportsSignedAndUnsignedEnumConstants()
        {
            GeneratorOutput output = RunGeneratorWithCompilation(EnumApiSource);
            Assembly assembly = EmitAssembly(output.Compilation);
            Type enumApiType = assembly.GetType("Fixtures.EnumApi", throwOnError: true);
            MethodInfo registerMethod = enumApiType.GetMethod(
                "__NovaSharpGeneratedRegisterEnumTables",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            await Assert.That(registerMethod).IsNotNull().ConfigureAwait(false);

            using LuaEngine engine = LuaEngine.Create();
            LuaTable destination = engine.CreateTable();
            registerMethod.Invoke(null, new object[] { engine, destination });

            LuaTable signedModeTable = destination.Get("SignedMode").AsTable();
            LuaTable unsignedMaskTable = destination.Get("UnsignedMask").AsTable();

            await Assert
                .That(signedModeTable.Get("Negative").AsInteger())
                .IsEqualTo(-1)
                .ConfigureAwait(false);
            await Assert
                .That(signedModeTable.Get("Min").AsInteger())
                .IsEqualTo(long.MinValue)
                .ConfigureAwait(false);
            await Assert
                .That(unsignedMaskTable.Get("Low").AsInteger())
                .IsEqualTo(4)
                .ConfigureAwait(false);
            await Assert
                .That(unsignedMaskTable.Get("High").AsNumber())
                .IsEqualTo(9223372036854775808d)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratedEnumRegistrarDisambiguatesDuplicateSimpleEnumNames()
        {
            GeneratorOutput output = RunGeneratorWithCompilation(DuplicateEnumApiSource);
            Assembly assembly = EmitAssembly(output.Compilation);
            Type collisionApiType = assembly.GetType("Fixtures.CollisionApi", throwOnError: true);
            MethodInfo registerMethod = collisionApiType.GetMethod(
                "__NovaSharpGeneratedRegisterEnumTables",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            await Assert.That(registerMethod).IsNotNull().ConfigureAwait(false);

            using LuaEngine engine = LuaEngine.Create();
            LuaTable destination = engine.CreateTable();
            registerMethod.Invoke(null, new object[] { engine, destination });

            await Assert.That(destination.Get("Team").IsNil).IsTrue().ConfigureAwait(false);

            LuaTable firstTeamTable = destination.Get("Fixtures.First.Team").AsTable();
            LuaTable secondTeamTable = destination.Get("Fixtures.Second.Team").AsTable();

            await Assert
                .That(firstTeamTable.Get("Red").AsInteger())
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert
                .That(secondTeamTable.Get("Blue").AsInteger())
                .IsEqualTo(2)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratedEnumRegistrarDoesNotOverwriteMemberNameCollisions()
        {
            GeneratorOutput output = RunGeneratorWithCompilation(MemberCollisionEnumApiSource);
            Assembly assembly = EmitAssembly(output.Compilation);
            Type collisionApiType = assembly.GetType(
                "Fixtures.MemberCollisionApi",
                throwOnError: true
            );
            MethodInfo registerMethod = collisionApiType.GetMethod(
                "__NovaSharpGeneratedRegisterEnumTables",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            await Assert.That(registerMethod).IsNotNull().ConfigureAwait(false);

            using LuaEngine engine = LuaEngine.Create();
            LuaTable destination = engine.CreateTable();
            destination.Set("Team", LuaValue.FromInteger(99));
            registerMethod.Invoke(null, new object[] { engine, destination });

            await Assert
                .That(destination.Get("Team").AsInteger())
                .IsEqualTo(99)
                .ConfigureAwait(false);

            LuaTable teamTable = destination.Get("Fixtures.Team").AsTable();

            await Assert.That(teamTable.Get("Red").AsInteger()).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task GeneratedEnumRegistrarSkipsIgnoredEnumsAndMembers()
        {
            GeneratorOutput output = RunGeneratorWithCompilation(IgnoredEnumApiSource);
            Assembly assembly = EmitAssembly(output.Compilation);
            Type ignoreApiType = assembly.GetType("Fixtures.IgnoreApi", throwOnError: true);
            MethodInfo registerMethod = ignoreApiType.GetMethod(
                "__NovaSharpGeneratedRegisterEnumTables",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            await Assert.That(registerMethod).IsNotNull().ConfigureAwait(false);

            using LuaEngine engine = LuaEngine.Create();
            LuaTable destination = engine.CreateTable();
            registerMethod.Invoke(null, new object[] { engine, destination });

            await Assert.That(destination.Get("IgnoredMode").IsNil).IsTrue().ConfigureAwait(false);

            LuaTable visibleModeTable = destination.Get("VisibleMode").AsTable();

            await Assert
                .That(visibleModeTable.Get("Shown").AsInteger())
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert.That(visibleModeTable.Get("Hidden").IsNil).IsTrue().ConfigureAwait(false);
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
            return RunGeneratorWithCompilation(source).GeneratedSources;
        }

        private static GeneratorOutput RunGeneratorWithCompilation(string source)
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
                return new GeneratorOutput(Array.Empty<GeneratedSourceResult>(), outputCompilation);
            }

            ImmutableArray<GeneratedSourceResult> generatedSources = runResult
                .Results[0]
                .GeneratedSources;
            GeneratedSourceResult[] results = new GeneratedSourceResult[generatedSources.Length];
            generatedSources.CopyTo(results);
            return new GeneratorOutput(results, outputCompilation);
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

        private static Assembly EmitAssembly(Compilation compilation)
        {
            using MemoryStream stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream);
            ThrowIfCompilationHasErrors(result.Diagnostics);
            stream.Position = 0;
            return Assembly.Load(stream.ToArray());
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

        private const string PlayerApiSource =
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
";

        private const string EnumApiSource =
            @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial class EnumApi
    {
        [LuaMember]
        public SignedMode Mode { get; set; }

        [LuaMember]
        public UnsignedMask Mask { get; set; }
    }

    public enum SignedMode : long
    {
        Negative = -1,
        Min = long.MinValue,
    }

    public enum UnsignedMask : ulong
    {
        Low = 4UL,
        High = 9223372036854775808UL,
    }
}
";

        private const string DuplicateEnumApiSource =
            @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial class CollisionApi
    {
        [LuaMember]
        public First.Team FirstTeam { get; set; }

        [LuaMember]
        public Second.Team SecondTeam { get; set; }
    }
}

namespace Fixtures.First
{
    public enum Team
    {
        Red = 1,
    }
}

namespace Fixtures.Second
{
    public enum Team
    {
        Blue = 2,
    }
}
";

        private const string MemberCollisionEnumApiSource =
            @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial class MemberCollisionApi
    {
        [LuaMember]
        public int Team { get; set; }

        [LuaMember]
        public Team Affiliation { get; set; }
    }

    public enum Team
    {
        Red = 1,
    }
}
";

        private const string IgnoredEnumApiSource =
            @"
using NovaSharp;

/*fixture*/ namespace Fixtures
{
    [LuaObject]
    public partial class IgnoreApi
    {
        [LuaMember]
        public IgnoredMode Ignored { get; set; }

        [LuaMember]
        public VisibleMode Visible { get; set; }
    }

    [LuaIgnore]
    public enum IgnoredMode
    {
        Hidden = 1,
    }

    public enum VisibleMode
    {
        Shown = 1,

        [LuaIgnore]
        Hidden = 2,
    }
}
";

        private sealed class GeneratorOutput
        {
            public GeneratorOutput(
                GeneratedSourceResult[] generatedSources,
                Compilation compilation
            )
            {
                GeneratedSources = generatedSources;
                Compilation = compilation;
            }

            public GeneratedSourceResult[] GeneratedSources { get; }

            public Compilation Compilation { get; }
        }
    }
}
