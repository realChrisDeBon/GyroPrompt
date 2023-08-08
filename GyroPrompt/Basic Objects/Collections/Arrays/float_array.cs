using System.Globalization;
namespace GyroPrompt.Basic_Objects.Collections.Arrays
{
    public class float_array : array_baseitem
    {
        List<float> floatarray = new List<float>();
        public float_array(string name_, List<float> initialValues)
        {
            Name = name_;
            arrayType = Array_Type.String;
            floatarray = initialValues;
            numberOfElements = floatarray.Count;
        }
        public override void addItem(string value)
        {
            floatarray.Add(float.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            numberOfElements = floatarray.Count;
        }
        public override void removeItem(int atIndex)
        {
            floatarray.RemoveAt(atIndex);
            numberOfElements = floatarray.Count;
        }
        public override string getElementAt(int atIndex)
        {
            return floatarray[atIndex].ToString();
        }
    }
}