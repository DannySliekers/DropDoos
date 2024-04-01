namespace DropDoosServer.Data;

public class File
{
    public required string Name { get; set; }
    public string? Content { get; set; }
    public long Size { get; set; }
    public long Position { get; set; }
}
