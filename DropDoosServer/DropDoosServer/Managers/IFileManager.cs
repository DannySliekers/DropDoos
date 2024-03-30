using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal interface IFileManager
{
    Task<long> UploadFile(File file);
    void AddServerFilesToDownloadQueue();
    int GetNumberOfFiles();
}
