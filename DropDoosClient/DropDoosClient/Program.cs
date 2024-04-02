using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DropDoosClient;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole();

builder.Services.AddOptions<ClientConfig>().Bind(builder.Configuration.GetSection("Paths"));
builder.Services.AddHostedService<Client>();
builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));

var host = builder.Build();
host.Run();
