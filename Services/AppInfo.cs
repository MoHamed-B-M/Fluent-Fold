using System.Reflection;
using Windows.ApplicationModel;

namespace FluentFold.Services;

public static class AppInfo
{
    public static string CurrentVersion
    {
        get
        {
            try
            {
                var v = Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
            catch
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{ver?.Major ?? 1}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}.{ver?.Revision ?? 0}";
            }
        }
    }
}