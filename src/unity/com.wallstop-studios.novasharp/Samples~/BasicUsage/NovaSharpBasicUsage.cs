namespace WallstopStudios.NovaSharp.Unity.Samples
{
    using UnityEngine;
    using Facade = global::NovaSharp;

    /// <summary>
    /// Demonstrates minimal facade-based NovaSharp script execution from a Unity component.
    /// </summary>
    public sealed class NovaSharpBasicUsage : MonoBehaviour
    {
        private Facade.LuaEngine _lua;
        private Facade.LuaFunction _onUpdate;
        private int _frameCount;

        private void Start()
        {
            Facade.LuaEngineOptions options = Facade.LuaEngineOptions.Default;
            options.Print = Debug.Log;
            _lua = Facade.LuaEngine.Create(options);

            Facade.LuaValue answer = _lua.Run(
                @"
print('Hello from Lua!')
return 40 + 2"
            );
            Debug.Log(string.Concat("Lua answered ", answer.AsInteger()));

            _onUpdate = _lua.Run(
                    @"
local elapsed = 0
return function(dt)
    elapsed = elapsed + dt
    return elapsed
end"
                )
                .AsFunction();

            using Facade.LuaEngine sandbox = Facade.LuaEngine.Create(
                Facade.LuaEngineOptions.HardSandbox
            );
            sandbox.Globals["hostName"] = "Unity";
            Facade.LuaValue sandboxStatus = sandbox.Run(
                "return hostName .. ': io=' .. type(io) .. ', load=' .. type(load)"
            );
            Debug.Log(sandboxStatus.AsString());
        }

        private void Update()
        {
            if (_onUpdate == null)
            {
                return;
            }

            Facade.LuaValue elapsed = _onUpdate.Call(Time.deltaTime);
            _frameCount++;
            if ((_frameCount % 60) == 0)
            {
                Debug.Log(string.Concat("Lua elapsed seconds: ", elapsed.AsNumber()));
            }
        }

        private void OnDestroy()
        {
            _lua?.Dispose();
        }
    }
}
