using Microsoft.Extensions.Logging;
using DropDoosServer.Data;

namespace DropDoosServer.Managers;

internal class PacketManager : IPacketManager
{
    private readonly ILogger<IFileManager> _logger;
    private readonly IFileManager _fileManager;

    public PacketManager(IFileManager fileManager, ILogger<IFileManager> logger)
    {
        _logger = logger;
        _fileManager = fileManager;
    }

    public Packet? HandlePacket(Packet packet)
    {
        switch (packet.Command)
        {
            case Command.Connect:
                return HandleConnectPacket(packet);
            case Command.Init:
                return HandleInitPacket(packet).Result;
            case Command.Sync:
            //return HandleSyncPacket(packet);
            case Command.Download:
                return HandleDownloadPacket(packet);
            default:
                return null;
        }
    }

    private Packet? HandleConnectPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        Packet response = new() { Command = Command.Connect_Resp };
        _logger.LogInformation("Sending {command} to client", response.Command);
        return response;
    }

    private async Task<Packet?> HandleInitPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        var doneWithUploading = await HandleUploads(packet);
        Packet? response = null;

        if (doneWithUploading)
        {
            response = new() { Command = Command.Init_Resp };
            _logger.LogInformation("Sending {command} to client", response.Command);
        }

        return response;
    }

    private Packet? HandleDownloadPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        _fileManager.AddServerFilesToDownloadQueue();
        var response = new Packet() { Command = Command.Download_Resp };
        _logger.LogInformation("Sending {command} to client", response.Command);

        return response;
    }

    //private Packet HandleSyncPacket(Packet packet)
    //{
    //    _logger.LogInformation("Socket server received message: {command}", packet.Command);
    //    var optionalFields = Sync(packet);
    //    Packet response = new() { Command = Command.Sync_Resp, optionalFields = optionalFields };
    //    _logger.LogInformation("Sending {command} to client", response.Command);
    //    return response;
    //}

    //private List<File> Sync(Packet packet)
    //{
    //    HandleUploads(packet);
    //    List<File> downloadList = HandleDownloads(packet);
    //    var optionalFields = new Dictionary<string, string>();
    //    downloadList.ForEach(file =>
    //    {
    //        optionalFields.Add(file.Name, file.Content);
    //    });

    //    return optionalFields;
    //}

    //private List<File> HandleDownloads(Packet packet)
    //{
    //    var fileList = new List<File>();
    //    foreach (var field in packet.optionalFields)
    //    {
    //        var file = new File() { Name = field.Key, Content = field.Value };
    //        fileList.Add(file);
    //    }
    //    return _fileManager.BuildDownloadList(fileList);
    //}

    private async Task<bool> HandleUploads(Packet packet)
    {
        bool doneWithUploading = true;
        var serverFileSize =  await _fileManager.UploadFile(packet.File);

        if (packet.File.Size != serverFileSize)
        {
            doneWithUploading = false;
        }

        return doneWithUploading;
    }
}
