using System.Collections;
using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger;
using UnityEngine;

public class DebuggerTestBehaviour : MonoBehaviour
{
    MoonSharpVsCodeDebugServer server;
    Script script;
    Closure func;

    // Use this for initialization
    void Start()
    {
        server = new MoonSharpVsCodeDebugServer().Start();
        script = new Script();

        Script script1 = new Script();
        script1.DoString(
            @"

	function run()
		for i = 1, 4 do
			print (i)
		end
	end
"
        );

        server.AttachToScript(script1, "Script #1");
        func = script1.Globals.Get("run").Function;
    }

    // Update is called once per frame
    void Update()
    {
        func.Call();
    }
}
