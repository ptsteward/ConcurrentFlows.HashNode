using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.BusinessLogic
{
    public static class BusinessLogicTransforms
    {
        public static async IAsyncEnumerable<DataOutput> TransformDataInputToDataOutput(DataInput input)
        {
            foreach (var c in input.Payload)
            {
                await Task.Delay(500);
                yield return new DataOutput()
                {
                    Result = c
                };
            }
            if (!input.Payload.Any())
                yield return default;
        }
    }
}
