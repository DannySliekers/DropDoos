using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

internal interface IFileManager
{
    void AddFile(File newFile);
    void UploadFile(File file);
    bool CheckIfFileExists(File file);
    bool CheckIfContentEqual(File file);
    List<File> BuildDownloadList(List<File> fileList);
    List<File> GetFiles();
}
