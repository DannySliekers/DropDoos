using DropDoosServer.Data;
using File = DropDoosServer.Data.File;

namespace DropDoosServer.Queue;

internal interface IDownloadQueue
{
    public void Add(File file, int totalNumberOfFiles);
    public Packet? Get();
}
