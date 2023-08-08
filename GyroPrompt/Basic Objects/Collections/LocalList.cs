using GyroPrompt.Basic_Objects.Variables;

namespace GyroPrompt.Basic_Objects.Collections
{
    public enum ArrayType
    {
        String,
        Int,
        Float,
        Boolean
    }
    public class LocalList
    {
        public string Name { get; set; }
        public List<LocalVariable> items = new List<LocalVariable>();
        public int numberOfElements = 0;
        public ArrayType arrayType { get; set; }

        // Internal methods for adding and removing items
        public void itemAdd(LocalVariable item)
        {
            // Check to make sure the LocalVariable 'item' has the same type as the array type to avoid compatibility issues
            if (item.Type == VariableType.String)
            {
                if (arrayType == ArrayType.String)
                {
                    items.Add(item);
                    numberOfElements++;
                } else
                {
                    Console.WriteLine($"Cannot add {item.Name} to non-string list.");
                }
            }

            if (item.Type == VariableType.Int)
            {
                if (arrayType == ArrayType.Int)
                {
                    items.Add(item);
                    numberOfElements++;
                }
                else
                {
                    Console.WriteLine($"Cannot add {item.Name} to non-integer list.");
                }
            }

            if (item.Type == VariableType.Float)
            {
                if (arrayType == ArrayType.Float)
                {
                    items.Add(item);
                    numberOfElements++;
                }
                else
                {
                    Console.WriteLine($"Cannot add {item.Name} to non-float list.");
                }
            }

            if (item.Type == VariableType.Boolean)
            {
                if (arrayType == ArrayType.Boolean)
                {
                    items.Add(item);
                    numberOfElements++;
                }
                else
                {
                    Console.WriteLine($"Cannot add {item.Name} to non-boolean list.");
                }
            }
        }
        public void itemRemove(string name)
        {
            bool itemExists = false;
            foreach (LocalVariable item in items)
            {
                if (item.Name == name)
                {
                    items.Remove(item);
                    itemExists = true;
                    numberOfElements--;
                    break;
                }
            }
            if (itemExists == false) { Console.WriteLine($"{name} does not exist in list."); }
        }
        // Internal method for setting all items to specified value
        public void SetAllWithValue(string value)
        {
            foreach(LocalVariable item in items)
            {
                item.Value = value;
            }
        }
        // Internal method for setting individual item to specified values
        public void SetItemTo(string name, string value)
        {
            foreach(LocalVariable localVariable in items)
            {
                if(localVariable.Name == name)
                {
                    localVariable.Value = value;
                    break;
                }
            }
        }
        // Internal method for returning the value of a item as specific index
        public string GetValueAtIndex(int index)
        {
            string a = "";
            if (index <= numberOfElements)
            {
                a = items[index].Value;
            } else if (index > numberOfElements)
            {
                Console.WriteLine($"Array does not have item at index {index}.");
            }
            return a;
        }
        public string GetNameAtIndex(int index)
        {
            string a = "";
            if (index <= numberOfElements)
            {
                a = items[index].Name;
            }
            else if (index > numberOfElements)
            {
                Console.WriteLine($"Array does not have item at index {index}.");
            }
            return a;
        }
        public string GetValueWithName(string name)
        {
            foreach(LocalVariable localVariable in items)
            {
                if (localVariable.Name == name)
                {
                    Console.WriteLine($"DEBUG: {localVariable.Name}");
                    return localVariable.Value;
                    break;
                }
            }

            return $"{name} not found";
        }
        public void PrintAll()
        {
            Console.WriteLine($"Contents of list {Name}");
            foreach(LocalVariable var in items)
            {
                Console.WriteLine($"Variable name: {var.Name}\tType:{var.Type.ToString()}\tValue: {var.Value}");
            }
        }

        public override string ToString()
        {
            string v = "";
            foreach(LocalVariable var in items)
            {
                v += var.Value;
                if (items.IndexOf(var) != items.Count)
                {
                    v += ", ";
                }
            }
            return v;
        }
    }
}
