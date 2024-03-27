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

    public bool CheckIfContentEqual(File file)
    {
        var serverFiles = BuildServerFileList();
        var serverFile = serverFiles.Find(f => f.Name.Equals(file.Name));
        if (serverFile != null)
        {
            return serverFile.Content.Equals(file.Content);
        }
        else
        {
            return false;
        }
    }

    public void AddServerFilesToDownloadQueue()
    {
        string[] clientFolder = Directory.GetFiles(_config.ServerFolder);

        foreach (var file in clientFolder)
        {
            if(_initFileNames.Contains(Path.GetFileName(file)))
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
                        Size = new FileInfo(file).Length
                    };

                    _downloadQueue.Add(fileToSend);
                }
            }
        }
    }

    public List<File> BuildDownloadList(List<File> fileList)
    {
        var serverFiles = BuildServerFileList();
        return serverFiles.Where(f => !fileList.Any(of => of.Name == f.Name)).ToList();
    }

    private List<File> BuildServerFileList()
    {
        try
        {
            var serverFileList = new List<File>();
            var serverFiles = Directory.GetFiles(_config.ServerFolder).ToList();
            foreach (var file in serverFiles)
            {
                var content = System.IO.File.ReadAllBytes(file);
                serverFileList.Add(new File() { Name = Path.GetFileName(file), Content = content, Size = new FileInfo(file).Length });
            }
            return serverFileList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while building server file list");
        }

        return new List<File>();
    }
}
