namespace NeoBeard.Age;

internal sealed record Stanza
{
    public Stanza(string type, string[] args, byte[] body)
    {
        this.Type = type;
        this.Args = args;
        this.Body = body;
    }

    public string Type { get; }

    public string[] Args { get; }

    public byte[] Body { get; }
}