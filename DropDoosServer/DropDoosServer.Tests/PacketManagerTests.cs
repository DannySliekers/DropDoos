using NSubstitute;
using DropDoosServer.Managers;
using DropDoosServer.Data;
using Microsoft.Extensions.Logging;
using File = DropDoosServer.Data.File;

namespace DropDoosServerTests;

[TestClass]
public class PacketManagerTests
{
    private PacketManager _packetManager;

    [TestInitialize]
    public void Initialize()
    {
        var fileManagerMock = Substitute.For<IFileManager>();
        var loggerMock = Substitute.For<ILogger<IFileManager>>();
        var clientManager = Substitute.For<IClientManager>();
        _packetManager = new PacketManager(fileManagerMock, loggerMock, clientManager);
    }

    [TestMethod]
    public void TestHandleConnect()
    {
        var packet = new Packet() { Command = Command.Connect };
        var response = _packetManager.HandlePacket(packet);
        Assert.AreEqual(response.Command, Command.Connect_Resp);
    }

    [TestMethod]
    public void TestHandleInit()
    {
        var fileManagerMock = Substitute.For<IFileManager>();
        fileManagerMock.GetFileNames().Returns(new List<string>());
        var loggerMock = Substitute.For<ILogger<IFileManager>>();
        var clientManager = Substitute.For<IClientManager>();
        _packetManager = new PacketManager(fileManagerMock, loggerMock, clientManager);
        var packet = new Packet() { Command = Command.Init, FileList = new List<string>() };
        var response = _packetManager.HandlePacket(packet);
        Assert.AreEqual(response.Command, Command.Init_Resp);
    }

    [TestMethod]
    public void TestHandleDownload()
    {
        var packet = new Packet() { Command = Command.Download, File = new File() { Name = "test", Position = 0 } };
        var response = _packetManager.HandlePacket(packet);
        Assert.AreEqual(response.Command, Command.Download_Resp);
    }

    [TestMethod]
    public void TestHandleUpload()
    {
        var packet = new Packet() { Command = Command.Upload, File = new File() { Name = "test", Position = 0 } };
        var response = _packetManager.HandlePacket(packet);
        Assert.AreEqual(response.Command, Command.Upload_Resp);
    }

    [TestMethod]
    public void TestHandleSync()
    {
        var fileManagerMock = Substitute.For<IFileManager>();
        fileManagerMock.GetFileNames().Returns(new List<string>());
        var loggerMock = Substitute.For<ILogger<IFileManager>>();
        var clientManager = Substitute.For<IClientManager>();
        _packetManager = new PacketManager(fileManagerMock, loggerMock, clientManager);
        var packet = new Packet() { Command = Command.Sync, FileList = new List<string>() { "Test" } };
        var response = _packetManager.HandlePacket(packet);
        Assert.AreEqual(response.Command, Command.Sync_Resp);
    }
}
