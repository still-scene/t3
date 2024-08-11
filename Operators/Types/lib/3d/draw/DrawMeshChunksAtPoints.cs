using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0e313329_74fb_4f2a_b1c2_136e1ecf9b3e
{
    public class DrawMeshChunksAtPoints : Instance<DrawMeshChunksAtPoints>
    {
        [Output(Guid = "aec2ae24-3b64-48ae-a61c-49291829f284", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "40e840c7-b707-4f63-8ae8-bd7498d34f54")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "43527e99-7da1-459a-b51d-0addeeb6786a")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "93561534-b33e-4e72-9601-36216031e5d2")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "8613e460-b7c3-47d7-9bc6-01fff9150ec0")]
        public readonly InputSlot<bool> EnableZWrite = new();

        [Input(Guid = "f758666a-4716-446e-8ad3-a9e7b2a05ff6")]
        public readonly InputSlot<bool> EnableZTest = new();

        [Input(Guid = "f3c88d15-a33e-4d44-bf13-27f76a1e7a50", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        [Input(Guid = "821e86ce-9118-429d-94d8-d30f5df9e455")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "3ba54ff1-0c53-4f8d-92f0-20de48144b82")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> CullMode = new();

        [Input(Guid = "5ff607e0-a3db-4ece-93dc-3713669def9e")]
        public readonly InputSlot<bool> UseWForSize = new();

        [Input(Guid = "010d4e6c-26b8-444e-8d22-3be0d93222ef")]
        public readonly InputSlot<float> AlphaCutOff = new();

        [Input(Guid = "c159c08f-ceef-43b3-8e69-91bfb943c226", MappedType = typeof(FillMode))]
        public readonly InputSlot<int> FillMode = new();

        [Input(Guid = "7d609639-d9f3-4c7c-9fde-91c71a29aedc")]
        public readonly InputSlot<bool> UseStretch = new InputSlot<bool>();
    }
}
