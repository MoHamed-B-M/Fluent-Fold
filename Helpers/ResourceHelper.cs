using System.Resources;

namespace FluentFold.Helpers;

/// <summary>
/// Provides centralized access to localized resources from .resx files.
/// </summary>
public static class ResourceHelper
{
    private static readonly ResourceManager LogResourceManager = new("FluentFold.Resources.LogMessages", typeof(ResourceHelper).Assembly);
    private static readonly ResourceManager ErrorResourceManager = new("FluentFold.Resources.ErrorMessages", typeof(ResourceHelper).Assembly);

    /// <summary>Gets a localized log message string by key.</summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    public static string GetLog(string key) => LogResourceManager.GetString(key) ?? key;

    /// <summary>Gets a localized error message string by key.</summary>
    /// <param name="key">The resource key.</returns>
    /// <returns>The localized string, or the key if not found.</returns>
    public static string GetError(string key) => ErrorResourceManager.GetString(key) ?? key;

    /// <summary>Gets a formatted localized log message.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public static string FormatLog(string key, params object?[] args) => string.Format(GetLog(key), args);

    /// <summary>Gets a formatted localized error message.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public static string FormatError(string key, params object?[] args) => string.Format(GetError(key), args);
}
