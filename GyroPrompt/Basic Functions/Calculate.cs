using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Functions
{
    public class Calculate
    {
        public string calculate_string(string equation)
        {
            double solution = MathEvaluator.Evaluate(equation);
            string return_string = solution.ToString();
            return return_string.TrimEnd();
        }

        public int calculate_int(string equation)
        {
            double solution = MathEvaluator.Evaluate(equation);
            int integer_solution = Int32.Parse(solution.ToString());
            return integer_solution;
        }
    }

    public class MathEvaluator
    {
        public static double Evaluate(string equation)
        {
            try
            {
                // Remove white spaces from the equation
                equation = equation.Replace(" ", "");

                // Evaluate the equation using DataTable.Compute method
                var result = new DataTable().Compute(equation, null);

                // Convert the result to double
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Invalid equation.");
                return double.NaN;
            }
        }
    }
}
