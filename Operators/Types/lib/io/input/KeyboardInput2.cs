using System;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;

namespace T3.Operators.Types.Id_e3ddfd41_2d0b_4620_9f61_a201bbd25875
{
    public class KeyboardInput2 : Instance<KeyboardInput2>, IDisposable
    {
        [Output(Guid = "64B9DEC6-2B3A-4D7E-A174-991C8A8B92CE", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> PressedNumber = new();

        

        public KeyboardInput2()
        {
            PressedNumber.UpdateAction = Update;
        }

        private int _lastPressedNumber = 0;

        private void Update(EvaluationContext context)
        {
            int currentZone = Zone.GetValue(context);
            int currentMode = Mode.GetValue(context);
            bool keyPressed = false;
            int newPressedNumber = 0;

            switch (currentZone)
            {
                case 0: // Number row keys (48-57)
                    for (int i = 48; i <= 57; i++)
                    {
                        if (KeyHandler.PressedKeys[i])
                        {
                            newPressedNumber = i - 48;
                            keyPressed = true;
                            break;
                        }
                    }
                    break;

                case 1: // Numpad keys (96-105)
                    for (int i = 96; i <= 105; i++)
                    {
                        if (KeyHandler.PressedKeys[i])
                        {
                            newPressedNumber = i - 96;
                            keyPressed = true;
                            break;
                        }
                    }
                    break;
            }

            switch (currentMode)
            {
                case 0: // Original behavior - value is 0 when key is released
                    PressedNumber.Value = keyPressed ? newPressedNumber : 0;
                    _lastPressedNumber = PressedNumber.Value;
                    break;

                case 1: // Hold value - only update on new key press
                    if (keyPressed && newPressedNumber != _lastPressedNumber)
                    {
                        _lastPressedNumber = newPressedNumber;
                        PressedNumber.Value = newPressedNumber;
                    }
                    else if (!keyPressed)
                    {
                        PressedNumber.Value = _lastPressedNumber;
                    }
                    break;
            }
        }

        private enum Modes
        {
            IsDown,
            KeepActive,
        }
        private enum Zones
        {
            NumRow,
            Numpad,
        }

        [Input(Guid = "2aa9e2e4-1830-4483-88e5-1f2ca50bfe3f", MappedType = typeof(Zones))]
        public readonly InputSlot<int> Zone = new();

        [Input(Guid = "8846aa50-e4d0-433c-9e5b-013a93f17f79", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();
    }
}