#! /usr/bin/lua
--
-- lua-TestMore inspired coverage for Lua 5.4 `<close>` variables
--
-- Exercises return/goto/break/error paths plus nested closures so NovaSharp
-- mirrors the behaviour validated by the upstream TAP suite (§3.3.8).
--

require 'Test.More'

plan(10)

local function newcloser(log, name)
  local token = {}
  setmetatable(token, {
    __close = function(_, err)
      table.insert(log, string.format('%s:%s', name, err or 'nil'))
    end
  })
  return token
end

-- return statements close all pending scopes before values reach the caller
do
  local log = {}

  local function make()
    local outer <close> = newcloser(log, 'outer')
    do
      local inner <close> = newcloser(log, 'inner')
      return 'done'
    end
  end

  is(make(), 'done', 'return keeps value even when <close> locals exist')
  is_deeply(log, {'inner:nil', 'outer:nil'}, 'return unwinds <close> scopes in reverse order')
end

-- goto jumps unwind <close> locals from every skipped block
do
  local log = {}

  do
    local outer <close> = newcloser(log, 'outer')
    do
      local inner <close> = newcloser(log, 'inner')
      goto skip
    end
  end

  ::skip::

  is_deeply(log, {'inner:nil', 'outer:nil'}, 'goto unwinds <close> locals across nested blocks')
end

-- breaking out of a loop closes the in-scope <close> variable
do
  local log = {}

  for i = 1, 3 do
    local closer <close> = newcloser(log, 'loop_' .. i)
    break
  end

  is_deeply(log, {'loop_1:nil'}, 'break closes loop-scoped <close> variable once per iteration')
end

-- nested loops close their <close> locals as control leaves each level
do
  local log = {}

  while true do
    local outer <close> = newcloser(log, 'outer_loop')
    for _ = 1, 1 do
      local inner <close> = newcloser(log, 'inner_loop')
      break
    end
    break
  end

  is_deeply(log, {'inner_loop:nil', 'outer_loop:nil'}, 'break across nested loops preserves reverse-order close')
end

-- errors propagate into __close metamethods and the thrown message
do
  local log = {}

  local function boom()
    local guard <close> = newcloser(log, 'guard')
    error('fail')
  end

  local ok, err = pcall(boom)
  is(ok, false, 'pcall receives propagated error when __close runs')
  like(err, 'fail', 'pcall surfaces the original error message')
  like(log[1], '^guard:.*fail', '__close receives the error payload')
end

-- nested closures run their own <close> locals before the parent unwinds
do
  local log = {}

  local function outer()
    local outer <close> = newcloser(log, 'outer_fn')

    local function inner()
      local inner <close> = newcloser(log, 'inner_fn')
      return 'value'
    end

    return inner()
  end

  is(outer(), 'value', 'nested function returns original value')
  is_deeply(log, {'inner_fn:nil', 'outer_fn:nil'}, 'nested closures close inner then outer scopes')
end

-- nil and false assignments are ignored for <close> declarations (§3.3.8)
do
  local ok_nil, err_nil = pcall(function()
    local ignored <close> = nil
  end)
  ok(ok_nil, 'nil can back a <close> variable without a __close metamethod')
  is(err_nil, nil, 'nil assignment does not raise an error')

  local ok_false, err_false = pcall(function()
    local ignored <close> = false
  end)
  ok(ok_false, 'false can back a <close> variable without a __close metamethod')
  is(err_false, nil, 'false assignment does not raise an error')
end

-- coroutines close <close> locals only when the coroutine finishes, not at yield
do
  local log = {}

  local co = coroutine.create(function()
    local guard <close> = newcloser(log, 'co')
    coroutine.yield('pause')
    return 'done'
  end)

  local ok_first, first = coroutine.resume(co)
  ok(ok_first, 'first resume succeeds')
  is(first, 'pause', 'yield returns sentinel value')
  is(#log, 0, 'no __close call while coroutine suspended')

  local ok_second, second = coroutine.resume(co)
  ok(ok_second, 'second resume succeeds')
  is(second, 'done', 'coroutine return propagates after yield')
  is_deeply(log, {'co:nil'}, '__close fires once the coroutine finishes')
end

-- coroutine errors propagate into __close after a yield
do
  local log = {}

  local failing = coroutine.create(function()
    local guard <close> = newcloser(log, 'co_err')
    coroutine.yield('pause')
    error('boom')
  end)

  local ok_first = coroutine.resume(failing)
  ok(ok_first, 'first resume of failing coroutine succeeds')
  is(#log, 0, 'no __close invocation before erroring resume')

  local ok_second, err = coroutine.resume(failing)
  ok(ok_second == false, 'second resume fails when coroutine errors')
  like(err, 'boom', 'error message bubbles out of resume')
  like(log[1], '^co_err:.*boom', '__close receives the propagated error value')
end

-- coroutine.close closes suspended coroutines and flushes __close hooks (§6.2 + §3.3.8)
do
  local log = {}

  local co = coroutine.create(function()
    local guard <close> = newcloser(log, 'co_pending')
    coroutine.yield('pause')
    return 'done'
  end)

  local ok_first, first = coroutine.resume(co)
  ok(ok_first, 'resume suspended coroutine before coroutine.close')
  is(first, 'pause', 'yield returns value before coroutine.close')

  local close_ok, close_err = coroutine.close(co)
  ok(close_ok, 'coroutine.close returns true for suspended coroutine')
  is(close_err, nil, 'no error object when coroutine.close succeeds')
  is_deeply(log, {'co_pending:nil'}, 'coroutine.close flushes pending __close values')
end

-- coroutine.close returns the original error when the coroutine died due to an exception (§6.2)
do
  local log = {}

  local co = coroutine.create(function()
    local guard <close> = newcloser(log, 'co_failed')
    error('kapow')
  end)

  local ok_resume, resume_err = coroutine.resume(co)
  ok(ok_resume == false, 'resume captures coroutine failure')
  like(resume_err, 'kapow', 'resume surfaces the thrown error')
  is(#log, 1, '__close already ran during error unwinding')
  like(log[1], '^co_failed:.*kapow', '__close recorded the propagated error')

  local close_ok, close_err = coroutine.close(co)
  ok(close_ok == false, 'coroutine.close returns false when coroutine died with error')
  like(close_err, 'kapow', 'coroutine.close surfaces the original error object')

  local close_again_ok, close_again_err = coroutine.close(co)
  ok(close_again_ok == false, 'coroutine.close continues to report failure for errored coroutines')
  like(close_again_err, 'kapow', 'repeated close calls surface the same error payload')
end

-- Local Variables:
--   mode: lua
--   lua-indent-level: 2
--   fill-column: 100
-- End:
-- vim: ft=lua expandtab shiftwidth=2:
