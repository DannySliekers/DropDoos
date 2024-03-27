using DropDoosServer.Data;
using System.Collections.Concurrent;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Queue;

internal class DownloadQueue : IDownloadQueue
{
    private readonly BlockingCollection<Packet> _downloadQueue;

    public DownloadQueue()
    {
        _downloadQueue = new BlockingCollection<Packet>();
    }

    public void Add(File file)
    {
        var downloadPush = new Packet() { Command = Command.Download_Push, File = file };
        _downloadQueue.Add(downloadPush);
    }

    public IEnumerable<Packet> Get()
    {
        return _downloadQueue.GetConsumingEnumerable();
    }
}
