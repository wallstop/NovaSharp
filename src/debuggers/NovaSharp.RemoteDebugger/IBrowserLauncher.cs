namespace NovaSharp.RemoteDebugger
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Abstraction used by debugger hosts to surface the remote debugger UI.
    /// </summary>
    public interface IBrowserLauncher
    {
        /// <summary>
        /// Opens the provided debugger UI URL using the host environment.
        /// </summary>
        /// <param name="url">Destination to launch.</param>
        public void Launch(Uri url);
    }

    /// <summary>
    /// Default implementation that shells out to the platform browser.
    /// </summary>
    public sealed class ProcessBrowserLauncher : IBrowserLauncher
    {
        public static readonly ProcessBrowserLauncher Instance = new();

        private ProcessBrowserLauncher() { }

        /// <summary>
        /// Launches the system-configured browser for the specified debugger URL.
        /// </summary>
        /// <param name="url">Destination to open.</param>
        public void Launch(Uri url)
        {
            if (url == null)
            {
                return;
            }

            ProcessStartInfo psi = new() { FileName = url.AbsoluteUri, UseShellExecute = true };

            Process.Start(psi);
        }
    }
}
