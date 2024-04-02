using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

public interface IFileManager
{
    Task UploadFile(File file, Guid clientId);
    List<string> GetFileNames();
    File GetFile(string fileName, long position, Guid clientId);
    List<string> GetServerEditedFilesForClient(Guid clientId);
}
