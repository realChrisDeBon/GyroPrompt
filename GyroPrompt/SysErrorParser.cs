using GyroPrompt.Basic_Objects.GUIComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GyroPrompt
{
    public  class SysErrorParser
    {
        public Parser topLevelParser;
        public ConsoleOutputDirector GUIConsole;

        public readonly Dictionary<int, string> errorCategory = new Dictionary<int, string>
        {
            { 10, "Statement not recognized: "},
            { 11, "Incorrect format for " },
            { 12, "Could not find or locate " },
            { 13, "Name already in use: " },
            { 14, "Invalid value for " },
            { 15, "Expecting two values separated by comma: " },
            { 16, "Variable or object name may only contain letters and numbers. Invalid name: "},
            { 17, "Missing requirement: " },
            { 18, "GUI mode must be on."},
            { 19, "GUI mode must be off."},
            { 20, "Missing parameter: " },
            { 21, "Wrong type of variable or object: " },
            { 22, "Script must be running for this command to execute." },
            { 23, "Must terminate text read with a vertical pipe |" }
        };
        public void ThrowError(int errorCode, string failedItem = "object", string missingObject = "object", string badValue = "bad value", string expectedParameter = "parameter", string expectedFormat = "format")
        {
            string firstTwo = errorCode.ToString();
            int errCategory = int.Parse(firstTwo.Substring(0, 2));

            string outputMessage = "";
            switch (errCategory)
            {
                case 10:
                    outputMessage += errorCategory[errCategory] + badValue;
                    break;
                case 11:
                    outputMessage += errorCategory[errCategory] + failedItem + ". Expected syntax: " + expectedFormat;
                    break;
                case 12:
                    outputMessage += errorCategory[errCategory] + missingObject;
                    break;
                case 13:
                    outputMessage += errorCategory[errCategory] + badValue;
                    break;
                case 14:
                    outputMessage += errorCategory[errCategory] + failedItem + ", cannot take " + badValue + ", expecting: " + expectedParameter;
                    break;
                case 15:
                    outputMessage += errorCategory[errCategory] + expectedParameter;
                    break;
                case 16:
                    outputMessage += errorCategory[errCategory] + failedItem;
                    break;
                case 17:
                    outputMessage += errorCategory[errCategory] + missingObject;
                    break;
                case 18:
                    outputMessage = errorCategory[errCategory];
                    break;
                case 19:
                    outputMessage = errorCategory[errCategory];
                    break;
                case 20:
                    outputMessage += errorCategory[errCategory] + expectedParameter + ". Expected syntax: " + expectedFormat;
                    break;
                case 21:
                    outputMessage += errorCategory[errCategory] + badValue + ", expecting: " + expectedParameter;
                    break;
                case 22:
                    outputMessage += errorCategory[errCategory];
                    break;
                case 23:
                    outputMessage += errorCategory[errCategory];
                    break;
            }
            
            if (topLevelParser.GUIModeEnabled == false)
            {
                Console.WriteLine(outputMessage);
            } else
            {
                // Error will be printed into a message box prompt in console director
                try
                {
                    Terminal.Gui.Application.MainLoop.Invoke(() =>
                    {
                        GUIConsole.error_msgbox("Error!", outputMessage);
                    });
                }
                catch
                {
                    // um
                }
            }

            
        }

    }
}
