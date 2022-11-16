using FluentAssertions;
using System.Threading.Channels;

namespace Concurrentflows.AsyncMediator.Tests;

public class SystemChannelsTests
{
    [Fact]
    public async Task MultipleChannels_MultiplePipes()
    {
        var channel1 = Channel.CreateUnbounded<int>();
        var channel2 = Channel.CreateUnbounded<int>();
        var range = Enumerable.Range(0, 1000);
        var isEven = (int i) => i % 2 == 0;
        var ch1Set = new List<int>();
        var ch2Set = new List<int>();

        (var reader1, var reader2) = await ReadAndWriteAll(
            range, isEven,
            channel1, channel2,
            ch1Set, ch2Set);

        await Task.WhenAny(reader1, reader2);

        var evens = range.Where(i => isEven(i)).ToArray();
        var odds = range.Where(i => !isEven(i)).ToArray();
        ch1Set.Should().BeEquivalentTo(evens);
        ch2Set.Should().BeEquivalentTo(odds);
    }

    private async Task<(Task reader1, Task reader2)> ReadAndWriteAll(
        IEnumerable<int> range,
        Func<int, bool> isEven,
        Channel<int> channel1,
        Channel<int> channel2,
        ICollection<int> ch1Set,
        ICollection<int> ch2Set)
    {
        Task reader1;
        Task reader2;
        using (var cts = new CancellationTokenSource())
        {

            reader1 = Task.Run(async () =>
            {
                await foreach (var i in channel1.Reader.ReadAllAsync(cts.Token))
                    ch1Set.Add(i);
            });


            reader2 = Task.Run(async () =>
            {
                await foreach (var i in channel2.Reader.ReadAllAsync(cts.Token))
                    ch2Set.Add(i);
            });

            var writingTasks = range.Select(async i =>
            {
                await Task.Yield();
                if (isEven(i))
                    await channel1.Writer.WriteAsync(i);
                else
                    await channel2.Writer.WriteAsync(i);
            });
            await Task.WhenAll(writingTasks);
            cts.Cancel();
        }
        return (reader1, reader2);
    }
}
