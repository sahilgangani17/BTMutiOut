using System;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace BTMultiOut
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // ── CLI mode: called by installer to restore default audio device ──
            // Usage: BTMultiOut.exe --set-default "<device friendly name>"
            if (args.Length == 2 && args[0] == "--set-default")
            {
                SetDefaultDeviceByName(args[1]);
                return;
            }

            // ── Normal GUI mode ───────────────────────────────────────────────
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void SetDefaultDeviceByName(string friendlyName)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                foreach (var device in enumerator.EnumerateAudioEndPoints(
                    DataFlow.Render, DeviceState.Active))
                {
                    if (string.Equals(device.FriendlyName, friendlyName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        PolicyConfigClient.SetDefaultDevice(device.ID);
                        return;
                    }
                }
                // Device not found — silently exit (may have been renamed/removed)
            }
            catch
            {
                // Swallow — installer continues regardless
            }
        }
    }
}