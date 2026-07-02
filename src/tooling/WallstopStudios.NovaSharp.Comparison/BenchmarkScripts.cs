namespace WallstopStudios.NovaSharp.Comparison;

using System;

/// <summary>
/// Supplies Lua scripts used by the comparison benchmarks.
/// </summary>
internal static class BenchmarkScripts
{
    private const int LoopIterations = 2_000;
    private const int TableEntryCount = 128;
    private const int CoroutineSteps = 256;
    private const int FibonacciInput = 30;
    private const int NBodySteps = 100;
    private const int BinaryTreesDepth = 8;
    private const int BinaryTreesIterations = 64;
    private const int SpectralNormSize = 30;
    private const int TableWorkloadCount = 512;
    private const int TableLookupPasses = 12;
    private const int TableTraversalCount = 384;
    private const int TableChurnCycles = 128;
    private const int StringConcatPieces = 256;
    private const int StringPatternPasses = 64;
    private const int StringFormatIterations = 512;

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
            ScriptScenario.FibonacciRecursive,
            ScriptScenario.NBody,
            ScriptScenario.BinaryTrees,
            ScriptScenario.SpectralNorm,
            ScriptScenario.TableIntegerFillIterate,
            ScriptScenario.TableStringKeyLookup,
            ScriptScenario.TableNextTraversal,
            ScriptScenario.TableInsertRemoveChurn,
            ScriptScenario.StringConcatChains,
            ScriptScenario.StringPatternGsubFind,
            ScriptScenario.StringFormat,
        };

    /// <summary>
    /// Returns the benchmark scenario names used by BenchmarkDotNet parameter sources.
    /// </summary>
    public static string[] GetScenarioNames()
    {
        ScriptScenario[] scenarios = GetScenarios();
        string[] names = new string[scenarios.Length];
        for (int i = 0; i < scenarios.Length; i++)
        {
            names[i] = GetScenarioName(scenarios[i]);
        }

        return names;
    }

    /// <summary>
    /// Returns the stable display name for <paramref name="scenario"/>.
    /// </summary>
    public static string GetScenarioName(ScriptScenario scenario) =>
        scenario switch
        {
            ScriptScenario.NumericLoops => nameof(ScriptScenario.NumericLoops),
            ScriptScenario.TableMutation => nameof(ScriptScenario.TableMutation),
            ScriptScenario.TowerOfHanoi => nameof(ScriptScenario.TowerOfHanoi),
            ScriptScenario.EightQueens => nameof(ScriptScenario.EightQueens),
            ScriptScenario.CoroutinePingPong => nameof(ScriptScenario.CoroutinePingPong),
            ScriptScenario.FibonacciRecursive => nameof(ScriptScenario.FibonacciRecursive),
            ScriptScenario.NBody => nameof(ScriptScenario.NBody),
            ScriptScenario.BinaryTrees => nameof(ScriptScenario.BinaryTrees),
            ScriptScenario.SpectralNorm => nameof(ScriptScenario.SpectralNorm),
            ScriptScenario.TableIntegerFillIterate => nameof(
                ScriptScenario.TableIntegerFillIterate
            ),
            ScriptScenario.TableStringKeyLookup => nameof(ScriptScenario.TableStringKeyLookup),
            ScriptScenario.TableNextTraversal => nameof(ScriptScenario.TableNextTraversal),
            ScriptScenario.TableInsertRemoveChurn => nameof(ScriptScenario.TableInsertRemoveChurn),
            ScriptScenario.StringConcatChains => nameof(ScriptScenario.StringConcatChains),
            ScriptScenario.StringPatternGsubFind => nameof(ScriptScenario.StringPatternGsubFind),
            ScriptScenario.StringFormat => nameof(ScriptScenario.StringFormat),
            _ => throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario,
                "Unknown comparison benchmark scenario."
            ),
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
            ScriptScenario.FibonacciRecursive => FibonacciRecursive,
            ScriptScenario.NBody => NBody,
            ScriptScenario.BinaryTrees => BinaryTrees,
            ScriptScenario.SpectralNorm => SpectralNorm,
            ScriptScenario.TableIntegerFillIterate => TableIntegerFillIterate,
            ScriptScenario.TableStringKeyLookup => TableStringKeyLookup,
            ScriptScenario.TableNextTraversal => TableNextTraversal,
            ScriptScenario.TableInsertRemoveChurn => TableInsertRemoveChurn,
            ScriptScenario.StringConcatChains => StringConcatChains,
            ScriptScenario.StringPatternGsubFind => StringPatternGsubFind,
            ScriptScenario.StringFormat => StringFormat,
            _ => throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario,
                "Unknown comparison benchmark scenario."
            ),
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

    private static readonly string FibonacciRecursive =
        $@"
        local function fib(n)
            if n < 2 then
                return n
            end
            return fib(n - 1) + fib(n - 2)
        end

        return fib({FibonacciInput})
    ";

    private static readonly string NBody =
        $@"
        local bodies = {{
            {{ x = 0.0, y = 0.0, z = 0.0, vx = 0.0, vy = 0.0, vz = 0.0, mass = 1.0 }},
            {{ x = 4.84143144246472090, y = -1.16032004402742839, z = -0.10362204447112311, vx = 0.00166007664274404, vy = 0.00769901118419740, vz = -0.00006904600169721, mass = 0.00095479193842433 }},
            {{ x = 8.34336671824457987, y = 4.12479856412430479, z = -0.40352341711432138, vx = -0.00276742510726862, vy = 0.00499852801234917, vz = 0.00002304172975738, mass = 0.00028588598066613 }},
            {{ x = 12.8943695621391310, y = -15.1111514016986312, z = -0.22330757889265573, vx = 0.00296460137564762, vy = 0.00237847173959481, vz = -0.00002965895685402, mass = 0.00004366244043351 }},
            {{ x = 15.3796971148509165, y = -25.9193146099879641, z = 0.17925877295037118, vx = 0.00268067772490389, vy = 0.00162824170038242, vz = -0.00009515922545197, mass = 0.00005151389020466 }}
        }}

        local function advance(dt)
            for i = 1, #bodies do
                local bi = bodies[i]
                for j = i + 1, #bodies do
                    local bj = bodies[j]
                    local dx = bi.x - bj.x
                    local dy = bi.y - bj.y
                    local dz = bi.z - bj.z
                    local distance_squared = dx * dx + dy * dy + dz * dz
                    local distance = math.sqrt(distance_squared)
                    local magnitude = dt / (distance_squared * distance)

                    bi.vx = bi.vx - dx * bj.mass * magnitude
                    bi.vy = bi.vy - dy * bj.mass * magnitude
                    bi.vz = bi.vz - dz * bj.mass * magnitude
                    bj.vx = bj.vx + dx * bi.mass * magnitude
                    bj.vy = bj.vy + dy * bi.mass * magnitude
                    bj.vz = bj.vz + dz * bi.mass * magnitude
                end
            end

            for i = 1, #bodies do
                local body = bodies[i]
                body.x = body.x + dt * body.vx
                body.y = body.y + dt * body.vy
                body.z = body.z + dt * body.vz
            end
        end

        local function energy()
            local total = 0.0
            for i = 1, #bodies do
                local bi = bodies[i]
                total = total + 0.5 * bi.mass * (bi.vx * bi.vx + bi.vy * bi.vy + bi.vz * bi.vz)
                for j = i + 1, #bodies do
                    local bj = bodies[j]
                    local dx = bi.x - bj.x
                    local dy = bi.y - bj.y
                    local dz = bi.z - bj.z
                    total = total - (bi.mass * bj.mass) / math.sqrt(dx * dx + dy * dy + dz * dz)
                end
            end
            return total
        end

        for i = 1, {NBodySteps} do
            advance(0.01)
        end

        return energy()
    ";

    private static readonly string BinaryTrees =
        $@"
        local function make_tree(depth)
            if depth <= 0 then
                return {{ 1 }}
            end
            return {{ make_tree(depth - 1), make_tree(depth - 1) }}
        end

        local function check(node)
            local left = node[1]
            if type(left) == ""number"" then
                return left
            end
            return 1 + check(left) + check(node[2])
        end

        local checksum = 0
        for i = 1, {BinaryTreesIterations} do
            checksum = checksum + check(make_tree({BinaryTreesDepth}))
        end

        return checksum
    ";

    private static readonly string SpectralNorm =
        $@"
        local n = {SpectralNormSize}

        local function a(i, j)
            local ij = i + j - 1
            return 1.0 / ((ij * (ij + 1) / 2) + i)
        end

        local function multiply_av(source, target)
            for i = 1, n do
                local sum = 0.0
                for j = 1, n do
                    sum = sum + a(i, j) * source[j]
                end
                target[i] = sum
            end
        end

        local function multiply_atv(source, target)
            for i = 1, n do
                local sum = 0.0
                for j = 1, n do
                    sum = sum + a(j, i) * source[j]
                end
                target[i] = sum
            end
        end

        local u = {{}}
        local v = {{}}
        local tmp = {{}}
        for i = 1, n do
            u[i] = 1.0
            v[i] = 0.0
            tmp[i] = 0.0
        end

        for i = 1, 10 do
            multiply_av(u, tmp)
            multiply_atv(tmp, v)
            multiply_av(v, tmp)
            multiply_atv(tmp, u)
        end

        local v_bv = 0.0
        local vv = 0.0
        for i = 1, n do
            v_bv = v_bv + u[i] * v[i]
            vv = vv + v[i] * v[i]
        end

        return math.sqrt(v_bv / vv)
    ";

    private static readonly string TableIntegerFillIterate =
        $@"
        local values = {{}}
        for i = 1, {TableWorkloadCount} do
            values[i] = i * 3
        end

        local sum = 0
        for pass = 1, {TableLookupPasses} do
            for i = 1, {TableWorkloadCount} do
                sum = sum + values[i]
            end
        end

        return sum
    ";

    private static readonly string TableStringKeyLookup =
        $@"
        local values = {{}}
        for i = 1, {TableWorkloadCount} do
            values[""field_"" .. i] = i
        end

        local sum = 0
        for pass = 1, {TableLookupPasses} do
            for i = 1, {TableWorkloadCount} do
                sum = sum + values[""field_"" .. i]
            end
        end

        return sum
    ";

    private static readonly string TableNextTraversal =
        $@"
        local values = {{}}
        for i = 1, {TableTraversalCount} do
            values[i] = i
            values[""key_"" .. i] = i * 2
        end

        local sum = 0
        local key = nil
        local value = nil
        while true do
            key, value = next(values, key)
            if key == nil then
                break
            end
            sum = sum + value
        end

        return sum
    ";

    private static readonly string TableInsertRemoveChurn =
        $@"
        local values = {{}}
        for i = 1, {TableEntryCount} do
            table.insert(values, i)
        end

        local sum = 0
        for cycle = 1, {TableChurnCycles} do
            table.insert(values, 1, cycle)
            sum = sum + table.remove(values)
            if (cycle % 4) == 0 then
                table.insert(values, cycle * 2)
            end
        end

        return sum + #values
    ";

    private static readonly string StringConcatChains =
        $@"
        local text = """"
        for i = 1, {StringConcatPieces} do
            text = text .. ""x"" .. (i % 10)
        end

        return #text
    ";

    private static readonly string StringPatternGsubFind =
        $@"
        local source = """"
        for i = 1, 32 do
            source = source .. ""id"" .. i .. ""=value"" .. (i % 7) .. ""; ""
        end

        local score = 0
        for i = 1, {StringPatternPasses} do
            local replaced, count = string.gsub(source, ""value(%d)"", ""v%1"")
            local first, last = string.find(replaced, ""id%d+=v%d"")
            score = score + count + (first or 0) + (last or 0)
        end

        return score
    ";

    private static readonly string StringFormat =
        $@"
        local total = 0
        for i = 1, {StringFormatIterations} do
            local text = string.format(""%04d:%0.3f:%s"", i, i / 3, ""bench"")
            total = total + #text
        end

        return total
    ";
}
