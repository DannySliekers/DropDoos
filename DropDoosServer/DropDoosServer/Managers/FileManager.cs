using DropDoosServer.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal class FileManager : IFileManager
{
    private readonly PathConfig _config;
    private readonly ILogger<IFileManager> _logger;
    private readonly List<string> _initFileNames;
    private readonly IDownloadQueue _downloadQueue;

    public FileManager(IDownloadQueue downloadQueue, IOptions<PathConfig> config, ILogger<IFileManager> logger)
    {
        _config = config.Value;
        _logger = logger;
        _initFileNames = new List<string>();
        _downloadQueue = downloadQueue;
    }

    public async Task<long> UploadFile(File file)
    {
        try
        {
            if (!_initFileNames.Contains(file.Name))
            {
                _initFileNames.Add(file.Name);
            }

            var tempPath = _config.ServerFolder + "\\temp\\" + file.Name;
            using FileStream fs = new FileStream(tempPath, FileMode.Append);
            var data = file.Content;
            await fs.WriteAsync(data, 0, data.Length);
            return new FileInfo(tempPath).Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while writing to file");
            return 0;
        }
    }

    public void AddServerFilesToDownloadQueue()
    {
        string[] serverFolder = Directory.GetFiles(_config.ServerFolder);
        int fileNumber = 1;

        foreach (var file in serverFolder)
        {
            if (_initFileNames.Contains(Path.GetFileName(file)))
            {
                continue;
            }

            long position = 0;
            var fileSize = new FileInfo(file).Length;
            while (position <= fileSize)
            {
                if (position == fileSize)
                {
                    break;
                }

                using MemoryStream memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {

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
                        Name = Path.GetFileName(file),
                        Content = memoryStream.ToArray(),
                        Size = new FileInfo(file).Length,
                        FileNumber = fileNumber
                    };

                    int totalNumberOfFiles = Directory.GetFiles(_config.ServerFolder).Except(_initFileNames).Count();
                    _downloadQueue.Add(fileToSend, totalNumberOfFiles);
                }
            }
            fileNumber++;
        }
    }

    public void ClearTempFolder(string fileName)
    {
        var tempPath = _config.ServerFolder + "\\temp\\" + fileName;
        var serverPath = Path.Combine(_config.ServerFolder, fileName);
        System.IO.File.Copy(tempPath, serverPath, true);
        System.IO.File.Delete(tempPath);
    }
}
