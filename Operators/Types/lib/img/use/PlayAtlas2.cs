using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_48b64c79_b947_49c8_8606_0cf40dc1576d
{
    public class PlayAtlas2 : Instance<PlayAtlas2>
    {
        [Output(Guid = "f365e83a-15d8-4c71-934a-a2d91c40c62d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();


        [Input(Guid = "0d9ab8d3-9710-4828-bf05-258552282b2e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "8bf2c823-125c-4563-ba07-022f2eb4aac9")]
        public readonly InputSlot<int> CountX = new InputSlot<int>();

        [Input(Guid = "78096d4b-eef2-4b9d-b2b7-cb4656e6ab95")]
        public readonly InputSlot<int> CountY = new InputSlot<int>();

        [Input(Guid = "11bf0d1e-643f-4a3e-abd1-fdecf3dfa16a")]
        public readonly InputSlot<int> Truncate = new InputSlot<int>();

        [Input(Guid = "7a624e46-e580-4441-b232-9f53c93cb397")]
        public readonly InputSlot<float> Rate = new InputSlot<float>();
    }
}

