using System;
using System.Collections.Generic;

public static class EventCenter
{
    private static readonly Dictionary<GameEventKey, List<Delegate>> handlers = new();

    public static void Subscribe<T>(GameEvent<T> gameEvent, Action<T> handler)
    {
        if (handler == null)
            return;

        var key = new GameEventKey(gameEvent.Name, typeof(T));
        if (!handlers.TryGetValue(key, out List<Delegate> list))
        {
            list = new List<Delegate>();
            handlers[key] = list;
        }

        if (!list.Contains(handler))
            list.Add(handler);
    }

    public static void Unsubscribe<T>(GameEvent<T> gameEvent, Action<T> handler)
    {
        if (handler == null)
            return;

        var key = new GameEventKey(gameEvent.Name, typeof(T));
        if (!handlers.TryGetValue(key, out List<Delegate> list))
            return;

        list.Remove(handler);
        if (list.Count == 0)
            handlers.Remove(key);
    }

    public static void Publish<T>(GameEvent<T> gameEvent, T data)
    {
        var key = new GameEventKey(gameEvent.Name, typeof(T));
        if (!handlers.TryGetValue(key, out List<Delegate> list))
            return;

        var snapshot = list.ToArray();
        foreach (Delegate handler in snapshot)
        {
            if (handler is Action<T> typedHandler)
                typedHandler.Invoke(data);
        }
    }

    public static void Clear()
    {
        handlers.Clear();
    }

    private readonly struct GameEventKey : IEquatable<GameEventKey>
    {
        private readonly string name;
        private readonly Type payloadType;

        public GameEventKey(string eventName, Type type)
        {
            name = eventName;
            payloadType = type;
        }

        public bool Equals(GameEventKey other)
        {
            return name == other.name && payloadType == other.payloadType;
        }

        public override bool Equals(object obj)
        {
            return obj is GameEventKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (name.GetHashCode() * 397) ^ payloadType.GetHashCode();
            }
        }
    }
}
