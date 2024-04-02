using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

public class FileManager : IFileManager
{
    private readonly PathConfig _config;
    private readonly ILogger<IFileManager> _logger;

    public FileManager(IOptions<PathConfig> config, ILogger<IFileManager> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task UploadFile(File file)
    {
        if(file.Position == 0)
        {
            System.IO.File.Delete(Path.Combine(_config.ServerFolder, file.Name));
        }

        try
        {
            var path = Path.Combine(_config.ServerFolder, file.Name);
            using FileStream fs = new FileStream(path, FileMode.Append);
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

    public File GetFile(string fileName, long position)
    {
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
}
