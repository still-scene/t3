using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.user.pixtur.examples
{
	[Guid("f02d4cf2-4fb0-4964-8990-2360d2db7979")]
    public class SimpleSoundReactionExample : Instance<SimpleSoundReactionExample>
    {
        [Output(Guid = "476568b4-a53a-4bc0-b25a-8d6fb4e5a923")]
        public readonly Slot<Command> Detection = new();


    }
}
