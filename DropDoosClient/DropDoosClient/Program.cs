using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DropDoosClient;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole();

builder.Services.AddOptions<PathOptions>().Bind(builder.Configuration.GetSection("Paths"));
builder.Services.AddHostedService<Client>();

var host = builder.Build();
host.Run();
