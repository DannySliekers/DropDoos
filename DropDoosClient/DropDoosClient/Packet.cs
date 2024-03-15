namespace DropDoosClient;

internal class Packet
{
    public required string command;
    public Dictionary<string, string>? optionalFields;
}
