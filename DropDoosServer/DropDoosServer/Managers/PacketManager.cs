using Microsoft.Extensions.Logging;
using DropDoosServer.Data;
using System.Net.Sockets;

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
        _logger.LogInformation("Socket server received message: {command}", packet.Command);
        switch (packet.Command)
        {
            case Command.Connect:
                return HandleConnectPacket();
            case Command.Init:
                return HandleInitPacket(packet);
            case Command.Download:
                return HandleDownloadPacket(packet);
            case Command.Upload:
                return HandleUploadPacket(packet).Result;
            case Command.Sync:
                return HandleSyncPacket(packet);
            default:
                return null;
        }
    }

    private Packet HandleSyncPacket(Packet packet)
    {
        var fileList = PrepareFileList(packet.FileList);
        var response = new Packet() { Command = Command.Sync_Resp, FileList = fileList };
        return response;
    }

    private Packet HandleInitPacket(Packet packet)
    {
        var fileList = PrepareFileList(packet.FileList);
        var response = new Packet() { Command = Command.Init_Resp, FileList = fileList };
        return response;
    }

    private Packet HandleConnectPacket()
    {
        Packet response = new() { Command = Command.Connect_Resp };
        _logger.LogInformation("Sending {command} to client", response.Command);
        return response;
    }

    private Packet HandleDownloadPacket(Packet packet)
    {
        var file = _fileManager.GetFile(packet.File.Name, packet.File.Position);
        var response = new Packet() { Command = Command.Download_Resp, File = file };
        return response;
    }

    private async Task<Packet> HandleUploadPacket(Packet packet)
    {
        await _fileManager.UploadFile(packet.File);
        var response = new Packet() { Command = Command.Upload_Resp };
        return response;
    }

    private List<string> PrepareFileList(List<string> fileList)
    {
        var fileNames = _fileManager.GetFileNames();

        foreach (var file in fileList.ToList())
        {
            if (fileNames.Contains(file))
            {
                fileList.Remove(file);
                fileNames.Remove(file);
            }
        }

        var finalList = fileList.Concat(fileNames).ToList();
        return finalList;
    }
}
