namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    internal static class LuaVersionData
    {
        private static readonly LuaCompatibilityVersion[] AllVersionValues =
        {
            LuaCompatibilityVersion.Lua51,
            LuaCompatibilityVersion.Lua52,
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
        };

        private static readonly IReadOnlyList<LuaCompatibilityVersion> AllVersionsReadOnly =
            Array.AsReadOnly(AllVersionValues);

        internal static IReadOnlyList<LuaCompatibilityVersion> AllVersions
        {
            get { return AllVersionsReadOnly; }
        }

        internal static IReadOnlyList<LuaCompatibilityVersion> From(
            LuaCompatibilityVersion minimumVersion
        )
        {
            int normalizedMinimum = Normalize(minimumVersion);
            List<LuaCompatibilityVersion> versions = new List<LuaCompatibilityVersion>();

            foreach (LuaCompatibilityVersion version in AllVersionValues)
            {
                if ((int)version >= normalizedMinimum)
                {
                    versions.Add(version);
                }
            }

            if (versions.Count == 0)
            {
                throw new ArgumentException(
                    "No Lua compatibility versions match the requested minimum value.",
                    nameof(minimumVersion)
                );
            }

            return versions.ToArray();
        }

        internal static IReadOnlyList<LuaCompatibilityVersion> Until(
            LuaCompatibilityVersion maximumVersion
        )
        {
            int normalizedMaximum = Normalize(maximumVersion);
            List<LuaCompatibilityVersion> versions = new List<LuaCompatibilityVersion>();

            foreach (LuaCompatibilityVersion version in AllVersionValues)
            {
                if ((int)version <= normalizedMaximum)
                {
                    versions.Add(version);
                }
            }

            if (versions.Count == 0)
            {
                throw new ArgumentException(
                    "No Lua compatibility versions match the requested maximum value.",
                    nameof(maximumVersion)
                );
            }

            return versions.ToArray();
        }

        internal static IReadOnlyList<LuaCompatibilityVersion> Range(
            LuaCompatibilityVersion minimumVersion,
            LuaCompatibilityVersion maximumVersion
        )
        {
            int normalizedMinimum = Normalize(minimumVersion);
            int normalizedMaximum = Normalize(maximumVersion);

            if (normalizedMinimum > normalizedMaximum)
            {
                throw new ArgumentException(
                    "The minimum Lua version must be less than or equal to the maximum Lua version."
                );
            }

            List<LuaCompatibilityVersion> versions = new List<LuaCompatibilityVersion>();

            foreach (LuaCompatibilityVersion version in AllVersionValues)
            {
                int numericVersion = (int)version;

                if (numericVersion >= normalizedMinimum && numericVersion <= normalizedMaximum)
                {
                    versions.Add(version);
                }
            }

            if (versions.Count == 0)
            {
                throw new ArgumentException(
                    "The requested Lua version range does not include any supported versions."
                );
            }

            return versions.ToArray();
        }

        private static int Normalize(LuaCompatibilityVersion version)
        {
            if (version == LuaCompatibilityVersion.Latest)
            {
                return (int)LuaCompatibilityVersion.Lua55;
            }

            return (int)version;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public abstract class LuaVersionDataSourceAttributeBase : UntypedDataSourceGeneratorAttribute
    {
        private readonly IReadOnlyList<LuaCompatibilityVersion> _versions;

        protected LuaVersionDataSourceAttributeBase(IReadOnlyList<LuaCompatibilityVersion> versions)
        {
            if (versions == null)
            {
                throw new ArgumentNullException(nameof(versions));
            }

            if (versions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one Lua compatibility version must be specified.",
                    nameof(versions)
                );
            }

            _versions = versions;
        }

        protected override IEnumerable<Func<object[]>> GenerateDataSources()
        {
            foreach (LuaCompatibilityVersion version in _versions)
            {
                yield return () => new object[] { version };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AllLuaVersionsAttribute : LuaVersionDataSourceAttributeBase
    {
        public AllLuaVersionsAttribute()
            : base(LuaVersionData.AllVersions) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class LuaVersionsFromAttribute : LuaVersionDataSourceAttributeBase
    {
        public LuaVersionsFromAttribute(LuaCompatibilityVersion minimumVersion)
            : base(LuaVersionData.From(minimumVersion)) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class LuaVersionsUntilAttribute : LuaVersionDataSourceAttributeBase
    {
        public LuaVersionsUntilAttribute(LuaCompatibilityVersion maximumVersion)
            : base(LuaVersionData.Until(maximumVersion)) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class LuaVersionRangeAttribute : LuaVersionDataSourceAttributeBase
    {
        public LuaVersionRangeAttribute(
            LuaCompatibilityVersion minimumVersion,
            LuaCompatibilityVersion maximumVersion
        )
            : base(LuaVersionData.Range(minimumVersion, maximumVersion)) { }
    }
}
