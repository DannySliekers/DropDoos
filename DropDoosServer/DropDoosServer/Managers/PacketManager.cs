using Microsoft.Extensions.Logging;
using DropDoosServer.Data;

namespace DropDoosServer.Managers;

public class PacketManager : IPacketManager
{
    private readonly ILogger<IFileManager> _logger;
    private readonly IFileManager _fileManager;
    private readonly IClientManager _clientManager;

    public PacketManager(IFileManager fileManager, ILogger<IFileManager> logger, IClientManager clientManager)
    {
        _logger = logger;
        _fileManager = fileManager;
        _clientManager = clientManager;
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
                return HandleUploadPacket(packet);
            case Command.Sync:
                return HandleSyncPacket(packet);
            case Command.Disconnect:
                return HandleDisconnect(packet);
            default:
                return null;
        }
    }

    private Packet HandleDisconnect(Packet packet)
    {
        _clientManager.DisconnectClient(packet.ClientId);
        var response = new Packet() { Command = Command.Disconnect_Resp };
        return response;
    }

    private Packet HandleSyncPacket(Packet packet)
    {
        var fileList = PrepareFileList(packet.FileList);
        var serverEditedFiles = _fileManager.GetServerEditedFilesForClient(packet.ClientId);
        var response = new Packet() { Command = Command.Sync_Resp, FileList = fileList, ClientEditedFiles = packet.ClientEditedFiles, ServerEditedFiles = serverEditedFiles };
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
        var clientId = _clientManager.ConnectClient();
        Packet response = new() { Command = Command.Connect_Resp, ClientId = clientId };
        _logger.LogInformation("Sending {command} to client", response.Command);
        return response;
    }

    private Packet HandleDownloadPacket(Packet packet)
    {
        var file = _fileManager.GetFile(packet.File.Name, packet.File.Position, packet.ClientId);
        var response = new Packet() { Command = Command.Download_Resp, File = file };
        return response;
    }

    private Packet HandleUploadPacket(Packet packet)
    {
        var uploadedFile = _fileManager.UploadFile(packet.File, packet.ClientId).Result;
        uploadedFile.Content = null;
        var response = new Packet() { Command = Command.Upload_Resp, File = uploadedFile };
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
