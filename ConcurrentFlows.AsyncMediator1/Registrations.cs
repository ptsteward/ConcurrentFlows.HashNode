namespace ConcurrentFlows.AsyncMediator1;

public static class Registrations
{
    public static IServiceCollection AddFlow<TSchema>(this IServiceCollection services)
        where TSchema : Envelope
    {
        services.TryAddSingleton<IFlowSource<TSchema>, FlowSource<TSchema>>();
        services.TryAddTransient<IFlowSink<TSchema>>(sp =>
        {
            var source = sp.GetRequiredService<IFlowSource<TSchema>>();
            var linkable = source as ILinkableSource<TSchema>
                ?? throw new ArgumentException(
                    $"Invalid FlowSource Registration. Source Type, {source.GetType().Name}, is not linkable");
            return new FlowSink<TSchema>(linkable);
        });
        return services;
    }
}
