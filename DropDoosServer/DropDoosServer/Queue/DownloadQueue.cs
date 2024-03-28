using DropDoosServer.Data;
using System.Collections.Concurrent;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Queue;

internal class DownloadQueue : IDownloadQueue
{
    private readonly ConcurrentQueue<Packet> _downloadQueue;

    public DownloadQueue()
    {
        _downloadQueue = new ConcurrentQueue<Packet>();
    }

    public void Add(File file)
    {
        var downloadPush = new Packet() { Command = Command.Download_Push, File = file };
        _downloadQueue.Enqueue(downloadPush);
    }

    public Packet? Get()
    {
        _downloadQueue.TryDequeue(out var packet);
        return packet;
    }
}
