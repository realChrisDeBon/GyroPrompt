using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Variables
{
    public class StringVariable : LocalVariable
    {
        private string str_value_;
        
        public string str_value
        {
            get { return str_value_; }
            set
            {
                str_value_ = value;
                Value = str_value_;
            }
        }
    }
}
