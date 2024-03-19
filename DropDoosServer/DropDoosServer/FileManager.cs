namespace DropDoosServer;

internal class FileManager : IFileManager
{
    private readonly List<File> _files;
    private const string SERVER_MAP = "";

    public FileManager()
    {
        _files = new List<File>
        {
            new File() { Name = "test.txt", Content = "chest" }
        };
    }

    public void AddFile(File newFile)
    {
        _files.Add(newFile);
    }

    public void UploadFile(File file)
    {
        _files.Find(f => f.Name == file.Name).Content = file.Content;
    }

    public bool CheckIfFileExists(File file)
    {
        return _files.Exists(f => f.Name.Equals(file.Name));
    }

    public bool CheckIfContentEqual(File file)
    {
        var serverFile = _files.Find(f => f.Name.Equals(file.Name));
        if (serverFile != null)
        {
            return serverFile.Content.Equals(file.Content);
        } 
        else
        {
            return false;
        }
    }

    public List<File> BuildDownloadList(List<File> fileList)
    {
        return _files.Where(f => !fileList.Any(of => of.Name == f.Name)).ToList();
    }
}
