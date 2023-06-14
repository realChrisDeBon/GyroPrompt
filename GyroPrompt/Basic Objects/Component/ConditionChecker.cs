using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Component
{
    public enum OperatorTypes
    {
        EqualTo,
        NotEqualTo,
        GreaterThan,
        LessThan,
        EqualToOrGreaterThan,
        EqualToOrLessThan
    }
    public class ConditionChecker
    {
        public IDictionary<string, OperatorTypes> operationsDictionary = new Dictionary<string, OperatorTypes>();
        public void LoadOperations()
        {
            operationsDictionary.Add("=", OperatorTypes.EqualTo);
            operationsDictionary.Add("!=", OperatorTypes.NotEqualTo);
            operationsDictionary.Add(">", OperatorTypes.GreaterThan);
            operationsDictionary.Add("<", OperatorTypes.LessThan);
            operationsDictionary.Add(">=", OperatorTypes.EqualToOrGreaterThan);
            operationsDictionary.Add("<=", OperatorTypes.EqualToOrLessThan);
        }
        public bool ConditionChecked(OperatorTypes operation, string valueComparing, string comparingTo) 
        {
            bool result = false;
            int a = 0;
            int b = 0;
            try
            {
                a = Int32.Parse(valueComparing);
                b = Int32.Parse(comparingTo);
            } catch { // TODO
                    }
            switch (operation)
            {
                case OperatorTypes.EqualTo:
                    if (valueComparing == comparingTo) { result = true; }
                    break;
                case OperatorTypes.NotEqualTo:
                    if (valueComparing != comparingTo) { result = true; }
                    break;
                case OperatorTypes.GreaterThan:
                    if (a > b) { result = true; }
                    break;
                case OperatorTypes.LessThan:
                    if (a < b) { result = true; }
                    break;
                case OperatorTypes.EqualToOrGreaterThan:
                    if (a >= b) { result = true; }
                    break;
                case OperatorTypes.EqualToOrLessThan:
                    if (a <= b) { result = true; }
                    break;
                default:
                    result = false; 
                    break;
            }
            return result;
        }
    }
}
