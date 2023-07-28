using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Collections.Arrays
{
    public class int_array : array_baseitem
    {
        List<int> integerarray = new List<int>();
        public int_array(string name_, List<int> initialValues)
        {
            Name = name_;
            arrayType = Array_Type.Int;
            integerarray = initialValues;
            numberOfElements = integerarray.Count;
        }
        public override void addItem(string value)
        {
            int temp_ = Int32.Parse(value);
            integerarray.Add(temp_);
            numberOfElements = integerarray.Count;
        }
        public override void removeItem(int atIndex)
        {
            integerarray.RemoveAt(atIndex);
            numberOfElements = integerarray.Count;
        }
        public override string getElementAt(int atIndex)
        {
            return integerarray[atIndex].ToString();
        }
    }

}