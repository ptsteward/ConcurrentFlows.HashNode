﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcurrentFlows.AzureBusSeries.Part4;

public static class Extensions
{
    public static Task CompleteOnCancelAsync(
        this CancellationToken token)
    {
        var tcs = new TaskCompletionSource();
        token.Register(t =>
        {
            if (t is TaskCompletionSource tcs)
                tcs.SetResult();
        }, tcs);
        return tcs.Task;
    }

    [return: NotNullIfNotNull(nameof(item))]
    public static T ThrowIfNull<T>(
        [NotNull] this T? item,
        [CallerArgumentExpression(nameof(item))] string? paramName = null)
    {
        if (item is null)
            throw new ArgumentNullException(paramName);
        return item;
    }
}
