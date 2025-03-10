﻿using DropDoosServer;
using DropDoosServer.Managers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using File = DropDoosServer.Data.File;

namespace DropDoosServerTests;

[TestClass]
public class FileManagerTests
{
    private FileManager fileManager;

    [TestInitialize]
    public void Initialize()
    {
        var options = Options.Create(new ServerConfig() { ServerFolder = "D:/DropDoos/TestMap", IpAddress = "", Port = 0});
        var logger = Substitute.For<ILogger<IFileManager>>();
        var clientManager = Substitute.For<IClientManager>();
        fileManager = new FileManager(clientManager, options, logger);
    }

    [TestMethod]
    public void TestGetFile()
    {

        var file = fileManager.GetFile("test.txt", 0, Guid.NewGuid());
        Assert.AreEqual(file.Name, "test.txt");
        Assert.AreEqual(file.Content, "aGFoYQ==");
        Assert.AreEqual(file.Size, 4);
    }

    [TestMethod]
    public void TestGetFileNames()
    {
        var file = fileManager.GetFileNames();
        Assert.AreEqual(file.First(), "test.txt");
    }

    [TestMethod]
    public async Task TestUploadFile()
    {
        if (System.IO.File.Exists("D:\\DropDoos\\TestMap\\testfile.txt"))
        {
            System.IO.File.Delete("D:\\DropDoos\\TestMap\\testfile.txt");
        }

        var file = new File()
        {
            Name = "testfile.txt",
            Content = "dGVzdA==",
            Position = 0,
        };

        var addedFile = await fileManager.UploadFile(file, Guid.NewGuid());
        Assert.AreEqual(addedFile.Name, "testfile.txt");
        Assert.AreEqual(addedFile.Content, "dGVzdA==");
    }
}
