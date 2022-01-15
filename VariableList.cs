using GyroPromptNameSpace;
using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt
{
    public class VariableList : Parser
    {

        public List<Variable> VarList = new List<Variable>();
        public string Name { get; set; }

        public VariableList(string name)
        {
            Name = name;
        }
        
        public void AddItem (Variable obj)
        {
            VarList.Add(obj);
        }

        public void RemoveItem (string varName)
        {
            bool found = false;
            foreach(Variable var in VarList)
            {
                if (var.VarName == varName)
                {
                    VarList.Remove(var);
                    found = true;
                    break;
                }
            }
            if (found == false)
            {
                SendError($"Unable to locate variable {varName} in list {Name}.");
            }
        }

        public void UpdateAll()
        {
            if (VarList.Count != 0)
            {
                foreach (Variable var in VarList)
                {
                    foreach (Variable _var in ActiveVariables)
                    {
                        if (var.VarName == _var.VarName)
                        {
                            var.Message = _var.Message;
                            var.Number = _var.Number;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"List {Name} is empty.");
            }
        }


        public override string ToString()
        {
            int x = 0;
            StringBuilder str = new StringBuilder($"\n{Name}:\n");
            str.Append($"#\tName\tValue\n");
            foreach (Variable var in VarList)
            {
                str.Append($"{x}:\t{var.VarName}\t{var.Message}\n");
                x++;
            }
            if (x == 0) { str.Append("No items!\n"); }
            string a = str.ToString();
            return (a);
        }

    }
}
