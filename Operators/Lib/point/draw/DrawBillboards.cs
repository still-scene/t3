using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace Operators.lib.point.draw
{
	[Guid("18d3d929-e530-45fa-9131-658368060ae2")]
    public class DrawBillboards : Instance<DrawBillboards>
    {
        [Output(Guid = "363d4ef2-c8c9-4785-8848-2ea930457959")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "8f203c5f-6eb8-42a1-bfea-9ca52a49e132")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "7df44a8d-af20-400f-b8a7-4b2200f55ec1")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new();

        [Input(Guid = "3247ff54-e470-466a-a70c-cdba1e264f6b")]
        public readonly InputSlot<bool> UseWForScale = new();

        [Input(Guid = "4763d03c-7fae-4466-870c-693bb1acb9a0")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new();

        [Input(Guid = "909bdbcd-620c-479e-b150-cb7adfbfffe8", MappedType = typeof(Orientations))]
        public readonly InputSlot<int> Orientation = new();

        [Input(Guid = "14e0d722-9441-4bc0-b164-e0b8cae990e3")]
        public readonly InputSlot<float> RotateZ = new();

        [Input(Guid = "f9c698ae-3b59-4f1d-8497-c668f5037795")]
        public readonly InputSlot<System.Numerics.Vector3> RotationAxis = new();

        [Input(Guid = "a9e64a2e-6a88-4c36-9e02-95ca710ea86e")]
        public readonly InputSlot<float> Randomize = new();

        [Input(Guid = "8cd25011-5305-4bdf-9307-a46e52b5c503")]
        public readonly InputSlot<float> RandomPhase = new();

        [Input(Guid = "76c52e1f-1767-4635-86a1-93e44cc2a487")]
        public readonly InputSlot<System.Numerics.Vector3> RandomPosition = new();

        [Input(Guid = "80edea41-7f4e-492e-bf73-270df9b225be")]
        public readonly InputSlot<float> RandomRotate = new();

        [Input(Guid = "640e0f51-e636-406b-ba64-8746096fee36")]
        public readonly InputSlot<float> RandomScale = new();

        [Input(Guid = "211c4e91-e667-4cb7-a7b2-81081cd780cb")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "6ac9265b-c6ae-47dc-a605-993d685edf90", MappedType = typeof(DistributionModes))]
        public readonly InputSlot<int> ColorVariationMode = new();

        [Input(Guid = "dd345d58-09e4-4353-93e7-c20e576f0e82")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> ColorVariations = new();

        [Input(Guid = "975f7ed8-fc7f-41c7-9d90-d022efdf4b6b")]
        public readonly InputSlot<bool> UseRotationAsRgba = new();

        [Input(Guid = "d9dd77b2-cfa6-429c-b87d-f6e3c602be23", MappedType = typeof(DistributionModes))]
        public readonly InputSlot<int> ScaleDistribution = new();

        [Input(Guid = "48440453-8b59-4e84-beec-8766713c5775")]
        public readonly InputSlot<T3.Core.DataTypes.Curve> Scales = new();

        [Input(Guid = "19e10456-76bd-43a2-9c03-5f6dd93601cc")]
        public readonly InputSlot<float> SpreadLength = new();

        [Input(Guid = "0544aca0-8110-40dc-a6bf-b35d8e7bd57a")]
        public readonly InputSlot<float> SpreadPhase = new();

        [Input(Guid = "00331030-624f-47e4-b6ce-d24388bf8fb6")]
        public readonly InputSlot<bool> SpreadPingPong = new();

        [Input(Guid = "b32f7932-68a6-4a12-931b-a49d24951f27")]
        public readonly InputSlot<bool> SpreadRepeat = new();

        [Input(Guid = "aff03303-8f28-4efb-a035-d0784790ee42")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new();

        [Input(Guid = "face85cc-2405-4486-80ee-b1323118cce7", MappedType = typeof(DistributionModes))]
        public readonly InputSlot<int> AtlasMode = new();

        [Input(Guid = "261faf50-c63e-4d4a-a496-648bbb3ea2c5")]
        public readonly InputSlot<Int2> AtlasSize = new();

        [Input(Guid = "789900fa-6d0e-406f-936e-162834155c83")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxTexture = new();

        [Input(Guid = "b5477de9-4c59-47e8-89d6-93c9e2246dcd", MappedType = typeof(FxTextureModes))]
        public readonly InputSlot<int> FxTextureMode = new();

        [Input(Guid = "2e116e07-ee18-4b2a-a708-b0dca510a8dd")]
        public readonly InputSlot<System.Numerics.Vector4> FxTextureAmount = new();

        [Input(Guid = "860b2023-9c77-47aa-9a9b-8fbc735835d4", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new();

        [Input(Guid = "62c511b9-ca36-48db-a9cf-13c929f81db6")]
        public readonly InputSlot<bool> EnableDepthWrite = new();

        [Input(Guid = "c676823b-97d0-4e3e-a59e-e2297816ec69")]
        public readonly InputSlot<float> AlphaCut = new();

        [Input(Guid = "a129bf5c-e549-4f7c-b30b-5199ffd38f8c")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();
        
        private enum Orientations
        {
            Billboard,
            RotatedBillboard,
            PointRotation,
        }
        
        private enum DistributionModes
        {
            RandomWithPhase,
            Scatter,
            Spread,
            UseW,
            UseFogDistance,
        }

        private enum ScaleModes
        {
            Multiply,
            Add,
            Override,
        }

        private enum FxTextureModes
        {
            UseColor,
            UseAs_RotateScaleScatter,
        }
    }
}
