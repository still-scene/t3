using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_58d86d45_f32d_4ddb_8eab_180161e05b2a
{
    // Todo - deprecate?
    public class MakeResolution : Instance<MakeResolution>
    {
        [Output(Guid = "d1b59769-5f6f-41a3-9cba-4dff05e0a4b6")]
        public readonly Slot<Int2> Size = new();
        
        public MakeResolution()
        {
            Size.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Size.Value= new Int2(Width.GetValue(context), Height.GetValue(context));
        }

        [Input(Guid = "E04CBAAF-D130-4185-9BE7-DAADAFE9D402")]
        public readonly InputSlot<int> Width = new();
        
        [Input(Guid = "419B142A-2C32-4938-8EB8-3706546F543E")]
        public readonly InputSlot<int> Height = new();
        
    }
}
