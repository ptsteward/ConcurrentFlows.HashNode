using System.Collections.Generic;

namespace WebApplication1.Services
{
    public static class TransformerExtensions
    {
        public static async IAsyncEnumerable<TOutput> SubmitAndReturnResults<TInput, TOutput>(this Transformer<TInput, TOutput> transformer, TInput input)
        {
            await transformer.Source.WriteAsync(input);
            transformer.Source.Complete();
            while (await transformer.Results.WaitToReadAsync())
            {
                yield return await transformer.Results.ReadAsync();
            }
            await transformer.ExecuteComplete;
        }
    }
}
