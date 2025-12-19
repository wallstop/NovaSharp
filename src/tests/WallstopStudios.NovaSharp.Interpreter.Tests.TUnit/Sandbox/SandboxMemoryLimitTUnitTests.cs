namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Tests for <see cref="AllocationTracker"/> and memory limit sandbox features.
    /// </summary>
    public sealed class SandboxMemoryLimitTUnitTests
    {
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsAllocations(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);

            await Assert.That(tracker.CurrentBytes).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(tracker.TotalAllocated).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(tracker.PeakBytes).IsEqualTo(1024).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsMultipleAllocations(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordAllocation(2048);

            await Assert.That(tracker.CurrentBytes).IsEqualTo(3072).ConfigureAwait(false);
            await Assert.That(tracker.TotalAllocated).IsEqualTo(3072).ConfigureAwait(false);
            await Assert.That(tracker.PeakBytes).IsEqualTo(3072).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsDeallocations(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordDeallocation(512);

            await Assert.That(tracker.CurrentBytes).IsEqualTo(512).ConfigureAwait(false);
            await Assert.That(tracker.TotalAllocated).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(tracker.TotalFreed).IsEqualTo(512).ConfigureAwait(false);
            await Assert.That(tracker.PeakBytes).IsEqualTo(1024).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerTracksPeakMemory(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordAllocation(2048); // Peak = 3072
            tracker.RecordDeallocation(2048);
            tracker.RecordAllocation(512); // Current = 1536, Peak still = 3072

            await Assert.That(tracker.CurrentBytes).IsEqualTo(1536).ConfigureAwait(false);
            await Assert.That(tracker.PeakBytes).IsEqualTo(3072).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerResetClearsAllCounters(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordDeallocation(512);
            tracker.Reset();

            await Assert.That(tracker.CurrentBytes).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.PeakBytes).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.TotalAllocated).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.TotalFreed).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsLimitReturnsTrueWhenOverLimit(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);

            await Assert.That(tracker.ExceedsLimit(512)).IsTrue().ConfigureAwait(false);
            await Assert.That(tracker.ExceedsLimit(1024)).IsFalse().ConfigureAwait(false);
            await Assert.That(tracker.ExceedsLimit(2048)).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsLimitReturnsFalseForUnlimited(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(long.MaxValue / 2);

            await Assert.That(tracker.ExceedsLimit(0)).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsLimitWithSandboxOptions(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();
            SandboxOptions options = new SandboxOptions { MaxMemoryBytes = 1024 };

            tracker.RecordAllocation(2048);

            await Assert.That(tracker.ExceedsLimit(options)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsLimitWithNullOptions(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(long.MaxValue / 2);

            await Assert
                .That(tracker.ExceedsLimit((SandboxOptions)null))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsLimitWithUnrestrictedOptions(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(long.MaxValue / 2);

            await Assert
                .That(tracker.ExceedsLimit(SandboxOptions.Unrestricted))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerZeroAllocationIsNoOp(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(0);

            await Assert.That(tracker.CurrentBytes).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.TotalAllocated).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerZeroDeallocationIsNoOp(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordDeallocation(0);

            await Assert.That(tracker.CurrentBytes).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(tracker.TotalFreed).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerNegativeAllocationThrows(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            await Assert
                .That(() => tracker.RecordAllocation(-1))
                .Throws<System.ArgumentOutOfRangeException>()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerNegativeDeallocationThrows(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            await Assert
                .That(() => tracker.RecordDeallocation(-1))
                .Throws<System.ArgumentOutOfRangeException>()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationSnapshotCapturesState(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordAllocation(1024);
            tracker.RecordDeallocation(256);

            AllocationSnapshot snapshot = tracker.CreateSnapshot();

            await Assert.That(snapshot.CurrentBytes).IsEqualTo(768).ConfigureAwait(false);
            await Assert.That(snapshot.PeakBytes).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(snapshot.TotalAllocated).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(snapshot.TotalFreed).IsEqualTo(256).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationSnapshotToStringFormatsCorrectly(
            LuaCompatibilityVersion version
        )
        {
            AllocationSnapshot snapshot = new AllocationSnapshot(768, 1024, 1024, 256, 2, 3, 5);

            string result = snapshot.ToString();

            await Assert
                .That(result)
                .IsEqualTo(
                    "AllocationSnapshot(Current=768, Peak=1024, Allocated=1024, Freed=256, Coroutines=2, PeakCoroutines=3, TotalCoroutines=5)"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsHasMemoryLimitReturnsTrueWhenSet(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions { MaxMemoryBytes = 1024 };

            await Assert.That(options.HasMemoryLimit).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsHasMemoryLimitReturnsFalseWhenZero(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions { MaxMemoryBytes = 0 };

            await Assert.That(options.HasMemoryLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsHasMemoryLimitReturnsFalseByDefault(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions();

            await Assert.That(options.HasMemoryLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsMaxMemoryBytesNormalizesNegativeToZero(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions { MaxMemoryBytes = -100 };

            await Assert.That(options.MaxMemoryBytes).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(options.HasMemoryLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsCopyConstructorCopiesMemorySettings(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions original = new SandboxOptions
            {
                MaxMemoryBytes = 1024,
                OnMemoryLimitExceeded = (script, bytes) => true,
            };

            SandboxOptions copy = new SandboxOptions(original);

            await Assert.That(copy.MaxMemoryBytes).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(copy.OnMemoryLimitExceeded).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationExceptionMemoryLimitExceededFormatsMessage(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationException ex = new SandboxViolationException(
                SandboxViolationType.MemoryLimitExceeded,
                1024,
                2048
            );

            await Assert
                .That(ex.Message)
                .IsEqualTo(
                    "Sandbox violation: memory limit exceeded (limit: 1024 bytes, used: 2048 bytes)"
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.MemoryLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(ex.ConfiguredLimit).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(ex.ActualValue).IsEqualTo(2048).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationTypeIncludesMemoryLimitExceeded(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationType[] allTypes = System.Enum.GetValues<SandboxViolationType>();

            await Assert
                .That(allTypes)
                .Contains(SandboxViolationType.MemoryLimitExceeded)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsInstructionLimitCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.InstructionLimit(1000, 1500);

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(details.LimitValue).IsEqualTo(1000).ConfigureAwait(false);
            await Assert.That(details.ActualValue).IsEqualTo(1500).ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsFalse().ConfigureAwait(false);
            await Assert.That(details.AccessName).IsNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsRecursionLimitCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.RecursionLimit(100, 150);

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.RecursionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(details.LimitValue).IsEqualTo(100).ConfigureAwait(false);
            await Assert.That(details.ActualValue).IsEqualTo(150).ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsMemoryLimitCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.MemoryLimit(1024, 2048);

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.MemoryLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(details.LimitValue).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(details.ActualValue).IsEqualTo(2048).ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsModuleAccessDeniedCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.ModuleAccessDenied("os");

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.ModuleAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(details.AccessName).IsEqualTo("os").ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsFalse().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsTrue().ConfigureAwait(false);
            await Assert.That(details.LimitValue).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(details.ActualValue).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsFunctionAccessDeniedCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.FunctionAccessDenied(
                "loadfile"
            );

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.FunctionAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(details.AccessName).IsEqualTo("loadfile").ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsFalse().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsFormatMessageReturnsCorrectString(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails memoryDetails = SandboxViolationDetails.MemoryLimit(1024, 2048);

            string message = memoryDetails.FormatMessage();

            await Assert
                .That(message)
                .IsEqualTo(
                    "Sandbox violation: memory limit exceeded (limit: 1024 bytes, used: 2048 bytes)"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsToStringReturnsFormattedMessage(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.ModuleAccessDenied("io");

            string result = details.ToString();

            await Assert
                .That(result)
                .IsEqualTo("Sandbox violation: access to module 'io' is denied")
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsEqualityWorksCorrectly(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details1 = SandboxViolationDetails.MemoryLimit(1024, 2048);
            SandboxViolationDetails details2 = SandboxViolationDetails.MemoryLimit(1024, 2048);
            SandboxViolationDetails details3 = SandboxViolationDetails.MemoryLimit(1024, 4096);

            await Assert.That(details1 == details2).IsTrue().ConfigureAwait(false);
            await Assert.That(details1 != details3).IsTrue().ConfigureAwait(false);
            await Assert.That(details1.Equals(details2)).IsTrue().ConfigureAwait(false);
            await Assert.That(details1.Equals((object)details2)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsGetHashCodeIsConsistent(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details1 = SandboxViolationDetails.InstructionLimit(100, 200);
            SandboxViolationDetails details2 = SandboxViolationDetails.InstructionLimit(100, 200);

            await Assert
                .That(details1.GetHashCode())
                .IsEqualTo(details2.GetHashCode())
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationExceptionWithDetailsConstructorSetsProperties(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.MemoryLimit(1024, 2048);

            SandboxViolationException ex = new SandboxViolationException(details);

            await Assert.That(ex.Details).IsEqualTo(details).ConfigureAwait(false);
            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.MemoryLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(ex.ConfiguredLimit).IsEqualTo(1024).ConfigureAwait(false);
            await Assert.That(ex.ActualValue).IsEqualTo(2048).ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .IsEqualTo(
                    "Sandbox violation: memory limit exceeded (limit: 1024 bytes, used: 2048 bytes)"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationExceptionDetailsPropertyExposesStructuredData(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationException ex = new SandboxViolationException(
                SandboxViolationType.InstructionLimitExceeded,
                1000,
                1500
            );

            await Assert.That(ex.Details.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(ex.Details.IsAccessDenial).IsFalse().ConfigureAwait(false);
            await Assert
                .That(ex.Details.Kind)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationExceptionAccessDenialDetailsPropertyExposesStructuredData(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationException ex = new SandboxViolationException(
                SandboxViolationType.FunctionAccessDenied,
                "dofile"
            );

            await Assert.That(ex.Details.IsLimitViolation).IsFalse().ConfigureAwait(false);
            await Assert.That(ex.Details.IsAccessDenial).IsTrue().ConfigureAwait(false);
            await Assert.That(ex.Details.AccessName).IsEqualTo("dofile").ConfigureAwait(false);
            await Assert.That(ex.DeniedAccessName).IsEqualTo("dofile").ConfigureAwait(false);
        }

        // =====================================================
        // Script-level memory tracking integration tests
        // =====================================================

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptWithMemoryLimitHasAllocationTracker(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 }, // 1 MB
            };
            Script script = new Script(options);

            await Assert.That(script.AllocationTracker).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptWithoutMemoryLimitHasNoAllocationTracker(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);

            await Assert.That(script.AllocationTracker).IsNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsTableCreation(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            // Record initial allocation from script construction (registry, globals)
            long initialBytes = script.AllocationTracker.CurrentBytes;
            await Assert.That(initialBytes).IsGreaterThan(0).ConfigureAwait(false);

            // Create a table via Lua
            script.DoString("t = {}");

            // Should have more bytes allocated
            await Assert
                .That(script.AllocationTracker.CurrentBytes)
                .IsGreaterThan(initialBytes)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsTableEntries(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            // Create an empty table
            script.DoString("t = {}");
            long afterEmptyTable = script.AllocationTracker.CurrentBytes;

            // Add entries to the table
            script.DoString("for i = 1, 10 do t[i] = i end");
            long afterEntries = script.AllocationTracker.CurrentBytes;

            // Should have more bytes allocated after adding entries
            await Assert.That(afterEntries).IsGreaterThan(afterEmptyTable).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptMemoryLimitThrowsWhenExceeded(LuaCompatibilityVersion version)
        {
            // Very small memory limit to trigger violation quickly
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 4096 }, // 4 KB - very small
            };
            Script script = new Script(options);

            // Try to create many tables which should exceed the limit
            SandboxViolationException exception = await Assert
                .ThrowsAsync<SandboxViolationException>(async () =>
                {
                    await Task.Run(() =>
                        {
                            // This loop creates many tables, which should eventually exceed 4KB
                            script.DoString(
                                @"
                            local tables = {}
                            for i = 1, 1000 do
                                tables[i] = { a = i, b = i * 2, c = i * 3 }
                            end
                        "
                            );
                        })
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            await Assert
                .That(exception.ViolationType)
                .IsEqualTo(SandboxViolationType.MemoryLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(exception.ConfiguredLimit).IsEqualTo(4096).ConfigureAwait(false);
            await Assert.That(exception.ActualValue).IsGreaterThan(4096).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptMemoryLimitCallbackCanAllowContinuation(
            LuaCompatibilityVersion version
        )
        {
            bool callbackInvoked = false;
            long reportedMemory = 0;

            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions
                {
                    MaxMemoryBytes = 4096,
                    OnMemoryLimitExceeded = (script, currentMemory) =>
                    {
                        callbackInvoked = true;
                        reportedMemory = currentMemory;
                        // Return true to allow continuation (e.g., after GC or clearing caches)
                        return true;
                    },
                },
            };
            Script script = new Script(options);

            // This should trigger the callback but not throw
            script.DoString(
                @"
                local tables = {}
                for i = 1, 1000 do
                    tables[i] = { a = i, b = i * 2, c = i * 3 }
                end
            "
            );

            await Assert.That(callbackInvoked).IsTrue().ConfigureAwait(false);
            await Assert.That(reportedMemory).IsGreaterThan(4096).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerPeakBytesTracksHighWaterMark(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            // Create tables, record peak
            script.DoString(
                @"
                local t = {}
                for i = 1, 100 do
                    t[i] = { value = i }
                end
            "
            );

            long peakBytes = script.AllocationTracker.PeakBytes;

            // Peak should be recorded
            await Assert.That(peakBytes).IsGreaterThan(0).ConfigureAwait(false);
            await Assert
                .That(peakBytes)
                .IsGreaterThanOrEqualTo(script.AllocationTracker.CurrentBytes)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerSnapshotCapturesState(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            script.DoString("t = { a = 1, b = 2, c = 3 }");

            AllocationSnapshot snapshot = script.AllocationTracker.CreateSnapshot();

            // Snapshot should capture current state
            await Assert
                .That(snapshot.CurrentBytes)
                .IsEqualTo(script.AllocationTracker.CurrentBytes)
                .ConfigureAwait(false);
            await Assert
                .That(snapshot.PeakBytes)
                .IsEqualTo(script.AllocationTracker.PeakBytes)
                .ConfigureAwait(false);
            await Assert
                .That(snapshot.TotalAllocated)
                .IsEqualTo(script.AllocationTracker.TotalAllocated)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsClosureCreation(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeClosure = script.AllocationTracker.CurrentBytes;

            // Create a function (closure)
            script.DoString("function myFunc() return 42 end");

            long afterClosure = script.AllocationTracker.CurrentBytes;

            // Should have tracked the closure allocation
            await Assert.That(afterClosure).IsGreaterThan(beforeClosure).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsClosureWithUpValues(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeClosure = script.AllocationTracker.CurrentBytes;

            // Create a closure with upvalues
            script.DoString(
                @"
                local x = 10
                local y = 20
                local z = 30
                function closure()
                    return x + y + z
                end
            "
            );

            long afterClosure = script.AllocationTracker.CurrentBytes;

            // Closure with upvalues should use more memory
            await Assert.That(afterClosure).IsGreaterThan(beforeClosure).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsMultipleClosures(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeClosures = script.AllocationTracker.CurrentBytes;

            // Create multiple closures
            script.DoString(
                @"
                function f1() return 1 end
                function f2() return 2 end
                function f3() return 3 end
                function f4() return 4 end
                function f5() return 5 end
            "
            );

            long afterClosures = script.AllocationTracker.CurrentBytes;

            // Multiple closures should be tracked
            await Assert.That(afterClosures).IsGreaterThan(beforeClosures).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsCoroutineCreation(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeCoroutine = script.AllocationTracker.CurrentBytes;

            // Create a coroutine
            script.DoString(
                @"
                function myCoroutine()
                    coroutine.yield(1)
                    coroutine.yield(2)
                    return 3
                end
                co = coroutine.create(myCoroutine)
            "
            );

            long afterCoroutine = script.AllocationTracker.CurrentBytes;

            // Should have tracked the coroutine allocation
            await Assert.That(afterCoroutine).IsGreaterThan(beforeCoroutine).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsMultipleCoroutines(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeCoroutines = script.AllocationTracker.CurrentBytes;

            // Create multiple coroutines
            script.DoString(
                @"
                function gen()
                    coroutine.yield(1)
                end
                co1 = coroutine.create(gen)
                co2 = coroutine.create(gen)
                co3 = coroutine.create(gen)
            "
            );

            long afterCoroutines = script.AllocationTracker.CurrentBytes;

            // Multiple coroutines should be tracked
            await Assert
                .That(afterCoroutines)
                .IsGreaterThan(beforeCoroutines)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerCombinedTableClosureCoroutine(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script script = new Script(options);

            long beforeAll = script.AllocationTracker.CurrentBytes;

            // Create tables, closures, and coroutines
            script.DoString(
                @"
                -- Tables
                local t1 = { a = 1, b = 2 }
                local t2 = { x = 10, y = 20, z = 30 }
                
                -- Closures with upvalues
                local counter = 0
                function increment()
                    counter = counter + 1
                    return counter
                end
                
                -- Coroutine
                function producer()
                    for i = 1, 10 do
                        coroutine.yield(i)
                    end
                end
                co = coroutine.create(producer)
            "
            );

            long afterAll = script.AllocationTracker.CurrentBytes;

            // Combined allocations should be substantial
            await Assert.That(afterAll).IsGreaterThan(beforeAll).ConfigureAwait(false);
            // Should be at least table base (256) + closure base (128) + coroutine base (512)
            await Assert
                .That(afterAll - beforeAll)
                .IsGreaterThanOrEqualTo(256 + 128 + 512)
                .ConfigureAwait(false);
        }

        // ========================================================
        // Coroutine Limit Tests
        // ========================================================

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsCoroutineCreation(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();

            await Assert.That(tracker.CurrentCoroutines).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(tracker.PeakCoroutines).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(tracker.TotalCoroutinesCreated).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsMultipleCoroutineCreations(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();

            await Assert.That(tracker.CurrentCoroutines).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(tracker.PeakCoroutines).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(tracker.TotalCoroutinesCreated).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerRecordsCoroutineDisposal(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineDisposed();

            await Assert.That(tracker.CurrentCoroutines).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(tracker.PeakCoroutines).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(tracker.TotalCoroutinesCreated).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerTracksPeakCoroutines(LuaCompatibilityVersion version)
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated(); // Peak = 3
            tracker.RecordCoroutineDisposed();
            tracker.RecordCoroutineDisposed();
            tracker.RecordCoroutineCreated(); // Current = 2, Peak still = 3

            await Assert.That(tracker.CurrentCoroutines).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(tracker.PeakCoroutines).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerResetClearsCoroutineCounters(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();
            tracker.Reset();

            await Assert.That(tracker.CurrentCoroutines).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.PeakCoroutines).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tracker.TotalCoroutinesCreated).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsCoroutineLimitReturnsTrueWhenAtLimit(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();

            // At limit (2 >= 2)
            await Assert.That(tracker.ExceedsCoroutineLimit(2)).IsTrue().ConfigureAwait(false);
            // Under limit
            await Assert.That(tracker.ExceedsCoroutineLimit(3)).IsFalse().ConfigureAwait(false);
            // Over limit
            await Assert.That(tracker.ExceedsCoroutineLimit(1)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsCoroutineLimitReturnsFalseForUnlimited(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();

            await Assert.That(tracker.ExceedsCoroutineLimit(0)).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsCoroutineLimitWithSandboxOptions(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();
            SandboxOptions options = new SandboxOptions { MaxCoroutines = 2 };

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();

            await Assert
                .That(tracker.ExceedsCoroutineLimit(options))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerExceedsCoroutineLimitWithNullOptions(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();

            await Assert
                .That(tracker.ExceedsCoroutineLimit((SandboxOptions)null))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerSnapshotIncludesCoroutineData(
            LuaCompatibilityVersion version
        )
        {
            AllocationTracker tracker = new AllocationTracker();

            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineCreated();
            tracker.RecordCoroutineDisposed();

            AllocationSnapshot snapshot = tracker.CreateSnapshot();

            await Assert.That(snapshot.CurrentCoroutines).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(snapshot.PeakCoroutines).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(snapshot.TotalCoroutinesCreated).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsMaxCoroutinesPropertyWorks(LuaCompatibilityVersion version)
        {
            SandboxOptions options = new SandboxOptions { MaxCoroutines = 10 };

            await Assert.That(options.MaxCoroutines).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(options.HasCoroutineLimit).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsMaxCoroutinesZeroMeansUnlimited(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions { MaxCoroutines = 0 };

            await Assert.That(options.HasCoroutineLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsMaxCoroutinesNegativeBecomesZero(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions options = new SandboxOptions { MaxCoroutines = -5 };

            await Assert.That(options.MaxCoroutines).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(options.HasCoroutineLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxOptionsCopyConstructorCopiesCoroutineSettings(
            LuaCompatibilityVersion version
        )
        {
            SandboxOptions original = new SandboxOptions
            {
                MaxCoroutines = 15,
                OnCoroutineLimitExceeded = (_, __) => true,
            };

            SandboxOptions copy = new SandboxOptions(original);

            await Assert.That(copy.MaxCoroutines).IsEqualTo(15).ConfigureAwait(false);
            await Assert.That(copy.OnCoroutineLimitExceeded).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsCoroutineLimitCreatesCorrectDetails(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.CoroutineLimit(10, 11);

            await Assert
                .That(details.Kind)
                .IsEqualTo(SandboxViolationType.CoroutineLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(details.LimitValue).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(details.ActualValue).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(details.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(details.IsAccessDenial).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationDetailsCoroutineLimitFormatMessage(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationDetails details = SandboxViolationDetails.CoroutineLimit(5, 6);

            string message = details.FormatMessage();

            await Assert
                .That(message)
                .IsEqualTo("Sandbox violation: coroutine limit exceeded (limit: 5, count: 6)")
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SandboxViolationExceptionCoroutineLimitHasCorrectProperties(
            LuaCompatibilityVersion version
        )
        {
            SandboxViolationException ex = new SandboxViolationException(
                SandboxViolationType.CoroutineLimitExceeded,
                10,
                11
            );

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.CoroutineLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(ex.ConfiguredLimit).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(ex.ActualValue).IsEqualTo(11).ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .IsEqualTo("Sandbox violation: coroutine limit exceeded (limit: 10, count: 11)")
                .ConfigureAwait(false);
        }

        // ========================================================
        // Script Integration Tests for Coroutine Limits
        // ========================================================

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptWithCoroutineLimitHasAllocationTracker(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 5 },
            };
            Script script = new Script(options);

            await Assert.That(script.AllocationTracker).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptCoroutineLimitThrowsWhenExceeded(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 2 },
            };
            Script script = new Script(options);

            // Create first two coroutines - should succeed
            script.DoString("co1 = coroutine.create(function() end)");
            script.DoString("co2 = coroutine.create(function() end)");

            // Third coroutine should fail
            SandboxViolationException exception = await Assert
                .ThrowsAsync<SandboxViolationException>(async () =>
                {
                    await Task.Run(() =>
                        {
                            script.DoString("co3 = coroutine.create(function() end)");
                        })
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            await Assert
                .That(exception.ViolationType)
                .IsEqualTo(SandboxViolationType.CoroutineLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(exception.ConfiguredLimit).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(exception.ActualValue).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptCoroutineLimitCallbackCanAllowContinuation(
            LuaCompatibilityVersion version
        )
        {
            bool callbackInvoked = false;
            int reportedCount = 0;

            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions
                {
                    MaxCoroutines = 2,
                    OnCoroutineLimitExceeded = (script, currentCount) =>
                    {
                        callbackInvoked = true;
                        reportedCount = currentCount;
                        // Return true to allow continuation
                        return true;
                    },
                },
            };
            Script script = new Script(options);

            // Create three coroutines - callback should allow the third
            script.DoString("co1 = coroutine.create(function() end)");
            script.DoString("co2 = coroutine.create(function() end)");
            script.DoString("co3 = coroutine.create(function() end)");

            await Assert.That(callbackInvoked).IsTrue().ConfigureAwait(false);
            await Assert.That(reportedCount).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptCoroutineLimitCallbackCanDenyContinuation(
            LuaCompatibilityVersion version
        )
        {
            bool callbackInvoked = false;

            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions
                {
                    MaxCoroutines = 2,
                    OnCoroutineLimitExceeded = (script, currentCount) =>
                    {
                        callbackInvoked = true;
                        // Return false to deny
                        return false;
                    },
                },
            };
            Script script = new Script(options);

            script.DoString("co1 = coroutine.create(function() end)");
            script.DoString("co2 = coroutine.create(function() end)");

            SandboxViolationException exception = await Assert
                .ThrowsAsync<SandboxViolationException>(async () =>
                {
                    await Task.Run(() =>
                        {
                            script.DoString("co3 = coroutine.create(function() end)");
                        })
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            await Assert.That(callbackInvoked).IsTrue().ConfigureAwait(false);
            await Assert
                .That(exception.ViolationType)
                .IsEqualTo(SandboxViolationType.CoroutineLimitExceeded)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptAllocationTrackerRecordsCoroutineCount(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 10 },
            };
            Script script = new Script(options);

            await Assert
                .That(script.AllocationTracker.CurrentCoroutines)
                .IsEqualTo(0)
                .ConfigureAwait(false);

            script.DoString("co1 = coroutine.create(function() end)");

            await Assert
                .That(script.AllocationTracker.CurrentCoroutines)
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert
                .That(script.AllocationTracker.TotalCoroutinesCreated)
                .IsEqualTo(1)
                .ConfigureAwait(false);

            script.DoString("co2 = coroutine.create(function() end)");
            script.DoString("co3 = coroutine.create(function() end)");

            await Assert
                .That(script.AllocationTracker.CurrentCoroutines)
                .IsEqualTo(3)
                .ConfigureAwait(false);
            await Assert
                .That(script.AllocationTracker.TotalCoroutinesCreated)
                .IsEqualTo(3)
                .ConfigureAwait(false);
            await Assert
                .That(script.AllocationTracker.PeakCoroutines)
                .IsEqualTo(3)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptCoroutineLimitAllowsExactlyMaxCoroutines(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 3 },
            };
            Script script = new Script(options);

            // Should succeed for exactly 3 coroutines
            script.DoString(
                @"
                co1 = coroutine.create(function() end)
                co2 = coroutine.create(function() end)
                co3 = coroutine.create(function() end)
            "
            );

            await Assert
                .That(script.AllocationTracker.CurrentCoroutines)
                .IsEqualTo(3)
                .ConfigureAwait(false);
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptWithoutCoroutineLimitAllowsUnlimited(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 0 }, // Unlimited
            };
            Script script = new Script(options);

            // No tracker when no limits
            await Assert.That(script.AllocationTracker).IsNull().ConfigureAwait(false);

            // Should be able to create many coroutines - if we get here without exception, the test passes
            script.DoString(
                @"
                local coros = {}
                for i = 1, 100 do
                    coros[i] = coroutine.create(function() end)
                end
            "
            );
        }

        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptCoroutineWrapCountsAgainstLimit(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions
            {
                Sandbox = new SandboxOptions { MaxCoroutines = 2 },
            };
            Script script = new Script(options);

            // coroutine.wrap creates a coroutine internally
            script.DoString("f1 = coroutine.wrap(function() return 1 end)");
            script.DoString("f2 = coroutine.wrap(function() return 2 end)");

            // Third wrap should fail
            SandboxViolationException exception = await Assert
                .ThrowsAsync<SandboxViolationException>(async () =>
                {
                    await Task.Run(() =>
                        {
                            script.DoString("f3 = coroutine.wrap(function() return 3 end)");
                        })
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            await Assert
                .That(exception.ViolationType)
                .IsEqualTo(SandboxViolationType.CoroutineLimitExceeded)
                .ConfigureAwait(false);
        }
    }
}
