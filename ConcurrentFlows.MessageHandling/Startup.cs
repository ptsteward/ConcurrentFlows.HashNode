using ConcurrentFlows.MessageHandling.Channels;
using ConcurrentFlows.MessageHandling.HostedServices;
using ConcurrentFlows.MessageHandling.Hubs;
using ConcurrentFlows.MessageHandling.Interfaces;
using ConcurrentFlows.MessageHandling.Messages;
using ConcurrentFlows.MessageHandling.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
            services.AddMessenger(new[]
            {
                new ServicebusPublisher<EventMessage>(new TopicClient(connectionString, topic))
            },
            new List<Func<IServiceProvider, IPublisher<EventMessage>>>()
            {
                sp => new SampleHubPublisher(sp.GetRequiredService<IHubContext<SampleHub, ISampleHubClient>>())
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

    public static class RegistrationExtensions
    {
        public static void AddMessenger<TMessage>(
            this IServiceCollection services,
            IEnumerable<IPublisher<TMessage>> publishers = null,
            IEnumerable<Func<IServiceProvider, IPublisher<TMessage>>> publisherFactories = null)
            where TMessage : class
        {
            if ((publishers is null || !publishers.Any()) && (publisherFactories is null || !publisherFactories.Any()))
                throw new ArgumentException($"Must register at least one publisher for {typeof(TMessage).Name}");

            publishers ??= Enumerable.Empty<IPublisher<TMessage>>();
            publisherFactories ??= Enumerable.Empty<Func<IServiceProvider, IPublisher<TMessage>>>();

            foreach (var publisher in publishers)
            {
                services.AddSingleton(publisher);
            }
            foreach (var factory in publisherFactories)
            {
                services.AddSingleton(factory);
            }
            services.AddSingleton<IMessenger<TMessage>, Messenger<TMessage>>();
            services.AddSingleton(typeof(IMessengerWriter<TMessage>), sp => sp.GetRequiredService<IMessenger<TMessage>>());
            services.AddHostedService<BackgroundMessenger<TMessage>>();
        }
    }
}
