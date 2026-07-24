using Microsoft.Extensions.Logging;

namespace FluentFold.Services;

public sealed class FirstLaunchService(IAppSettingsService settings, ILogger<FirstLaunchService> logger) : IFirstLaunchService
{
    public bool IsFirstLaunch => !settings.HasCompletedFirstLaunch;

    public void MarkCompleted()
    {
        settings.HasCompletedFirstLaunch = true;
        logger.LogInformation("First-launch onboarding marked completed");
    }

    public void Reset()
    {
        settings.HasCompletedFirstLaunch = false;
        logger.LogInformation("First-launch onboarding reset");
    }
}
