using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Variables
{
    public enum VariableType
    {
        String,
        Int,
        Float
    }
    public class LocalVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public VariableType Type { get; set; }
        public string ToString()
        {
            return Value;
        }
    }
}