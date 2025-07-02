namespace Text2Diagram_Backend.Middlewares;

public static class SignalRContext
{
    private static readonly AsyncLocal<string?> _connectionId = new();

    public static string ConnectionId
    {
        get => _connectionId.Value ?? throw new ArgumentNullException(nameof(_connectionId));
        set => _connectionId.Value = value;
    }
}
