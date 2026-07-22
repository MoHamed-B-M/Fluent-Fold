namespace FluentFold.Services;

/// <summary>Provides access to the main application window handle.</summary>
public interface IWindowService
{
    /// <summary>The native window handle (HWND) of the main window.</summary>
    IntPtr WindowHandle { get; }
}
