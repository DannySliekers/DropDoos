using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
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

    public void UploadFile(File file)
    {
        try
        {
            using FileStream fs = System.IO.File.Create(_config.ServerFolder + "\\" + file.Name);
            byte[] data = Convert.FromBase64String(file.Content);
            fs.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while writing to file");
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
                serverFileList.Add(new File() { Name = Path.GetFileName(file), Content = Convert.ToBase64String(content) });
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
