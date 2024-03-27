using DropDoosServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DropDoosServer.Managers;
using Microsoft.Extensions.Configuration;
using DropDoosServer.Queue;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole();

builder.Services.AddOptions<PathConfig>().Bind(builder.Configuration.GetSection("Paths"));
builder.Services.AddHostedService<Server>();
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<IFileManager, FileManager>();
builder.Services.AddSingleton<IPacketManager, PacketManager>();

var host = builder.Build();
host.Run();