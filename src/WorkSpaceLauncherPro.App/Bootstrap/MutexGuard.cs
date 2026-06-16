using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace WorkSpaceLauncherPro.App.Bootstrap;

/// <summary>
/// Single-instance guard via a named mutex. If another instance holds the mutex,
/// the running instance is brought to the foreground instead of starting a new one.
/// </summary>
public sealed class MutexGuard : IDisposable
{
    private readonly string _name;
    private Mutex? _mutex;
    private bool _owned;

    public MutexGuard(string name) => _name = name;

    public bool Acquire()
    {
        _mutex = new Mutex(initiallyOwned: true, _name, out var createdNew);
        _owned = createdNew;
        return createdNew;
    }

    /// <summary>Sends a "show me" message to the first instance via a hidden message-only window.</summary>
    public void NotifyFirstInstance()
    {
        // Find the existing process's main window and bring it forward.
        var current = System.Diagnostics.Process.GetCurrentProcess();
        foreach (var p in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
        {
            if (p.Id == current.Id) continue;
            var hwnd = p.MainWindowHandle;
            if (hwnd == IntPtr.Zero) continue;
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            break;
        }
    }

    public void Dispose()
    {
        if (_owned && _mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* ignored */ }
        }
        _mutex?.Dispose();
    }

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
