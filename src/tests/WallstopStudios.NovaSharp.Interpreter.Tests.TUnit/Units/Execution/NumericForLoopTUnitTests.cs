namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Numeric <c>for</c> loop semantics verified against reference Lua 5.1-5.5, including
    /// zero-crossing ranges, integer boundaries, float ranges, and zero steps.
    /// </summary>
    public sealed class NumericForLoopTUnitTests
    {
        private static readonly LuaCompatibilityVersion[] AllVersions =
        {
            LuaCompatibilityVersion.Lua51,
            LuaCompatibilityVersion.Lua52,
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
        };

        private static string RunLoop(LuaCompatibilityVersion version, string range)
        {
            Script script = new(version);
            // The unresolved {range} placeholder makes the corpus extractor mark its
            // derived Unknown.lua snippet NovaSharp-only, keeping the comparable corpus
            // free of a partial helper body.
            LuaValue result = script.DoString(
                $"local t = {{}} for i = {range} do t[#t + 1] = i end return table.concat(t, ',')"
            );
            return result.String;
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetZeroCrossingData))]
        public async Task ZeroCrossingLoopsIterateEveryValue(
            LuaCompatibilityVersion version,
            string range,
            string expected
        )
        {
            await Assert.That(RunLoop(version, range)).IsEqualTo(expected).ConfigureAwait(false);
        }

        /// <summary>
        /// Reference Lua 5.1-5.5 all iterate the full zero-crossing range; NovaSharp used to
        /// exit before the first iteration because of a sign-only overflow heuristic.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, string)> GetZeroCrossingData()
        {
            (string range, string expected)[] cases =
            {
                ("-2, 2", "-2,-1,0,1,2"),
                ("2, -2, -1", "2,1,0,-1,-2"),
                ("-3, 3, 2", "-3,-1,1,3"),
                ("3, -3, -2", "3,1,-1,-3"),
                ("-1, 1", "-1,0,1"),
                ("1, -1, -1", "1,0,-1"),
                ("-10, 10, 5", "-10,-5,0,5,10"),
                ("10, -10, -5", "10,5,0,-5,-10"),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach ((string range, string expected) in cases)
                {
                    yield return (version, range, expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetStandardRangeData))]
        public async Task StandardRangesIterateLikeReference(
            LuaCompatibilityVersion version,
            string range,
            string expected
        )
        {
            await Assert.That(RunLoop(version, range)).IsEqualTo(expected).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, string)> GetStandardRangeData()
        {
            (string range, string expected)[] cases =
            {
                ("1, 3", "1,2,3"),
                ("3, 1, -1", "3,2,1"),
                ("5, 3", ""),
                ("1, 5, 2", "1,3,5"),
                ("5, 1, -2", "5,3,1"),
                ("1, 1", "1"),
                ("1, 0", ""),
                ("1, 1, -1", "1"),
                ("-3, -1", "-3,-2,-1"),
                ("-1, -3, -1", "-1,-2,-3"),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach ((string range, string expected) in cases)
                {
                    yield return (version, range, expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloatLoopWithFloatInitIteratesFractionalValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local n = 0
                  local last
                  for i = 1.5, 3 do n = n + 1 last = i end
                  return n, last"
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2.5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloatLoopWithFractionalLimitStopsAtWholeBound(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local n = 0
                  local last
                  for i = 3, 1.5, -1 do n = n + 1 last = i end
                  return n, last"
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloatLoopWithFractionalStepCountsEveryHalf(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local n = 0
                  for i = 1, 3, 0.5 do n = n + 1 end
                  return n"
            );

            await Assert.That(result.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerBoundaryLoopsNeverWrapTheControlVariable(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue ascending = script.DoString(
                @"local t = {}
                  for i = math.maxinteger - 2, math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );
            LuaValue descending = script.DoString(
                @"local t = {}
                  for i = math.mininteger + 2, math.mininteger, -1 do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );

            await Assert
                .That(ascending.String)
                .IsEqualTo("9223372036854775805,9223372036854775806,9223372036854775807")
                .ConfigureAwait(false);
            await Assert
                .That(descending.String)
                .IsEqualTo("-9223372036854775806,-9223372036854775807,-9223372036854775808")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task BoundaryLoopsFromExtremesIterateFullRange(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue fromMin = script.DoString(
                @"local t = {}
                  for i = math.mininteger, math.mininteger + 3 do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );

            await Assert
                .That(fromMin.String)
                .IsEqualTo(
                    "-9223372036854775808,-9223372036854775807,-9223372036854775806,-9223372036854775805"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task BoundaryLoopsWithMaximalStepsStopBeforeWrapping(
            LuaCompatibilityVersion version
        )
        {
            // Reference Lua 5.4/5.5: 0, maxinteger (two iterations). Lua 5.3.6 loops forever
            // here; NovaSharp follows the corrected 5.4 counter semantics.
            Script script = new(version);
            LuaValue ascending = script.DoString(
                @"local t = {}
                  for i = 0, math.maxinteger, math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );
            LuaValue descending = script.DoString(
                @"local t = {}
                  for i = math.maxinteger, 0, -math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );
            LuaValue descendingToMin = script.DoString(
                @"local t = {}
                  for i = math.maxinteger, math.mininteger, -math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );

            await Assert
                .That(ascending.String)
                .IsEqualTo("0,9223372036854775807")
                .ConfigureAwait(false);
            await Assert
                .That(descending.String)
                .IsEqualTo("9223372036854775807,0")
                .ConfigureAwait(false);
            await Assert
                .That(descendingToMin.String)
                .IsEqualTo("9223372036854775807,0,-9223372036854775807")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task OutOfRangeFloatLimitClampsToIntegerBoundary(
            LuaCompatibilityVersion version
        )
        {
            // Reference Lua 5.4/5.5 clamp a positive non-integral-representable limit to
            // maxinteger, so the loop visits 0 then maxinteger and stops.
            Script script = new(version);
            LuaValue ascending = script.DoString(
                @"local t = {}
                  for i = 0, 2e63, math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')"
            );

            await Assert
                .That(ascending.String)
                .IsEqualTo("0,9223372036854775807")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task ZeroStepRunsZeroIterationsBeforeLua54(LuaCompatibilityVersion version)
        {
            // Reference Lua 5.1-5.3 run zero iterations for an ascending zero step; a
            // descending zero step loops forever there, so NovaSharp terminates both.
            Script script = new(version);
            LuaValue iterations = script.DoString(
                @"local n = 0
                  for i = 1, 10, 0 do n = n + 1 end
                  for i = 1, 10, 0.0 do n = n + 1 end
                  return n"
            );

            await Assert.That(iterations.Number).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ZeroStepErrorsFromLua54(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            await Assert
                .That(() =>
                    script.DoString(
                        @"local n = 0
                          for i = 1, 10, 0 do n = n + 1 end
                          return n"
                    )
                )
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ZeroStepErrorMatchesReferenceMessage(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            ScriptRuntimeException captured = null;
            try
            {
                script.DoString("for i = 1, 10, 0 do end");
            }
            catch (ScriptRuntimeException ex)
            {
                captured = ex;
            }

            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(captured.Message)
                .Contains("'for' step is zero")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua54)]
        public async Task MutatingControlVariableDoesNotChangeIterationCount(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.5 makes the control variable const, so mutating it is a 5.5 error
            // rather than a tolerated no-op on the internal counter.
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local t = {}
                  for i = -2, 2 do t[#t + 1] = i i = i + 100 end
                  return table.concat(t, ',')"
            );

            await Assert.That(result.String).IsEqualTo("-2,-1,0,1,2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ControlVariableIsOutOfScopeAfterLoop(LuaCompatibilityVersion version)
        {
            // Reference Lua scopes the loop variable to the loop body in every version.
            Script script = new(version);
            LuaValue result = script.DoString("for i = -2, 2 do end return i");

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ZeroCrossingLoopSurvivesCoroutineSuspension(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local co = coroutine.create(function()
                      local t = {}
                      for i = -2, 2 do
                          coroutine.yield(i)
                          t[#t + 1] = i
                      end
                      return table.concat(t, ',')
                  end)
                  local out = {}
                  repeat
                      local ok, v = coroutine.resume(co)
                      out[#out + 1] = tostring(v)
                  until coroutine.status(co) == 'dead'
                  return table.concat(out, ',')"
            );

            await Assert
                .That(result.String)
                .IsEqualTo("-2,-1,0,1,2,-2,-1,0,1,2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ZeroCrossingLoopRoundTripsThroughBinaryDump(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.LoadString(
                "local t = {} for i = -2, 2 do t[#t + 1] = i end return table.concat(t, ',')"
            );

            using MemoryStream stream = new();
            script.Dump(function, stream);
            stream.Position = 0;
            LuaValue loaded = script.LoadStream(stream);
            LuaValue result = script.Call(loaded);

            await Assert.That(result.String).IsEqualTo("-2,-1,0,1,2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SkippedLoopRoundTripsThroughBinaryDump(LuaCompatibilityVersion version)
        {
            // ForPrep carries the loop's skip target in NumVal; the dump must preserve it
            // so a dumped skipped loop still resumes past the loop instead of address zero.
            Script script = new(version);
            LuaValue function = script.LoadString(
                "local t = {} for i = 5, 3 do t[#t + 1] = i end t[#t + 1] = 99 "
                    + "return table.concat(t, ',')"
            );

            using MemoryStream stream = new();
            script.Dump(function, stream);
            stream.Position = 0;
            LuaValue loaded = script.LoadStream(stream);
            LuaValue result = script.Call(loaded);

            await Assert.That(result.String).IsEqualTo("99").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetNanBoundTruthTableData))]
        public async Task NonFiniteBoundsMatchReferenceIterations(
            LuaCompatibilityVersion version,
            string range,
            string expected
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "local n = 0 for i = "
                    + range
                    + " do n = n + 1 if n > 3 then break end end return n > 3 and 'MANY' or tostring(n)"
            );

            await Assert.That(result.String).IsEqualTo(expected).ConfigureAwait(false);
        }

        /// <summary>
        /// Reference-verified matrix for NaN and infinite bounds. Lua 5.4+ route a float
        /// loop's first decision through a comparison that never rejects NaN, so such loops
        /// run one iteration; Lua 5.1-5.3 compute the entry index as (init - step) + step,
        /// which NaN poisons, so they never start. Integer loops under Lua 5.3+ clamp a NaN
        /// descending limit to mininteger.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            string,
            string
        )> GetNanBoundTruthTableData()
        {
            (LuaCompatibilityVersion version, string expected)[] byVersion =
            {
                (LuaCompatibilityVersion.Lua51, "0"),
                (LuaCompatibilityVersion.Lua52, "0"),
                (LuaCompatibilityVersion.Lua53, "MANY"),
                (LuaCompatibilityVersion.Lua54, "MANY"),
                (LuaCompatibilityVersion.Lua55, "MANY"),
            };

            foreach ((LuaCompatibilityVersion version, string expected) in byVersion)
            {
                yield return (version, "5,0/0,-1", expected);
            }

            // Float loops with NaN bounds: one iteration on 5.4+, none before.
            (LuaCompatibilityVersion version, string expected)[] floatNanByVersion =
            {
                (LuaCompatibilityVersion.Lua51, "0"),
                (LuaCompatibilityVersion.Lua52, "0"),
                (LuaCompatibilityVersion.Lua53, "0"),
                (LuaCompatibilityVersion.Lua54, "1"),
                (LuaCompatibilityVersion.Lua55, "1"),
            };

            foreach ((LuaCompatibilityVersion version, string expected) in floatNanByVersion)
            {
                yield return (version, "5.5,0/0,-1", expected);
                yield return (version, "10,1,0/0", expected);
                yield return (version, "1,0/0,1.0", expected);
            }

            // Ascending toward NaN never runs; descending toward infinity never runs.
            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                yield return (version, "5,0/0", "0");
                yield return (version, "5,2e400,-1", "0");
                yield return (version, "5.5,2e400,-1", "0");
            }
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloatLoopControlsAreFloatFromFirstIteration(
            LuaCompatibilityVersion version
        )
        {
            // Reference Lua stores a float loop's controls as floats, so even an
            // integer-subtype initial value observes the float subtype in the body.
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local t = {}
                  for i = 1, 3, 1.0 do t[#t + 1] = math.type(i) end
                  return table.concat(t, ',')"
            );

            await Assert.That(result.String).IsEqualTo("float,float,float").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task FloatLoopControlsKeepIntegralFormattingBeforeLua53(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.1/5.2 have one number type: an integral float limit with integer
            // controls still renders whole loop values without a decimal point.
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local t = {}
                  for i = 1, 3.0 do t[#t + 1] = tostring(i) end
                  return table.concat(t, ',')"
            );

            await Assert.That(result.String).IsEqualTo("1,2,3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ZeroStepReportedBeforeInvalidLimitFromLua54(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.4+ reject a zero integer step before validating the limit.
            Script script = new(version);
            ScriptRuntimeException captured = null;
            try
            {
                script.DoString("for i = 1, {}, 0 do end");
            }
            catch (ScriptRuntimeException ex)
            {
                captured = ex;
            }

            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(captured.Message)
                .Contains("'for' step is zero")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task InvalidLimitReportedBeforeZeroStepThroughLua53(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.1-5.3 validate the limit before considering the (tolerated) zero step.
            Script script = new(version);
            ScriptRuntimeException captured = null;
            try
            {
                script.DoString("for i = 1, {}, 0 do end");
            }
            catch (ScriptRuntimeException ex)
            {
                captured = ex;
            }

            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(captured.Message)
                .Contains("'for' limit must be a number")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task NonNumberControlErrorsUseLua54MessageFormat(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            await Assert
                .That(() => script.DoString("for i = 1, {}, 1 do end"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);

            ScriptRuntimeException captured = null;
            try
            {
                script.DoString("for i = 1, {}, 1 do end");
            }
            catch (ScriptRuntimeException ex)
            {
                captured = ex;
            }

            await Assert
                .That(captured.Message)
                .IsEqualTo("bad 'for' limit (number expected, got table)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GotoOutOfNumericLoopDoesNotLeakControlSlots(
            LuaCompatibilityVersion version
        )
        {
            // Each backward goto out of the loop must pop the control triple; before the
            // fix this leaked three value-stack slots per jump until the stack overflowed.
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local n = 0
                  ::top::
                  for i = 1, 2 do
                      if n < 100000 then
                          n = n + 1
                          goto top
                      end
                  end
                  return n"
            );

            await Assert.That(result.Number).IsEqualTo(100000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoOutOfGenericLoopDoesNotLeakIteratorSlot(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"local n = 0
                  local t = { 1, 2, 3 }
                  ::top::
                  for k, v in pairs(t) do
                      if n < 50000 then
                          n = n + 1
                          goto top
                      end
                  end
                  return n"
            );

            await Assert.That(result.Number).IsEqualTo(50000).ConfigureAwait(false);
        }
    }
}
