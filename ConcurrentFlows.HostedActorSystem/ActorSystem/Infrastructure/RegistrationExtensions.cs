using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddActor<TActor, TQuery, TPayload, TAnswer>(this IServiceCollection services)
            where TActor : QueryActor<TQuery, TPayload, TAnswer>
            where TQuery : ActorQuery<TPayload, TAnswer>
        {
            services.AddSingleton<IAnswerStream<TAnswer>, AnswerStream<TAnswer>>();
            services.AddHostedService(sp => sp.GetRequiredService<IAnswerStream<TAnswer>>());
            services.AddHostedService<TActor>();
            var inputChannel = Channel.CreateUnbounded<KeyValuePair<Guid, TQuery>>();
            services.AddSingleton(inputChannel.Writer);
            services.AddSingleton(inputChannel.Reader);
            var answerChannel = Channel.CreateUnbounded<KeyValuePair<Guid, TAnswer>>();
            services.AddSingleton(answerChannel.Writer);
            services.AddSingleton(answerChannel.Reader);
            return services;
        }
    }
}
