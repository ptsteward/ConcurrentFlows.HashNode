using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentFlows.AzureBusSeries.Part4.AppModel;

public interface IMessageHandler<T>
{
    public Task HandleAsync(T message, CancellationToken cancelToken);
}
