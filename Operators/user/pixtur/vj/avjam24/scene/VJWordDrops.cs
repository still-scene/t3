using System.Runtime.InteropServices;
using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.avjam24.scene
{
    [Guid("a9f09407-b002-4658-ba91-ed0d10cd43ff")]
    public class VJWordDrops : Instance<VJWordDrops>
    {
        [Output(Guid = "efd7c1d2-61d7-4d67-a3bb-028e3356ee14")]
        public readonly Slot<Command> Output = new Slot<Command>();

    }
}
