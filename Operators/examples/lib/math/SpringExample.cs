using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.math
{
    [Guid("04b8f508-2101-42dc-8d91-60b585bc561e")]
    public class SpringExample : Instance<SpringExample>
    {
        [Output(Guid = "4381982d-9e95-44d1-a4cb-d6be0cde4ccb")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}
