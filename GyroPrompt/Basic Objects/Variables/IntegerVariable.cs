﻿
namespace GyroPrompt.Basic_Objects.Variables
{
    public class IntegerVariable : LocalVariable
    {
        private int int_value_;

        public int int_value
        {
            get { return int_value_; }
            set
            {
                int_value_ = value;
                Value = int_value_.ToString();
            }
        }
    }
}