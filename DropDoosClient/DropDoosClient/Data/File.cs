namespace DropDoosClient.Data;

internal class File
{
    public required string Name { get; set; }
    public required byte[] Content { get; set; }
    public required long Size { get; set; }
}
