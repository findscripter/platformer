public readonly struct GameEvent<T>
{
    public GameEvent(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
