using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GyroPrompt.Functions
{
    class UserInquiry
    {
        public string Prompt { get; set; }
        public int InquiryType { get; set; }
        public bool UserResponseBool { get; set; }
        public string UserResponseString { get; set; }
        public bool ValidAnswer = false;

        public UserInquiry(string UserPrompt, int Inquiry)
        {
            Prompt = UserPrompt;
            InquiryType = Inquiry;
        }

        public string Response()
        {
            string Response = "";
            switch(InquiryType)
            {
                case 0:
                    Response = YesNoQuestion();
                    break;
                case 1:
                    Response = AskString();
                    break;
                case 2:
                    Response = AskNumber();
                    break;
            }
            return Response;
        }

        private string YesNoQuestion()
        {
            while (ValidAnswer == false)
            {
                Console.Write(Prompt + " (Y/N) ");
                var InputKey = Console.ReadKey();
                switch(InputKey.Key)
                {
                    case ConsoleKey.Y:
                        UserResponseBool = true;
                        UserResponseString = "1"; // 1 = true
                        ValidAnswer = true;
                        break;
                    case ConsoleKey.N:
                        UserResponseBool = false;
                        UserResponseString = "0"; // 0 = false
                        ValidAnswer = true;
                        break;
                }
                Console.WriteLine();
            }
            return UserResponseString;
        }

        private string AskString()
        {
            while (ValidAnswer == false)
            {
                Console.Write(Prompt + ": ");
                var InputString = Console.ReadLine();
                UserResponseString = InputString.ToString();
                ValidAnswer = true;
                Console.WriteLine();
            }
            return UserResponseString;
        }

        private string AskNumber()
        {
            while (ValidAnswer == false)
            {
                Console.Write(Prompt + ": ");
                var InputString = Console.ReadLine();
                UserResponseString = InputString.ToString();
                Console.WriteLine();
                if (UserResponseString.All(char.IsDigit)) { ValidAnswer = true;  } else { Console.WriteLine("Digits only! Try again."); }
            }

            return UserResponseString;
        }
    }
}
