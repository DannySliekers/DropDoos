using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

public class FileManager : IFileManager
{
    private readonly IClientManager _clientManager;
    private readonly ServerConfig _config;
    private readonly ILogger<IFileManager> _logger;
    private readonly Dictionary<Guid, List<string>> _serverEditedFiles;
    private readonly ConcurrentQueue<string> _fileQueue;

    public FileManager(IClientManager clientManager, IOptions<ServerConfig> config, ILogger<IFileManager> logger)
    {
        _clientManager = clientManager;
        _config = config.Value;
        _logger = logger;
        _serverEditedFiles = new Dictionary<Guid, List<string>>();
        _fileQueue = new ConcurrentQueue<string>();
    }

    public Task<File> UploadFile(File file, Guid clientId)
    {
        var filePath = Path.Combine(_config.ServerFolder, file.Name);
        ManageServerEditedFiles(file, clientId, filePath);

        if (file.Position == 0)
        {
            Task.Run(() => FileWriter(filePath, file.Size));
        } else
        {
            _fileQueue.Enqueue(file.Content);
        }

        return Task.FromResult(file);
    }

    public List<string> GetFileNames()
    {
        var fileNames = new List<string>();
        string[]? fileList;

        try
        {
            fileList = Directory.GetFiles(_config.ServerFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong while getting server files", ex);
            return new List<string>();
        }

        foreach (var file in fileList)
        {
            if(!IsFileLocked(new FileInfo(file)))
            {
                fileNames.Add(Path.GetFileName(file));
            }
        }

        return fileNames;
    }

    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }

    public File GetFile(string fileName, long position, Guid clientId)
    {
        RemoveEditedFileForClient(clientId, fileName);

        var path = Path.Combine(_config.ServerFolder, fileName);
        using MemoryStream memoryStream = new MemoryStream();
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileSize = new FileInfo(path).Length;

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
            Name = Path.GetFileName(fileName),
            Content = Convert.ToBase64String(memoryStream.ToArray()),
            Size = new FileInfo(path).Length,
            Position = position
        };

        return fileToSend;
    }

    public List<string> GetServerEditedFilesForClient(Guid clientId)
    {
        if(_serverEditedFiles.TryGetValue(clientId, out var serverEditedFiles))
        {
            return serverEditedFiles;
        } else
        {
            return new List<string>();
        }
    }

    private void RemoveEditedFileForClient(Guid clientId, string fileName)
    {
        if (_serverEditedFiles.TryGetValue(clientId, out var clientSpecificEditedFiles))
        {
            if (clientSpecificEditedFiles != null && clientSpecificEditedFiles.Contains(fileName))
            {
                _serverEditedFiles[clientId].Remove(fileName);
            }
        }
    }

    private void ManageServerEditedFiles(File file, Guid clientId, string filePath)
    {
        if (file.Position == 0 && System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            var connectedClients = _clientManager.GetClients();
            foreach (var client in connectedClients)
            {
                if (!_serverEditedFiles.ContainsKey(client))
                {
                    _serverEditedFiles.Add(client, new List<string>());
                }

                if (client != clientId)
                {
                    _serverEditedFiles[client].Add(file.Name);
                }
            }
        }
    }

    private async Task FileWriter(string path, long fileSize)
    {
        using FileStream fs = new FileStream(path, FileMode.Append);

        while (new FileInfo(path).Length < fileSize)
        {
            if (_fileQueue.TryDequeue(out var base64Data))
            {
                try
                {
                    var data = Convert.FromBase64String(base64Data);
                    await fs.WriteAsync(data, 0, data.Length);
                    fs.Flush();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Something went wrong while writing to file");
                }
            }
        }
    }
}
