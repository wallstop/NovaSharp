namespace WallstopStudios.NovaSharp.Comparison;

/// <summary>
/// Supplies Lua scripts used by the comparison benchmarks.
/// </summary>
internal static class BenchmarkScripts
{
    private const int LoopIterations = 2_000;
    private const int TableEntryCount = 128;
    private const int CoroutineSteps = 256;

    /// <summary>
    /// Returns the benchmark scenarios that should be exported for external runners.
    /// </summary>
    public static ScriptScenario[] GetScenarios() =>
        new[]
        {
            ScriptScenario.NumericLoops,
            ScriptScenario.TableMutation,
            ScriptScenario.TowerOfHanoi,
            ScriptScenario.EightQueens,
            ScriptScenario.CoroutinePingPong,
        };

    /// <summary>
    /// Returns the script associated with <paramref name="scenario"/>.
    /// </summary>
    public static string GetScript(ScriptScenario scenario) =>
        scenario switch
        {
            ScriptScenario.NumericLoops => NumericLoops,
            ScriptScenario.TableMutation => TableMutation,
            ScriptScenario.TowerOfHanoi => TowerOfHanoi,
            ScriptScenario.EightQueens => EightQueens,
            ScriptScenario.CoroutinePingPong => CoroutinePingPong,
            _ => TowerOfHanoi,
        };

    private static readonly string NumericLoops =
        $@"
        local sum = 0.0
        for i = 1, {LoopIterations} do
            sum = sum + math.sin(i) * math.cos(i * 0.5)
            if (i % 7) == 0 then
                sum = sum / 2.0
            end
        end
        return sum
    ";

    private static readonly string TableMutation =
        $@"
        local source = {{}}
        for i = 1, {TableEntryCount} do
            source[i] = i * 1.5
        end

        local acc = 0
        for i = 1, #source do
            acc = acc + source[i]
            source[i] = acc % 17
        end
        for k = #source, 1, -3 do
            source[k] = nil
        end
        return acc
    ";

    private const string TowerOfHanoi =
        @"
        function move(n, src, dst, via)
            if n > 0 then
                move(n - 1, src, via, dst)
                move(n - 1, via, dst, src)
            end
        end

        for i = 1, 1000 do
            move(4, 1, 2, 3)
        end
    ";

    private const string EightQueens =
        @"
        local N = 8
        local board = {}
        for i = 1, N do
            board[i] = {}
            for j = 1, N do
                board[i][j] = false
            end
        end

        local function allowed(x, y)
            for i = 1, x - 1 do
                if board[i][y] or (i <= y and board[x - i][y - i]) or (y + i <= N and board[x - i][y + i]) then
                    return false
                end
            end
            return true
        end

        local function solve(x)
            for y = 1, N do
                if allowed(x, y) then
                    board[x][y] = true
                    if x == N or solve(x + 1) then
                        return true
                    end
                    board[x][y] = false
                end
            end
            return false
        end

        solve(1)
    ";

    private static readonly string CoroutinePingPong =
        $@"
        local function producer(n)
            local value = 0
            for i = 1, n do
                value = value + math.sqrt(i)
                coroutine.yield(value)
            end
            return value
        end

        local function run(n)
            local co = coroutine.create(function() return producer(n) end)
            local last = 0
            for i = 1, n do
                local ok, result = coroutine.resume(co, i)
                if not ok then error(result) end
                last = result
            end
            return last
        end

        run({CoroutineSteps})
    ";
}
