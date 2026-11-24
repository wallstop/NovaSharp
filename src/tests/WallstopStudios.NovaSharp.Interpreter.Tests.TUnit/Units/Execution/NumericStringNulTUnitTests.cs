namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// Verifies version-specific NUL handling in Lua numeric coercions.
    /// </summary>
    public sealed class NumericStringNulTUnitTests
    {
        /// <summary>Exercises numeric coercion with valid, leading, embedded, and trailing NUL inputs.</summary>
        [Test]
        [AllLuaVersions]
        public async Task NumericStringNulFollowsCompatibilityProfile(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            bool stopsAtNul = version == LuaCompatibilityVersion.Lua51;

            (string Label, string Text, bool Accepts)[] numerals =
            {
                ("valid", "17", true),
                ("trailing NUL", "17\0", stopsAtNul),
                ("embedded NUL", "17\08", stopsAtNul),
                ("leading NUL", "\017", false),
            };

            (string Body, double Expected)[] protectedOperations =
            {
                ("return -numericText", -17),
                ("return numericText - 2", 15),
                ("return 2 - numericText", -15),
                ("return tonumber('10', numericText)", 17),
                ("for i = numericText, 32 do return i end", 17),
                ("for i = 1, numericText do return i end", 1),
                ("for i = 1, 32, numericText do return i end", 1),
            };

            (string Expression, double Expected)[] conversions =
            {
                ("tonumber(numericText)", 17),
                ("tonumber(numericText, 10)", 17),
                ("tonumber(numericText, 16)", 23),
            };

            foreach ((string label, string text, bool accepts) in numerals)
            {
                script.Globals.Set("numericText", LuaValue.NewString(text));
                foreach ((string body, double expected) in protectedOperations)
                {
                    LuaValue result = Execute($"return pcall(function() {body} end)");
                    await Assert
                        .That(result.Tuple[0].Boolean)
                        .IsEqualTo(accepts)
                        .Because($"{version}, {label}, {body}");

                    if (accepts)
                    {
                        await Assert.That(result.Tuple[1].Number).IsEqualTo(expected);
                    }
                }

                foreach ((string expression, double expected) in conversions)
                {
                    LuaValue result = Execute($"return {expression}");
                    await Assert
                        .That(result.IsNil)
                        .IsEqualTo(!accepts)
                        .Because($"{version}, {label}, {expression}");

                    if (accepts)
                    {
                        await Assert.That(result.Number).IsEqualTo(expected);
                    }
                }

                if (version >= LuaCompatibilityVersion.Lua53)
                {
                    LuaValue integer = Execute("return math.tointeger(numericText)");
                    await Assert
                        .That(integer.IsNil)
                        .IsEqualTo(!accepts)
                        .Because($"{version}, {label}, math.tointeger");

                    if (accepts)
                    {
                        await Assert.That(integer.Number).IsEqualTo(17);
                    }
                    else
                    {
                        foreach (string bitwise in new[] { "~numericText", "numericText & 3" })
                        {
                            LuaValue result = Execute(
                                $"return pcall(function() return {bitwise} end)"
                            );
                            await Assert
                                .That(result.Tuple[0].Boolean)
                                .IsFalse()
                                .Because($"{version}, {label}, {bitwise}");
                        }
                    }
                }
            }

            foreach (
                (string text, double expected) in new[] { ("0x11\0bad", 17d), ("1.75\0bad", 1.75) }
            )
            {
                script.Globals.Set("numericText", LuaValue.NewString(text));
                LuaValue result = Execute("return tonumber(numericText)");
                await Assert.That(result.IsNil).IsEqualTo(!stopsAtNul);
                if (stopsAtNul)
                {
                    await Assert.That(result.Number).IsEqualTo(expected);
                }
            }

            LuaValue Execute(string luaChunk) => script.DoString(luaChunk);
        }
    }
}
