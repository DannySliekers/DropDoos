using System.Net.Sockets;

namespace DropDoosServer;

internal class PacketManager
{
    private readonly FileManager _fileManager;
    public PacketManager(FileManager fileManager) 
    {
        _fileManager = fileManager;
    }

    public byte[]? HandlePacket(Packet packet)
    {
        if (packet == null)
        {
            return null;
        }

        if (packet.command == Command.Connect)
        {
            return HandleConnectPacket(packet);
        } 
        else if (packet.command == Command.Init)
        {
            return HandleInitPacket(packet);
        }

        return null;
    }

    private byte[]? HandleConnectPacket(Packet packet)
    {
        Console.WriteLine($"Socket server received message: {packet.command}");
        var optionalFields = new Dictionary<string, string>() { 
            { "unique_id", Guid.NewGuid().ToString() }
        };
        Packet response = new() { command = Command.Connect_Resp, optionalFields = optionalFields };
        return response.ToByteArray();
    }

    private byte[]? HandleInitPacket(Packet packet)
    {
        HandleUploads(packet);
        List<File> downloadList = HandleDownloads(packet);
        var optionalFields = new Dictionary<string, string>();
        downloadList.ForEach(file => { 
            optionalFields.Add(file.Name, file.Content);
        });
        Packet response = new() { command = Command.Init_Resp, optionalFields = optionalFields};
        return response.ToByteArray();
    }

    private List<File> HandleDownloads(Packet packet)
    {
        var fileList = new List<File>();
        foreach(var field in packet.optionalFields)
        {
            var file = new File() { Name = field.Key, Content = field.Value };
            fileList.Add(file);
        }
        return _fileManager.BuildDownloadList(fileList);
    }

    private void HandleUploads(Packet packet)
    {
        foreach (var field in packet.optionalFields)
        {
            var file = new File() { Name = field.Key, Content = field.Value };
            bool fileExists = _fileManager.CheckIfFileExists(file);
            bool fileContentEqual = _fileManager.CheckIfContentEqual(file);

            if (fileExists && !fileContentEqual)
            {
                _fileManager.UploadFile(file);
            } 
            else
            {
                _fileManager.AddFile(file);
            }
        }
    }
}
