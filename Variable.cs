using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPromptNameSpace
{

    public class Variable
    {
        public string VarName { get; set; }
        public bool IsString { get; set; }
        public bool IsInteger { get; set; }
        public int Number { get; set; }
        public string Message { get; set; }
        
        public Variable(bool Type, string Value, string Name)
        {
            if (Type == true) 
            { 
                IsString = true;
                IsInteger = false;
                Message = Value;
            }
            
            if (Type == false) 
            {
                Message = Value;
                IsString = false;
                IsInteger = true;
                Number = Convert.ToInt32(Value);
            }

            VarName = Name;
        }

    }
}
