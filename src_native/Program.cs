using System;
using System.Threading;
using System.Windows.Forms;
using EasyBluetooth.UI;

namespace EasyBluetooth
{
    internal static class Program
    {
        private static Mutex _mutex;

        private static void Log(string msg)
        {
            try
            {
                string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                System.IO.File.AppendAllText(logFile, string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] {1}\r\n", DateTime.Now, msg));
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int RegisterWindowMessage(string lpString);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        private static void Main(string[] args)
        {
            Log("Main started. Args: " + string.Join(" ", args));

            bool isNew;
            _mutex = new Mutex(true, "Local\\OmniBattery_SingleInstance_Mutex_2026", out isNew);
            Log("Mutex acquired: " + isNew);

            if (!isNew)
            {
                Log("Another instance is running. Signaling existing window to restore.");
                try
                {
                    int msg = RegisterWindowMessage("OmniBattery_Show_MainWindow_Message");
                    PostMessage((IntPtr)0xffff, msg, IntPtr.Zero, IntPtr.Zero);

                    IntPtr hWnd = FindWindow(null, "OmniBattery");
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, 9); // SW_RESTORE
                        SetForegroundWindow(hWnd);
                    }
                }
                catch { }
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Log("Creating MainWindow...");
                var mainWin = new MainWindow();
                Log("MainWindow created. Handle: " + mainWin.Handle);

                bool startInTray = false;
                foreach (string arg in args)
                {
                    if (arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-tray", StringComparison.OrdinalIgnoreCase))
                    {
                        startInTray = true;
                        break;
                    }
                }

                if (startInTray)
                {
                    Log("Starting in tray mode...");
                    mainWin.WindowState = FormWindowState.Minimized;
                    mainWin.ShowInTaskbar = false;
                    Application.Run(mainWin);
                }
                else
                {
                    Log("Starting normal mode...");
                    Application.Run(mainWin);
                }

                Log("Application.Run exited normally.");
            }
            catch (Exception ex)
            {
                Log("FATAL EXCEPTION: " + ex.ToString());
            }
            finally
            {
                Log("Main ended.");
            }
        }
    }
}
