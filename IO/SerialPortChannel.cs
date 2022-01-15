using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using GyroPromptNameSpace;
using System.Threading;

namespace GyroPrompt.IO
{
    public class SerialPortChannel : Parser 
    {
        public SerialPort srl_port = new SerialPort();
        public List<string> ActiveCommands = new List<string>();
        public string Designated_Serial_Buffer { get; set; }
        public bool Reading = true;
        public string PortName { get; set; }
        public bool RunCommands = true;

        public SerialPortChannel(string port_name, int baudrate, string designated_serialbuffer)
        {
            srl_port.PortName = port_name;
            PortName = port_name;
            srl_port.BaudRate = baudrate;
            Designated_Serial_Buffer = designated_serialbuffer;
            srl_port.DataReceived += (sender, e) =>
            {
                //do a foreach loop after framework finished
                string data = srl_port.ReadLine().ToString();
                Console.WriteLine(data);
                Console.Beep(1000, 1000);
            };
        }
        
        public void Initialize()
        {
            srl_port.DataBits = 8;
            srl_port.ParityReplace = 63;
            srl_port.ReadTimeout = -1;
            srl_port.ReadBufferSize = 2048;
            
            srl_port.Open();
            Thread thread = (new Thread(ReadPort));
            thread.Start();
        }

        public void SendData(string message)
        {
            srl_port.WriteLine(message);
        }

        public void ReadPort()
        {
            while (Reading == true)
            {
                string a = srl_port.ReadLine().ToString();
                if (RunCommands == true)
                {
                    foreach (Variable var in ActiveVariables)
                    {
                        if (var.VarName == Designated_Serial_Buffer)
                        {
                            var.Message = a;
                            Console.WriteLine(a);
                        }
                    }
                }
  
            }
        }

        public void AddCommand(string command)
        {  
            ActiveCommands.Add(command);
        }

        public void Toggle()
        {
            if (RunCommands == true)
            {
                RunCommands = false;
            } else
            {
                RunCommands = true;
            }
        }
    }
}
