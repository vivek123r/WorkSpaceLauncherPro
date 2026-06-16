namespace WorkSpaceLauncherPro.Core.Launching;

/// <summary>Result of a successful app launch — the PID and (optionally) the main hwnd.</summary>
public sealed record LaunchedApp(string Target, uint Pid, IntPtr Hwnd);
