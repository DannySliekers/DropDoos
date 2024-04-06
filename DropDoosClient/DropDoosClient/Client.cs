using DropDoosClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using File = DropDoosClient.Data.File;
using System.IO;
using System.Collections.Concurrent;

namespace DropDoosClient;

internal class Client : IHostedService, IDisposable
{
    private readonly ILogger<Client> _logger;
    private readonly ClientConfig _config;
    private readonly Socket _client;
    private readonly IPEndPoint _endPoint;
    private Timer? _timer;
    private List<string> downloadList;
    private List<string> uploadList;
    private bool uploadCompleted;
    private Guid clientId;
    private readonly ConcurrentQueue<string> _fileQueue;

    public Client(IOptions<ClientConfig> config, ILogger<Client> logger)
    {
        _logger = logger;
        _endPoint = new(IPAddress.Parse(config.Value.IpAddress), config.Value.Port);
        _client = new(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _config = config.Value;
        downloadList = new List<string>();
        uploadList = new List<string>();
        uploadCompleted = false;
        _fileQueue = new ConcurrentQueue<string>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting client");
        CreateOldFileMap();
        _timer = new Timer(Sync, null, TimeSpan.FromSeconds(60), _config.SyncRate);
        await _client.ConnectAsync(_endPoint);

        Packet connect = new() { Command = Command.Connect };
        await Send(connect);

        try
        {
            await Receive(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while receiving");
        }
    }

    private void CreateOldFileMap()
    {
        _logger.LogInformation("Creating backups");
        var oldFilesPath = Path.Combine(_config.ClientFolder, "__oldFiles__");

        if (!Directory.Exists(oldFilesPath))
        {
            Directory.CreateDirectory(oldFilesPath);
        } 
        else
        {
            Directory.Delete(oldFilesPath, true);
            Directory.CreateDirectory(oldFilesPath);
        }

        var currentFiles = Directory.GetFiles(_config.ClientFolder);

        foreach (var file in currentFiles)
        {
            var oldFilesPathFile = Path.Combine(oldFilesPath, Path.GetFileName(file));
            if (!System.IO.File.Exists(oldFilesPathFile))
            {
                System.IO.File.Copy(file, oldFilesPathFile);
            }
        }
    }

    private async void Sync(object? state)
    {
        if (uploadCompleted)
        {
            uploadCompleted = false;
            _logger.LogInformation("Syncing client with server");
            var fileList = GetFileNames();
            var editedFiles = GetEditedFileNames();
            var packet = new Packet() { Command = Command.Sync, ClientId = clientId, FileList = fileList, ClientEditedFiles = editedFiles };
            await Send(packet);
        } 
        else
        {
            _logger.LogInformation("Waiting for init to complete before syncing with server");
        }

    }

    private async Task Receive(CancellationToken cancellationToken)
    {
        using MemoryStream stream = new MemoryStream();

        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = new byte[4096];
            var bytesReceived = await _client.ReceiveAsync(buffer);
            var eomLength = Encoding.UTF8.GetBytes("||DropProto-EOM||").Length;
            var eomIndex = IndexOfEOM(buffer, eomLength);

            if (eomIndex > 0)
            {
                stream.Write(buffer[..eomIndex], 0, eomIndex);
                var packet = Packet.ToPacket(stream.ToArray());
                await HandleReceive(packet);
                stream.SetLength(0);
                stream.Write(buffer[(eomIndex + eomLength)..bytesReceived], 0, bytesReceived - (eomIndex + eomLength));
            }
            else if (bytesReceived > 0)
            {
                stream.Write(buffer[..bytesReceived], 0, bytesReceived);
            }
        }
    }

    private int IndexOfEOM(byte[] buffer, int eomLength)
    {
        //Native String.IndexOf() doesn't really work for the EOM so this is a custom one
        // basically starts searching in the buffer for the bytes 124 and 124 because thats what the EOM starts with
        // and then checks if the remainder of the 124 124 start matches the EOM bytes and returns the index
        // if not found returns -1
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == 124 && (i + eomLength) < buffer.Length && buffer[i + 1] == 124)
            {
                var potentialEomBytes = buffer[i..(i + eomLength)];
                var potentialEomString = Encoding.UTF8.GetString(potentialEomBytes);

                if (potentialEomString.Equals("||DropProto-EOM||"))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private async Task HandleReceive(Packet packet)
    {
        _logger.LogInformation("Client received packet with command: {command}", packet.Command);

        try
        {
            switch (packet.Command)
            {
                case Command.Connect_Resp:
                    await HandleConnectResp(packet);
                    break;
                case Command.Init_Resp:
                    await HandleInitSyncResp(packet);
                    break;
                case Command.Download_Resp:
                    await HandleDownloadResp(packet);
                    break;
                case Command.Upload_Resp:
                    await HandleUploadResp(packet);
                    break;
                case Command.Sync_Resp:
                    await HandleInitSyncResp(packet);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while getting client files");
        }

    }

    private async Task HandleConnectResp(Packet packet)
    {
        clientId = packet.ClientId;
        var fileList = GetFileNames();
        var init = new Packet() { Command = Command.Init, FileList = fileList };
        await Send(init);
    }

    private async Task HandleInitSyncResp(Packet packet)
    {
        downloadList = packet.FileList.Except(GetFileNames()).ToList();
        uploadList = packet.FileList.Except(downloadList).ToList();
        uploadList = uploadList.Concat(packet.ClientEditedFiles).ToList();
        downloadList = downloadList.Concat(packet.ServerEditedFiles).ToList();


        if (downloadList.Count > 0)
        {
            DeleteRedownloadingFile(downloadList.First());
            var downloadPacket = new Packet() { Command = Command.Download, ClientId = clientId, File = new File() { Name = downloadList.First(), Position = 0 } };
            await Send(downloadPacket);
        }
        else if (uploadList.Count > 0)
        {
            await SendNewUploadPacket();
        } 
        else
        {
            uploadCompleted = true;
        }
    }

    private async Task SendNewUploadPacket()
    {
        var fileSize = new FileInfo(Path.Combine(_config.ClientFolder, uploadList.First())).Length;
        var uploadPacket = new Packet()
        {
            Command = Command.Upload,
            ClientId = clientId,
            File = new File()
            {
                Name = uploadList.First(),
                Position = 0,
                Size = fileSize
            }
        };

        await Send(uploadPacket);
    }

    private List<string> GetFileNames()
    {
        var fileNames = new List<string>();
        var fileList = Directory.GetFiles(_config.ClientFolder);

        foreach(var file in fileList)
        {
            fileNames.Add(Path.GetFileName(file));    
        }

        return fileNames;
    }


    private List<string> GetEditedFileNames()
    {
        var editedFiles = new List<string>();
        var oldFiles = Directory.GetFiles(Path.Combine(_config.ClientFolder, "__oldFiles__"));
        var currentFiles = Directory.GetFiles(_config.ClientFolder);

        foreach (var file in oldFiles)
        {
            var fileName = Path.GetFileName(file);
            var matchingCurrentFile = currentFiles.FirstOrDefault(currentFile => Path.GetFileName(currentFile) == fileName);
            if (matchingCurrentFile != null)
            {
                var equalFiles = CompareFiles(file, matchingCurrentFile);

                if (!equalFiles)
                {
                    editedFiles.Add(fileName);
                }

                BackupFile(file, matchingCurrentFile);
            }

        }


        return editedFiles;
    }

    private void BackupFile(string oldFilePath, string currentFilePath)
    {
        System.IO.File.Delete(oldFilePath);
        System.IO.File.Copy(currentFilePath, oldFilePath);
    }

    private bool CompareFiles(string file1Path, string file2Path)
    {
        var file1Size = new FileInfo(file1Path).Length;
        var file2Size = new FileInfo(file2Path).Length;

        if (file1Size != file2Size)
        {
            return false;
        }

        using var fs1 = new FileStream(file1Path, FileMode.Open, FileAccess.Read);
        using var fs2 = new FileStream(file2Path, FileMode.Open, FileAccess.Read);
        int file1byte;
        int file2byte;

        do
        {
            file1byte = fs1.ReadByte();
            file2byte = fs2.ReadByte();
        }
        while ((file1byte == file2byte) && (file1byte != -1));

        return (file1byte - file2byte) == 0;
    }

    private async Task Send(Packet packet)
    {
        var packetBytes = packet.ToByteArray();
        var eom = Encoding.UTF8.GetBytes("||DropProto-EOM||");
        var totalPackage = new byte[packetBytes.Length + eom.Length];

        Array.Copy(packetBytes, 0, totalPackage, 0, packetBytes.Length);
        Array.Copy(eom, 0, totalPackage, packetBytes.Length, eom.Length);

        using var stream = new NetworkStream(_client);
        await stream.WriteAsync(totalPackage, 0, totalPackage.Length);

        _logger.LogInformation("Finished sending: {command}", packet.Command);
    }

    private async Task HandleUploadResp(Packet packet)
    {

        var path = Path.Combine(_config.ClientFolder, packet.File.Name);
        using MemoryStream memoryStream = new MemoryStream();
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var fileSize = new FileInfo(path).Length;
        var position = packet.File.Position;

        if (position != fileSize)
        {
            while (memoryStream.Length < _config.PackageSizeInBytes && memoryStream.Length < fileSize)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                byte[] buffer = new byte[4096];
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                memoryStream.Write(buffer, 0, bytesRead);
                position += bytesRead;

                if (bytesRead == 0)
                {
                    break;
                }
            }

            var fileToSend = new File()
            {
                Name = packet.File.Name,
                Content = Convert.ToBase64String(memoryStream.ToArray()),
                Size = new FileInfo(path).Length,
                Position = position
            };

            var uploadPacket = new Packet() { Command = Command.Upload, ClientId = clientId, File = fileToSend };
            await Send(uploadPacket);
        } 
        else if (uploadList.Count == 1)
        {
            uploadList.Remove(packet.File.Name);
            uploadCompleted = true;
        }
        else
        {
            uploadList.Remove(packet.File.Name);
            await SendNewUploadPacket();
        }
    }

    private async Task FileWriter(string path, long fileSize)
    {
        using FileStream fs = new FileStream(path, FileMode.Append);

        while(new FileInfo(path).Length < fileSize)
        {
            if (_fileQueue.TryDequeue(out var base64Data))
            {
                var data = Convert.FromBase64String(base64Data);
                await fs.WriteAsync(data, 0, data.Length);
                fs.Flush();
            }
        }

        BackupFile(Path.Combine(_config.ClientFolder, "__oldFiles__", Path.GetFileName(path)), path);
    }

    private async Task HandleDownloadResp(Packet packet)
    {
        try
        {
            var path = Path.Combine(_config.ClientFolder, packet.File.Name);

            if (!System.IO.File.Exists(path))
            {
                Task.Run(() => FileWriter(path, packet.File.Size));
            }

            _fileQueue.Enqueue(packet.File.Content);

            if (packet.File.Position != packet.File.Size)
            {
                var downloadPacket = new Packet() { Command = Command.Download, ClientId = clientId, File = new File() {Name = packet.File.Name, Position = packet.File.Position } };
                await Send(downloadPacket);
            } 
            else if (downloadList.Count == 1)
            {
                downloadList.Remove(packet.File.Name);
                _logger.LogInformation("Done with downloading, starting uploading");
                if (uploadList.Count() > 0)
                {
                    var uploadPacket = new Packet() { Command = Command.Upload, ClientId = clientId, File = new File() { Name = uploadList.First(), Position = 0 } };
                    await Send(uploadPacket);
                }
                else
                {
                    uploadCompleted = true;
                }

            }
            else
            {
                downloadList.Remove(packet.File.Name);
                DeleteRedownloadingFile(downloadList.First());
                var downloadPacket = new Packet() { Command = Command.Download, ClientId = clientId, File = new File() { Name = downloadList.First(), Position = 0 } };
                await Send(downloadPacket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while writing to file");
        }
    }

    private void DeleteRedownloadingFile(string fileName)
    {
        var path = Path.Combine(_config.ClientFolder, fileName);

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
