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
                return HandleInitSyncPacket(packet, Command.Init_Resp).Result;
            case Command.Sync:
                return HandleInitSyncPacket(packet, Command.Sync_Resp).Result;
            case Command.Download:
                HandleDownloadSyncPacket(packet);
                return null;
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

    private async Task<Packet?> HandleInitSyncPacket(Packet packet, Command command)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        var doneWithUploading = await HandleUploads(packet);
        Packet? response = null;

        if (doneWithUploading)
        {
            response = new() { Command = command };
            _logger.LogInformation("Sending {command} to client", response.Command);
        }

        return response;
    }

    private void HandleDownloadSyncPacket(Packet packet)
    {
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        _fileManager.AddServerFilesToDownloadQueue();
    }

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
