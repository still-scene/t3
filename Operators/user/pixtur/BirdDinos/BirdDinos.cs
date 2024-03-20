using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.BirdDinos
{
	[Guid("c5d7b3e2-9030-455e-941f-3c550838b73a")]
    public class BirdDinos : Instance<BirdDinos>
    {
        [Output(Guid = "a5c538bb-8c80-43ec-9e92-eaa59631f9c0")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}
