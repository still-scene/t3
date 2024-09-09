using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_25db2a97_38b2_4503_8842_fab3922d7a6c
{
    public class PointTrail : Instance<PointTrail>
    {

        [Output(Guid = "6e3ca38f-78d6-4e2b-b8ab-10a906e058e2")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "4389838c-fd1d-4400-b8e5-f373a05adff7")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "6a2cb3f0-3b5a-4551-a809-3dc172bb7d79")]
        public readonly InputSlot<int> TrailLength = new();

        [Input(Guid = "bcb7260e-8a84-4987-83ca-f31981ae94aa")]
        public readonly InputSlot<bool> IsEnabled = new();

        [Input(Guid = "63621a98-874b-4d1a-9724-f4fa70b8ccf1")]
        public readonly InputSlot<bool> Reset = new();

        [Input(Guid = "56eac471-ad48-41ec-b617-cbcb67646c97")]
        public readonly InputSlot<float> AddSeperatorThreshold = new();
    }
}

