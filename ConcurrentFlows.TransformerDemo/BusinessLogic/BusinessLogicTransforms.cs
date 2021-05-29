using System.Collections.Generic;
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
            yield return default;
        }
    }
}
