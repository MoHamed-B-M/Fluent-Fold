using System.Collections.Concurrent;

namespace FluentFold.Services;

public static class ServiceLocator
{
    private static readonly ConcurrentDictionary<Type, object> _services = new();

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public static T Resolve<T>() where T : class
    {
        return (_services.GetValueOrDefault(typeof(T)) as T)!;
    }
}
