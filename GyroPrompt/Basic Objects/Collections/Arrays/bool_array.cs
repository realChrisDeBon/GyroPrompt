namespace GyroPrompt.Basic_Objects.Collections.Arrays
{
    public class bool_array : array_baseitem
    {
        List<bool> boolarray = new List<bool>();
        Dictionary<string, bool> booldict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "True", true},
            { "False", false},
            { "1", true},
            { "0", false},
        };
        public bool_array(string name_, List<bool> initialValues)
        {
            Name = name_;
            arrayType = Array_Type.Boolean;
            boolarray = initialValues;
            numberOfElements = boolarray.Count;
        }
        public override void addItem(string value)
        {
            boolarray.Add(booldict[value]);
            numberOfElements = boolarray.Count;
        }
        public override void removeItem(int atIndex)
        {
            boolarray.RemoveAt(atIndex);
            numberOfElements = boolarray.Count;
        }
        public override string getElementAt(int atIndex)
        {
            return boolarray[atIndex].ToString();
        }
    }
}
