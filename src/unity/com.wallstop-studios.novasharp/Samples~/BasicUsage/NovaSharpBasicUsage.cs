namespace WallstopStudios.NovaSharp.Unity.Samples
{
    using UnityEngine;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Demonstrates minimal NovaSharp script execution from a Unity component.
    /// </summary>
    public sealed class NovaSharpBasicUsage : MonoBehaviour
    {
        private void Start()
        {
            Script script = new Script();

            script.DoString(
                @"
            print('Hello from Lua!')

            function greet(name)
                return 'Hello, ' .. name .. '!'
            end
        "
            );

            DynValue result = script.Call(script.Globals.Get("greet"), DynValue.NewString("Unity"));
            Debug.Log(result.String);

            script.Globals.Set("unityVersion", DynValue.NewString(Application.unityVersion));
            script.DoString("print('Running on Unity ' .. unityVersion)");
        }
    }
}
