namespace NovaSharp.Interpreter.Loaders
{
    using System;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// A script loader used for platforms we cannot initialize in any better way..
    /// </summary>
    internal class InvalidScriptLoader : IScriptLoader
    {
        private readonly string _error;

        /// <summary>
        /// Initializes the loader with a user-facing error message.
        /// </summary>
        internal InvalidScriptLoader(string frameworkname)
        {
            _error =
                $@"Loading scripts from files is not automatically supported on {frameworkname}. 
Please implement your own IScriptLoader (possibly, extending ScriptLoaderBase for easier implementation),
use a preexisting loader like EmbeddedResourcesScriptLoader or UnityAssetsScriptLoader or load scripts from strings.";
        }

        /// <summary>
        /// Always throws because file loading is not supported on this platform.
        /// </summary>
        public object LoadFile(string file, Table globalContext)
        {
            throw new PlatformNotSupportedException(_error);
        }

        /// <summary>
        /// Returns the provided filename unchanged; no normalization is available.
        /// </summary>
        public string ResolveFileName(string filename, Table globalContext)
        {
            return filename;
        }

        /// <summary>
        /// Always throws because module resolution is unavailable.
        /// </summary>
        public string ResolveModuleName(string modname, Table globalContext)
        {
            throw new PlatformNotSupportedException(_error);
        }
    }
}
