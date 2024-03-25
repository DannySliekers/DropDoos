using File = DropDoosServer.Data.File;
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

    public Task<Packet?> HandlePacket(Packet packet)
    {
        switch (packet.Command)
        {
            case Command.Connect:
                return HandleConnectPacket(packet);
            case Command.Init:
                return HandleInitPacket(packet);
            case Command.Sync:
                //return HandleSyncPacket(packet);
            default:
                return null;
        }
    }

    private Task<Packet> HandleConnectPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        Packet response = new() { Command = Command.Connect_Resp };
        _logger.LogInformation("Sending {command} to client", response.Command);
        return Task.FromResult(response);
    }

    private async Task<Packet?> HandleInitPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        var doneWithUploading = await HandleUploads(packet);
        Packet? response = null;
        if (doneWithUploading)
        {
            // TODO: add files
            response = new() { Command = Command.Init_Resp };
            _logger.LogInformation("Sending {command} to client", response.Command);
        }
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

        if(packet.File.Size != serverFileSize)
        {
            doneWithUploading = false;
        }

        return doneWithUploading;
        //foreach (var file in packet.Files)
        //{
        //    bool fileContentEqual = _fileManager.CheckIfContentEqual(file);

        //    if (!fileContentEqual)
        //    {
        //        _fileManager.UploadFile(file);
        //    }
        //}
    }
}
