namespace DropDoosServer.Data;

internal class File
{
    public required string Name { get; set; }
    public string? Content { get; set; }
    public long Size { get; set; }
    public long Position { get; set; }
}
