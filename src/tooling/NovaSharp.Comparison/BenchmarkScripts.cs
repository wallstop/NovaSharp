namespace NovaSharp.Comparison;

internal static class BenchmarkScripts
{
    public static string GetScript(ScriptScenario scenario) =>
        scenario switch
        {
            ScriptScenario.TowerOfHanoi => TOWER_OF_HANOI,
            ScriptScenario.EightQueens => EIGHT_QUEENS,
            ScriptScenario.CoroutinePingPong => COROUTINE_PING_PONG,
            _ => TOWER_OF_HANOI,
        };

    private const string TOWER_OF_HANOI =
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

    private const string EIGHT_QUEENS =
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

    private const string COROUTINE_PING_PONG =
        @"
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

        run(256)
    ";
}
