﻿namespace DropDoosClient.Data;

internal class File
{
    public required string Name { get; set; }
    public byte[]? Content { get; set; }
    public long Size { get; set; }
    public long Position { get; set; }
}
