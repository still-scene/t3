using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_1de7b1be_cab6_4beb_a837_4c817562efb2
{
    public class GetPointDataFromList : Instance<GetPointDataFromList>
    {
        [Output(Guid = "84FDAB7B-E9DA-4A15-8EA6-D5E9593C924F")]
        public readonly Slot<Vector3> Position = new ();

        [Output(Guid = "c57f6e99-2a0c-4394-90a9-ebbf2f0e0c38")]
        public readonly Slot<float> W = new();

        [Output(Guid = "46BB03A9-A31E-4416-B38B-7B06D7BCBC47")]
        public readonly Slot<Vector4> Orientation = new ();
        
        [Output(Guid = "5309EF41-30B9-48A7-8F5C-9210FE7ACE50")]
        public readonly Slot<Vector4> Color = new();

        public GetPointDataFromList()
        {
            W.UpdateAction = Update;
            Position.UpdateAction = Update;
            Orientation.UpdateAction = Update;
            Color.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var list = DataList.GetValue(context);
            var index = ItemIndex.GetValue(context);
            if (list is not StructuredList<Point> pointList)
            {
                Log.Warning($"{this} requires a structured point list", this );
                return;
            }

            if (pointList.NumElements == 0)
            {
                Log.Warning($"Point is is empty", this);
                return;
            }

            var point = pointList.TypedElements[index.Mod(pointList.NumElements)];
            Position.Value = point.Position;
            W.Value = point.W;
            Orientation.Value = new Vector4(point.Orientation.X, point.Orientation.Y, point.Orientation.Z, point.Orientation.W);
            Color.Value = point.Color;
        }
        
        [Input(Guid = "e35d2024-704e-42b4-8835-a53fa439a2bc")]
        public readonly InputSlot<int> ItemIndex = new();

        [Input(Guid = "b478510f-eb33-4cf0-be0c-80ecea34e40d")]
        public readonly InputSlot<StructuredList> DataList = new();
    }
}