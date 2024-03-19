using DropDoosServer.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DropDoosServer;

internal class FileSyncer : IHostedService, IDisposable
{
    private ILogger<FileSyncer> _logger;
    private readonly IFileManager _fileManager;
    private Timer? _timer;
    private const string SERVER_MAP = "D:\\DropDoos\\ServerMap";

    public FileSyncer(IFileManager fileManager, ILogger<FileSyncer> logger)
    {
        _fileManager = fileManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("File syncer starting up");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _logger.LogDebug("Syncing files");
        var files = _fileManager.GetFiles();
        foreach(var file in files)
        {
            try
            {
                using FileStream fs = File.Create(SERVER_MAP + "\\" + file.Name);
                byte[] data = Convert.FromBase64String(file.Content);
                string decodedString = Encoding.UTF8.GetString(data);
                byte[] info = new UTF8Encoding(true).GetBytes(decodedString);
                fs.Write(info, 0, info.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while writing to file");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("File syncer stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
