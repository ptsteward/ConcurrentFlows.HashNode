using ConcurrentFlows.MessageHandling.Hubs;
using ConcurrentFlows.MessageHandling.Messages;
using ConcurrentFlows.MessageHandling.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace ConcurrentFlows.MessageHandling
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ConcurrentFlows.MessageHandling", Version = "v1" });
            });
            services.AddSignalR().AddAzureSignalR("Endpoint=https://xxx.service.signalr.net;AccessKey=xxx;Version=1.0;");
            var connectionString = "Endpoint=sb://xxx.servicebus.windows.net/;SharedAccessKeyName=xxx;SharedAccessKey=xxx";
            var topic = "something";
            services.AddMessenger<EventMessage>(
                publishers: new[]
                {
                    typeof(SampleHubPublisher)
                },
                instances: new[]
                {
                    new ServicebusPublisher<EventMessage>(new TopicClient(connectionString, topic))
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConcurrentFlows.MessageHandling v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SampleHub>("/hub");
            });
        }
    }
}
