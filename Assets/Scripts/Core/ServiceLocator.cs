using System;
using System.Collections.Generic;

namespace WDG
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            var type = typeof(T);
            _services[type] = service;
        }

        public static T Get<T>()
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
