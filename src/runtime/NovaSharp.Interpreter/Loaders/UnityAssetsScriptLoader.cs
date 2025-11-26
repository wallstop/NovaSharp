namespace NovaSharp.Interpreter.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// A script loader which can load scripts from assets in Unity3D.
    /// Scripts should be saved as .txt files in a subdirectory of Assets/Resources.
    ///
    /// When NovaSharp is activated on Unity3D and the default script loader is used,
    /// scripts should be saved as .txt files in Assets/Resources/NovaSharp/Scripts.
    /// </summary>
    public class UnityAssetsScriptLoader : ScriptLoaderBase
    {
        private const string LoaderInitializationErrorFormat =
            "Error initializing UnityScriptLoader : {0}";

        private readonly Dictionary<string, string> _resources = new();

        /// <summary>
        /// The default path where scripts are meant to be stored (if not changed)
        /// </summary>
        public const string DefaultPath = "NovaSharp/Scripts";

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityAssetsScriptLoader"/> class.
        /// </summary>
        /// <param name="assetsPath">The path, relative to Assets/Resources. For example
        /// if your scripts are stored under Assets/Resources/Scripts, you should
        /// pass the value "Scripts". If null, "NovaSharp/Scripts" is used. </param>
        public UnityAssetsScriptLoader(string assetsPath = null)
        {
            assetsPath ??= DefaultPath;
#if UNITY_5
            LoadResourcesUnityNative(assetsPath);
#else
            LoadResourcesWithReflection(assetsPath);
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityAssetsScriptLoader"/> class.
        /// </summary>
        /// <param name="scriptToCodeMap">A dictionary mapping filenames to the proper Lua script code.</param>
        public UnityAssetsScriptLoader(Dictionary<string, string> scriptToCodeMap)
        {
            _resources = scriptToCodeMap;
        }

#if UNITY_5
        void LoadResourcesUnityNative(string assetsPath)
        {
            try
            {
                UnityEngine.Object[] array = UnityEngine.Resources.LoadAll(
                    assetsPath,
                    typeof(UnityEngine.TextAsset)
                );

                for (int i = 0; i < array.Length; i++)
                {
                    UnityEngine.TextAsset o = (UnityEngine.TextAsset)array[i];

                    string name = o.name;
                    string text = o.text;

                    _Resources.Add(name, text);
                }
            }
            catch (Exception ex) when (IsRecoverableLoaderInitializationException(ex))
            {
                UnityEngine.Debug.LogErrorFormat("Error initializing UnityScriptLoader : {0}", ex);
            }
        }

#else

        private void LoadResourcesWithReflection(string assetsPath)
        {
            try
            {
                Type resourcesType = Type.GetType("UnityEngine.Resources, UnityEngine");
                Type textAssetType = Type.GetType("UnityEngine.TextAsset, UnityEngine");

                MethodInfo textAssetNameGet = Framework.Do.GetGetMethod(
                    Framework.Do.GetProperty(textAssetType, "name")
                );
                MethodInfo textAssetTextGet = Framework.Do.GetGetMethod(
                    Framework.Do.GetProperty(textAssetType, "text")
                );

                MethodInfo loadAll = Framework.Do.GetMethod(
                    resourcesType,
                    "LoadAll",
                    new Type[] { typeof(string), typeof(Type) }
                );

                Array array = (Array)
                    loadAll.Invoke(null, new object[] { assetsPath, textAssetType });

                for (int i = 0; i < array.Length; i++)
                {
                    object o = array.GetValue(i);

                    string name = textAssetNameGet.Invoke(o, null) as string;
                    string text = textAssetTextGet.Invoke(o, null) as string;

                    _resources.Add(name, text);
                }
            }
            catch (Exception ex) when (IsRecoverableLoaderInitializationException(ex))
            {
                string message = FormatLoaderInitializationError(ex);
#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
                Console.WriteLine(message);
#endif
                System.Diagnostics.Debug.WriteLine(message);
            }
        }
#endif

        private static string GetFileName(string filename) => filename.SliceAfterLastSeparator();

        /// <summary>
        /// Opens a file for reading the script code.
        /// It can return either a string, a byte[] or a Stream.
        /// If a byte[] is returned, the content is assumed to be a serialized (dumped) bytecode. If it's a string, it's
        /// assumed to be either a script or the output of a string.dump call. If a Stream, autodetection takes place.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        /// A string, a byte[] or a Stream.
        /// </returns>
        /// <exception cref="System.Exception">UnityAssetsScriptLoader.LoadFile : Cannot load  + file</exception>
        public override object LoadFile(string file, Table globalContext)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            file = GetFileName(file);

            if (_resources.TryGetValue(file, out string script))
            {
                return script;
            }

            string error = string.Format(
                CultureInfo.InvariantCulture,
                @"Cannot load script '{0}'. By default, scripts should be .txt files placed under a Assets/Resources/{1} directory.
If you want scripts to be put in another directory or another way, use a custom instance of UnityAssetsScriptLoader or implement
your own IScriptLoader (possibly extending ScriptLoaderBase).",
                file,
                DefaultPath
            );

            throw new FileNotFoundException(error, file);
        }

        /// <summary>
        /// Checks if a given file exists
        /// </summary>
        /// <param name="name">The file.</param>
        /// <returns></returns>
        public override bool ScriptFileExists(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string normalizedName = GetFileName(name);
            return _resources.ContainsKey(normalizedName);
        }

        /// <summary>
        /// Gets the list of loaded scripts filenames (useful for debugging purposes).
        /// </summary>
        /// <returns></returns>
        public string[] GetLoadedScripts()
        {
            string[] keys = new string[_resources.Keys.Count];
            int index = 0;
            foreach (string key in _resources.Keys)
            {
                keys[index] = key;
                index += 1;
            }

            return keys;
        }

        private static string FormatLoaderInitializationError(Exception ex)
        {
            return string.Format(CultureInfo.InvariantCulture, LoaderInitializationErrorFormat, ex);
        }

        private static bool IsRecoverableLoaderInitializationException(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }

            if (exception is ScriptRuntimeException)
            {
                return true;
            }

            return exception
                is FileNotFoundException
                    or DirectoryNotFoundException
                    or UnauthorizedAccessException
                    or IOException
                    or SecurityException
                    or InvalidOperationException
                    or NotSupportedException
                    or TypeLoadException
                    or MissingMethodException
                    or TargetInvocationException
                    or ArgumentException;
        }
    }
}
