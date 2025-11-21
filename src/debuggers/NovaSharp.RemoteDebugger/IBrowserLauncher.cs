namespace NovaSharp.RemoteDebugger
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Abstraction used by debugger hosts to surface the remote debugger UI.
    /// </summary>
    public interface IBrowserLauncher
    {
        public void Launch(Uri url);
    }

    /// <summary>
    /// Default implementation that shells out to the platform browser.
    /// </summary>
    public sealed class ProcessBrowserLauncher : IBrowserLauncher
    {
        public static readonly ProcessBrowserLauncher Instance = new();

        private ProcessBrowserLauncher() { }

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
