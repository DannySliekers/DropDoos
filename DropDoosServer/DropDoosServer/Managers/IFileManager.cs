﻿using File = DropDoosServer.Data.File;

namespace DropDoosServer.Managers;

public interface IFileManager
{
    Task UploadFile(File file);
    void ClearTempFolder(string fileName);
    List<string> GetFileNames();
    File GetFile(string fileName, long position);
}
