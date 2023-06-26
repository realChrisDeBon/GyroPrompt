using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Functions
{
    public class TimeDateHandler
    {
        public string returnDateTime(string input)
        {
            StringBuilder datetimeReturned = new StringBuilder();

            DateTime currentTime = DateTime.Now;
            int x = 0;
            for(x = 0; x < input.Length; x++)
            {
                switch (input[x])
                {
                    case 'd':
                        if (input[x + 1] == 'o')
                        {
                            datetimeReturned.Append(currentTime.DayOfWeek);
                            x++;
                        }
                        else
                        {
                            datetimeReturned.Append(currentTime.Day);
                        }
                        break;
                    case 'm':
                        if (input[x + 1] == 'o')
                        {
                            datetimeReturned.Append(currentTime.Month);
                            x++;
                        } else if (input[x + 1] == 'n')
                        {
                            datetimeReturned.Append(currentTime.Minute);
                            x++;
                        }
                        break;
                    case 'y':
                        datetimeReturned.Append(currentTime.Year);
                        break;
                    case 's':
                        datetimeReturned.Append(currentTime.Second);
                        break;
                    case 'h':
                        datetimeReturned.Append(currentTime.Hour);
                        break;
                    default:
                        datetimeReturned.Append(input[x]);
                        break;
                }
            }
            return datetimeReturned.ToString();
        }
    }
}
