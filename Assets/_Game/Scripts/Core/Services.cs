using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Простой service locator. Сервисы регистрируются в GameBootstrap и достаются по типу.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Map = new Dictionary<Type, object>(32);

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Map[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            Map.Remove(typeof(T));
        }

        /// <summary>Снимает регистрацию, только если зарегистрирован именно этот объект (не новый того же типа).</summary>
        public static void Unregister<T>(T service) where T : class
        {
            if (Map.TryGetValue(typeof(T), out var registered) && ReferenceEquals(registered, service))
                Map.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (Map.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. Start the game from the Boot scene.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Map.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear()
        {
            Map.Clear();
        }

        // Сброс при старте Play Mode, если в редакторе отключён Domain Reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Clear();
        }
    }
}
