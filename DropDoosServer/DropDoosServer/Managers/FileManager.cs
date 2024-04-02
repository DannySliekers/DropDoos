using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

public class FileManager : IFileManager
{
    private readonly IClientManager _clientManager;
    private readonly PathConfig _config;
    private readonly ILogger<IFileManager> _logger;
    private readonly Dictionary<Guid, List<string>> _serverEditedFiles;

    public FileManager(IClientManager clientManager, IOptions<PathConfig> config, ILogger<IFileManager> logger)
    {
        _clientManager = clientManager;
        _config = config.Value;
        _logger = logger;
        _serverEditedFiles = new Dictionary<Guid, List<string>>();
    }

    public async Task UploadFile(File file, Guid clientId)
    {
        var filePath = Path.Combine(_config.ServerFolder, file.Name);
        ManageServerEditedFiles(file, clientId, filePath);

        try
        {
            using FileStream fs = new FileStream(filePath, FileMode.Append);
            var data = Convert.FromBase64String(file.Content);
            await fs.WriteAsync(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while writing to file");
        }
    }

    public List<string> GetFileNames()
    {
        var fileNames = new List<string>();
        var fileList = Directory.GetFiles(_config.ServerFolder);

        foreach (var file in fileList)
        {
            fileNames.Add(Path.GetFileName(file));
        }

        return fileNames;
    }

    public File GetFile(string fileName, long position, Guid clientId)
    {
        RemoveEditedFileForClient(clientId, fileName);

        var path = Path.Combine(_config.ServerFolder, fileName);
        using MemoryStream memoryStream = new MemoryStream();
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var fileSize = new FileInfo(path).Length;

        while (memoryStream.Length < 100_000_000 && memoryStream.Length < fileSize)
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
            Size = new FileInfo(path).Length
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
}
