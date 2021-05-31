using ConcurrentFlows.MessageMultiplexing.Hubs;
using ConcurrentFlows.MessageMultiplexing.Messages;
using ConcurrentFlows.MessageMultiplexing.Model;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.External;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.Internal;
using ConcurrentFlows.MessageMultiplexing.Publisher;
using ConcurrentFlows.MessageMultiplexing.Services;
using ConcurrentFlows.MessagingLibrary.RegistrationExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace ConcurrentFlows.MessageMultiplexing
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ConcurrentFlows.MessageRouter", Version = "v1" });
            });
            services.AddSignalR().AddAzureSignalR("Endpoint=https://xxx.service.signalr.net;AccessKey=xxx;Version=1.0;");
            services.AddMessenger<EntityCreatedMessage>(new[] { typeof(SampleHubPublisher) });
            services.AddMessenger<EntityUpdatedMessage>(new[] { typeof(SampleHubPublisher) });
            services.AddMessenger<EntityDeletedMessage>(new[] { typeof(SampleHubPublisher) });
            services.AddMessageRouter<SampleHubMessageType, SampleEntity, SampleHubInternalMessage>(typeof(SampleHubMessageFactory));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConcurrentFlows.MessageRouter v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
