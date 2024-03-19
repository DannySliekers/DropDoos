using DropDoosServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole();

builder.Services.AddHostedService<Server>();
builder.Services.AddSingleton<IFileManager, FileManager>();
builder.Services.AddSingleton<IPacketManager, PacketManager>();

var host = builder.Build();
host.Run();