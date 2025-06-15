using Microsoft.Extensions.Hosting;
using NATSUnleashed.CSharpIntro;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddNatsServices(builder.Configuration);

var app = builder.Build();

await app.RunAsync();
