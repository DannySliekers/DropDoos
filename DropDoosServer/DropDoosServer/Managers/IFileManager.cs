using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal interface IFileManager
{
    void UploadFile(File file);
    bool CheckIfContentEqual(File file);
    List<File> BuildDownloadList(List<File> fileList);
}
