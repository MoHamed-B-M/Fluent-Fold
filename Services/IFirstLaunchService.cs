namespace FluentFold.Services;

/// <summary>Tracks whether the user has completed the first-launch onboarding flow.</summary>
public interface IFirstLaunchService
{
    /// <summary>Returns true if the onboarding flow has not been completed.</summary>
    bool IsFirstLaunch { get; }
    /// <summary>Marks the onboarding flow as completed.</summary>
    void MarkCompleted();
    /// <summary>Resets the onboarding state so the flow will be shown again.</summary>
    void Reset();
}
