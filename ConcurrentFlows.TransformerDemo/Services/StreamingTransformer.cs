using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace WebApplication1.Services
{

    public class StreamingTransformer<TInput, TOutput> : IStreamingTransformer<TInput, TOutput>
    {
        public StreamingTransformer(Func<TInput, IAsyncEnumerable<TOutput>> transform)
        {
            Transform = transform ?? throw new ArgumentNullException(nameof(transform));
            Initialize();
            ExecuteComplete = ExecuteAsync();
        }

        private void Initialize()
        {
            var inputChannel = Channel.CreateUnbounded<TInput>();
            var outputChannel = Channel.CreateUnbounded<TOutput>();
            Input = inputChannel.Reader;
            Source = inputChannel.Writer;
            ResultsSource = outputChannel.Writer;
            Results = outputChannel.Reader;
        }

        public Task ExecuteComplete { get; private set; }
        public ChannelReader<TOutput> Results { get; private set; }
        public ChannelWriter<TInput> Source { get; private set; }

        private ChannelReader<TInput> Input { get; set; }
        private ChannelWriter<TOutput> ResultsSource { get; set; }
        private Func<TInput, IAsyncEnumerable<TOutput>> Transform { get; set; }

        protected async Task ExecuteAsync()
        {
            while (!Input.Completion.IsCompleted)
            {
                await foreach (var input in Input.ReadAllAsync())
                {
                    await foreach (var output in Transform(input))
                    {
                        await ResultsSource.WriteAsync(output);
                    }
                }
                ResultsSource.Complete();
            }
        }
    }
}
