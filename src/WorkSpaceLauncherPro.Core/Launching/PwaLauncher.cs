using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Core.Launching;

/// <summary>
/// Launches UWP / PWA / Windows Store apps by AppUserModelID (AUMID) using the
/// IApplicationActivationManager COM interface. Returns the spawned PID so the
/// window position engine can find and place the new window.
/// </summary>
public sealed class PwaLauncher
{
    private readonly WindowEnumerator _enumerator;
    private readonly ILogger<PwaLauncher> _log;

    public PwaLauncher(WindowEnumerator enumerator, ILogger<PwaLauncher> log)
    {
        _enumerator = enumerator;
        _log = log;
    }

    public LaunchedApp? LaunchByAumid(string aumid, string? arguments = null)
    {
        try
        {
            var type = Type.GetTypeFromCLSID(CLSID_ApplicationActivationManager, throwOnError: true)!;
            var mgr = (IApplicationActivationManager)Activator.CreateInstance(type)!;
            var hr = mgr.ActivateApplication(aumid, arguments ?? "", ActivateOptions.None, out var pid);
            if (hr != 0)
            {
                _log.LogError("ActivateApplication({Aumid}) failed with hr=0x{Hr:X8}", aumid, hr);
                return null;
            }
            _log.LogInformation("Launched AUMID {Aumid} as pid {Pid}", aumid, pid);
            return new LaunchedApp(aumid, pid, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "PwaLauncher.LaunchByAumid failed for {Aumid}", aumid);
            return null;
        }
    }

    public IntPtr? FindMainWindowForPid(uint pid, TimeSpan timeout, string? titleContains = null)
        => _enumerator.FindByPid(pid, titleContains, timeout);

    private static readonly Guid CLSID_ApplicationActivationManager =
        new("2e941141-7f97-4756-ba1d-9decde94a1b9");

    [Flags]
    private enum ActivateOptions : uint
    {
        None = 0x00000000,
        DesignMode = 0x00000001,
        Debug = 0x00000002,
        RunAsAdmin = 0x00000004,
        RunAsInvoker = 0x00000008
    }

    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde94a1b9"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            ActivateOptions options,
            out uint processId);
    }
}
