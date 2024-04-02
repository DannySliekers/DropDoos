using DropDoosServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DropDoosServer.Managers;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole();

builder.Services.AddOptions<PathConfig>().Bind(builder.Configuration.GetSection("Paths"));
builder.Services.AddSingleton<IClientManager, ClientManager>();
builder.Services.AddHostedService<Server>();
builder.Services.AddSingleton<IFileManager, FileManager>();
builder.Services.AddSingleton<IPacketManager, PacketManager>();

var host = builder.Build();
host.Run();