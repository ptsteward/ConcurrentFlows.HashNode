using Microsoft.Extensions.Hosting;
using NATSUnleashed.MyNatsApp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

await app.RunAsync();
