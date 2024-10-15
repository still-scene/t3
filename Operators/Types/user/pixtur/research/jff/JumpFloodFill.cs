using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_746076d6_5145_4270_9a39_9480fcfdb5f7
{
    public class JumpFloodFill : Instance<JumpFloodFill>
    {
        [Output(Guid = "fb795895-658c-4b4a-998e-49df91f6c80d")]
        public readonly Slot<Texture2D> Output = new();

        [Input(Guid = "b78645e4-5a94-403f-9f9a-55c3869cf31b")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new();

        [Input(Guid = "51206db3-c66f-4be8-9b2d-8a5cadd63a11")]
        public readonly InputSlot<int> StepCount = new InputSlot<int>();

        [Input(Guid = "1b36ee6b-9ac8-4ef1-aeeb-01728f94f6dd")]
        public readonly InputSlot<bool> TriggerInit = new InputSlot<bool>();
    }
}