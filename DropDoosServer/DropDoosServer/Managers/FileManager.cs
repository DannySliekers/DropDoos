using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal class FileManager : IFileManager
{
    private readonly PathConfig _config;
    private readonly ILogger<IFileManager> _logger;

    public FileManager(IOptions<PathConfig> config, ILogger<IFileManager> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<long> UploadFile(File file)
    {
        try
        {
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
