using DropDoosServer.Data;
using DropDoosServer.Managers;
using System.Collections.Concurrent;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Queue;

internal class DownloadQueue : IDownloadQueue
{
    private readonly IFileManager _fileManager;
    private readonly ConcurrentQueue<Packet> _downloadQueue;

    public DownloadQueue(IFileManager fileManager)
    {
        _downloadQueue = new ConcurrentQueue<Packet>();
        _fileManager = fileManager;
    }

    public void Add(File file)
    {
        var totalNumberOfFiles = _fileManager.GetNumberOfFiles();
        var downloadPush = new Packet() { Command = Command.Download_Push, File = file, TotalNumberOfFiles = totalNumberOfFiles };
        _downloadQueue.Enqueue(downloadPush);
    }

    public Packet? Get()
    {
        _downloadQueue.TryDequeue(out var packet);
        return packet;
    }
}
