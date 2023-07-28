using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Collections.Arrays
{
    public class str_array : array_baseitem
    {
        List<string> stringarray = new List<string>();
        public str_array(string name_, List<string> initialValues)
        {
            Name = name_;
            arrayType = Array_Type.String;
            stringarray = initialValues;
            numberOfElements = stringarray.Count;

        }
        public override void addItem(string value)
        {
            stringarray.Add(value);
            numberOfElements = stringarray.Count;
        }
        public override void removeItem(int atIndex)
        {
            stringarray.RemoveAt(atIndex);
            numberOfElements = stringarray.Count;
        }
        public override string getElementAt(int atIndex)
        {
            return stringarray[atIndex];
        }
    }
}