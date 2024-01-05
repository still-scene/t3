using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.user.pixtur.examples
{
	[Guid("2872d93c-882e-42ee-8bcf-747c24f9b042")]
    public class FadingFaces : Instance<FadingFaces>
    {
        [Output(Guid = "65cc5be1-6daa-4602-a0f7-88bf30f592f5")]
        public readonly Slot<Texture2D> Output = new();


    }
}
