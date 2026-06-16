using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Layout;

namespace WorkSpaceLauncherPro.App.Services;

public sealed record LaunchReport(int Requested, int Placed, int Failed, TimeSpan Elapsed);

public interface ILauncherService
{
    Task<LaunchReport> LaunchWithDefaultLayoutAsync(Profile profile, CancellationToken ct = default);
    Task<LaunchReport> LaunchWithLayoutAsync(Profile profile, LayoutTemplate layout, CancellationToken ct = default);
}
