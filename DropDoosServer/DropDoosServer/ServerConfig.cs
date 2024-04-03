namespace DropDoosServer;

public class ServerConfig
{
    public required string ServerFolder {  get; set; }
    public required string IpAddress { get; set; }
    public required int Port { get; set; }
    public required int PackageSizeInBytes { get; set; }
}
