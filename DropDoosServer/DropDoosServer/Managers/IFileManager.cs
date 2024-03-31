using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal interface IFileManager
{
    Task<long> UploadFile(File file);
    void AddServerFilesToDownloadQueue();
    void ClearTempFolder(string fileName);
}
