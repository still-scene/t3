using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.user.pixtur.dailies
{
	[Guid("0b124106-8f93-4323-9791-53c799a24585")]
    public class Daily_Mai23 : Instance<Daily_Mai23>
    {
        [Output(Guid = "b25aa2c2-3628-482e-819e-85b5eea1bbd2")]
        public readonly Slot<Texture2D> Output = new();


    }
}
