namespace DropDoosClient;

internal class ClientConfig
{
    public required string ClientFolder { get; set; }
    public required TimeSpan SyncRate { get; set; }
    public required string IpAddress { get; set; }
    public required int Port { get; set; }
}
