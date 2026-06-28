namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit
{
    using System;
    using System.Collections.Generic;
    using global::TUnit.Core;
    using global::TUnit.Core.Interfaces;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class LuaTestMatrixAttribute : UntypedDataSourceGeneratorAttribute
    {
        private readonly object[][] _argumentSets;

        public LuaTestMatrixAttribute(params object[] argumentSets)
        {
            ArgumentNullException.ThrowIfNull(argumentSets);

            _argumentSets = NormalizeArgumentSets(argumentSets);
            ArgumentSets = argumentSets;
            MinimumVersion = LuaCompatibilityVersion.Lua51;
            MaximumVersion = LuaCompatibilityVersion.Lua55;
        }

        public object[] ArgumentSets { get; }

        public LuaCompatibilityVersion MinimumVersion { get; set; }

        public LuaCompatibilityVersion MaximumVersion { get; set; }

        protected override IEnumerable<Func<object[]>> GenerateDataSources(
            DataGeneratorMetadata dataGeneratorMetadata
        )
        {
            IReadOnlyList<LuaCompatibilityVersion> versions = LuaVersionData.Range(
                MinimumVersion,
                MaximumVersion
            );

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach (object[] argumentSet in _argumentSets)
                {
                    yield return () => BuildDataRow(version, argumentSet);
                }
            }
        }

        private static object[][] NormalizeArgumentSets(object[] argumentSets)
        {
            if (argumentSets.Length == 0)
            {
                throw new ArgumentException(
                    "At least one argument value must be provided for matrix expansion.",
                    nameof(argumentSets)
                );
            }

            List<object[]> normalizedSets = new List<object[]>();

            foreach (object argument in argumentSets)
            {
                object[] nestedSet = argument as object[];

                if (nestedSet != null)
                {
                    if (nestedSet.Length == 0)
                    {
                        throw new ArgumentException(
                            "Nested argument sets cannot be empty.",
                            nameof(argumentSets)
                        );
                    }

                    normalizedSets.Add(CopyArguments(nestedSet));
                }
                else
                {
                    normalizedSets.Add(new object[] { argument });
                }
            }

            return normalizedSets.ToArray();
        }

        private static object[] CopyArguments(object[] source)
        {
            object[] copy = new object[source.Length];

            for (int index = 0; index < source.Length; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        private static object[] BuildDataRow(LuaCompatibilityVersion version, object[] argumentSet)
        {
            object[] dataRow = new object[argumentSet.Length + 1];
            dataRow[0] = version;

            for (int index = 0; index < argumentSet.Length; index++)
            {
                dataRow[index + 1] = argumentSet[index];
            }

            return dataRow;
        }
    }
}
