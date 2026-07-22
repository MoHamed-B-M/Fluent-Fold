using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentFold.Services;

public sealed class FirstLaunchService(ILogger<FirstLaunchService> logger) : IFirstLaunchService
{
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

    public bool IsFirstLaunch
    {
        get
        {
            var val = _settings.Values["HasCompletedOnboarding"];
            return val switch
            {
                bool b => !b,
                string s => !bool.TryParse(s, out var b) || !b,
                _ => true
            };
        }
    }

    public void MarkCompleted()
    {
        _settings.Values["HasCompletedOnboarding"] = true;
        logger.LogInformation("First-launch onboarding marked completed");
    }

    public void Reset()
    {
        _settings.Values.Remove("HasCompletedOnboarding");
        logger.LogInformation("First-launch onboarding reset");
    }
}
