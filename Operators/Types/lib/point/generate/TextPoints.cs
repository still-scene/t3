using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.DataTypes;
using T3.Core.Logging;
using PointT3 = T3.Core.DataTypes.Point;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace T3.Operators.Types.Id_bdb41a6d_e225_4a8a_8348_820d45153e3f
{
    public class TextPoints : Instance<TextPoints>
    {
        [Input(Guid = "7432f063-9957-47f7-8250-c3e6456ec7c6")]
        public readonly InputSlot<string> InputText = new InputSlot<string>();

        [Input(Guid = "abe3f777-a33b-4e39-9eee-07cc729acf32")]
        public readonly InputSlot<string> InputFont = new InputSlot<string>();

        [Input(Guid = "12467649-9036-4b47-9868-2b4436580227")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "711edaf8-3fd1-4453-966d-78ed777df934")]
        public readonly InputSlot<float> Resolution = new InputSlot<float>();

        [Input(Guid = "6b378d1a-afc1-4006-ac50-b5b80dc55535")]
        public readonly InputSlot<float> LineHeight = new InputSlot<float>();

        [Input(Guid = "e2cff4bc-6c40-40f1-95a1-9e48d0c8624f", MappedType = typeof(TextAligns))]
        public readonly InputSlot<int> TextAlign = new InputSlot<int>();

        [Input(Guid = "02163610-108e-4154-a3b2-84b65df852fa", MappedType = typeof(VerticalAlignment))]
        public readonly InputSlot<int> VerticalAlign = new InputSlot<int>();

        [Input(Guid = "128bd413-10ab-4c2c-9c49-3de925c26f02", MappedType = typeof(HorizontalAlignment))]
        public readonly InputSlot<int> HorizontalAlign = new InputSlot<int>();

        public TextPoints()
        {
            OutputList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputText = InputText.GetValue(context);
            var inputFont = InputFont.GetValue(context);

            // Check if text and font are not empty
            if (string.IsNullOrEmpty(inputText) || string.IsNullOrEmpty(inputFont))
            {
                return;
            }

            // Check if font file exist
            if (!File.Exists(inputFont))
            {
                Log.Debug($"File {inputFont} doesn't exist", this);
                return;
            }

            // Fetch parameters
            var size = Size.GetValue(context);
            var resolution = Resolution.GetValue(context);
            var appliedSize = size / resolution;
            var lineSpace = LineHeight.GetValue(context);
            var textAlign = (TextAlignment)TextAlign.GetValue(context);
            var vertical = (VerticalAlignment)VerticalAlign.GetValue(context);
            var horizontal = (HorizontalAlignment)HorizontalAlign.GetValue(context);

            // Create font
            FontCollection collection = new();
            var family = collection.Add(inputFont);
            var font = family.CreateFont(resolution);

            TextOptions textOptions = new(font)
                                          {
                                              TextAlignment = textAlign,
                                              VerticalAlignment = vertical,
                                              HorizontalAlignment = horizontal,
                                              LineSpacing = lineSpace,
                                          };

            // Generate points with text from font
            var points = new List<PointT3>();
            var glyphs = TextBuilder.GenerateGlyphs(inputText, textOptions);

            foreach (var glyph in glyphs)
            {

                var simplePaths = glyph.Flatten();
                foreach (var simplePath in simplePaths)
                {
                    // Fill list with points from glyph (and close the path)
                    var count = simplePath.Points.Length;
                    const int offsetForClosedPolygons = 1;
                    for (var pointIndex = 0; pointIndex < count + offsetForClosedPolygons; ++pointIndex)
                    {
                        var pointF = simplePath.Points.Span[pointIndex % count];
                        points.Add(new PointT3
                                       {
                                           Position = new Vector3(pointF.X * appliedSize, -pointF.Y * appliedSize, 0f),
                                           W = 1f,
                                           Orientation = new Quaternion(0f, 0f, 0f, 1f),
                                           Color = new Vector4(1f, 1f, 1f, 1f)
                                       });
                    }

                    // Add line separator
                    points.Add(PointT3.Separator());
                }
            }

            if (points.Count == 0)
                return;

            // Output list
            var list = new StructuredList<PointT3>(points.Count);
            for (var index = 0; index < points.Count; index++) list.TypedElements[index] = points[index];
            OutputList.Value = list;
        }

        private enum TextAligns
        {
            Left,
            Right,
            Center,
        }

        [Output(Guid = "c65da6e8-3eb7-4152-9b79-34fcaaa31807")]
        public readonly Slot<StructuredList> OutputList = new Slot<StructuredList>();
    }
}