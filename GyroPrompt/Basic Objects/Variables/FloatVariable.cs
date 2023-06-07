using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Variables
{
    public class FloatVariable : LocalVariable
    {
        private float float_value_;
        public float float_value
        {
            get { return float_value_; }
            set
            {
                float_value_ = value;
                Value = float_value_.ToString();
            }
        }
    }
}
