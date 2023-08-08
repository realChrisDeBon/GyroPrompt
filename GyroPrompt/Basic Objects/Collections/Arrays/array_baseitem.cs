namespace GyroPrompt.Basic_Objects.Collections.Arrays
{
    public enum Array_Type
    {
        String,
        Int,
        Float,
        Boolean,
        None
    }
    public class array_baseitem
    {
        public string Name { get; set; }
        public int numberOfElements { get; set; }
        public Array_Type arrayType { get; set; }

        public virtual void removeItem(int atIndex)
        {

        }
        public virtual void addItem(string value)
        {

        }
        public virtual int getLength()
        {
            return numberOfElements;
        }
        public virtual string getElementAt(int atIndex)
        {
            return "";
        }
    }
}
