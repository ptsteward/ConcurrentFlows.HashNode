namespace ConcurrentFlows.AsyncMediator1;

public abstract record Envelope(
    string CurrentId,
    string CausationId);
