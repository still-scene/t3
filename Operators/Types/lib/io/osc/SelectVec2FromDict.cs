using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_96b1e8f3_0b42_4a01_b82b_44ccbd857400
{
    public class SelectVec2FromDict : Instance<SelectVec2FromDict>, ICustomDropdownHolder
    {
        [Output(Guid = "7FF4E818-695D-4FFF-AA23-544D623EADFE")]
        public readonly Slot<Vector2> Result = new();

        
        public SelectVec2FromDict()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            _dict = DictionaryInput.GetValue(context);
            _selectCommand = SelectX.GetValue(context);

            if (_dict == null || _yKey== null)
                return;
            
            if(_dict.TryGetValue(_selectCommand, out var x) && _dict.TryGetValue(_yKey, out var y))
            {
                Result.Value = new Vector2(x, y);
            }
        }

        private Dict<float> _dict;
        private string _selectCommand;

        private string _yKey;
        
        #region select dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return SelectX.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != SelectX.Id || _dict == null)
            {
                yield return "";
                yield break;
            }

            foreach (var key in _dict.Keys)
            {
                yield return key;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            SelectX.SetTypedInputValue(result);
            _yKey= FindKeyForY(result);
        }

        private string FindKeyForY( string xKey)
        {
            var justFoundX = false;
            foreach (var key in _dict.Keys.OrderBy(x => x))
            {
                if (key == xKey)
                {
                    justFoundX = true;
                    continue;
                }

                if (justFoundX)
                {
                    return key;
                }
            }

            return null;
        }
        
        #endregion        
        
        
        
        [Input(Guid = "2f3472cb-4df8-41e7-b20e-1166992bf732")]
        public readonly InputSlot<Dict<float>> DictionaryInput = new();

        [Input(Guid = "c3f9db97-0355-4da7-9571-b806e9f2a191")]
        public readonly InputSlot<string> SelectX = new();
    }
}
