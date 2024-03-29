﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddActor<TActor, TQuery>(this IServiceCollection services)
            where TActor : QueryActor<TQuery>
        {            
            services.AddHostedService<TActor>();
            var inputChannel = Channel.CreateUnbounded<KeyValuePair<Guid, TQuery>>();
            services.AddSingleton(inputChannel.Writer);
            services.AddSingleton(inputChannel.Reader);            
            return services;
        }

        public static IServiceCollection AddAnswerStream(this IServiceCollection services)
        {
            services.AddSingleton<IAnswerStream, AnswerStream>();
            services.AddHostedService(sp => sp.GetRequiredService<IAnswerStream>());
            var answerChannel = Channel.CreateUnbounded<KeyValuePair<Guid, dynamic>>();
            services.AddSingleton(answerChannel.Writer);
            services.AddSingleton(answerChannel.Reader);
            return services;
        }
    }
}
