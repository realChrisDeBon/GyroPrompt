using System;
using System.Collections.Generic;
using System.Text;
using BlockIoLib;

namespace GyroPrompt.NetworkObjects
{
    class DogeCoinNetwork
    {
        
        
        public DogeCoinNetwork()
        {

        }

        public void Initialize()
        {
            BlockIo DogeNetwork = new BlockIo("a33f-3011-dcb1-fc68");
            try
            {
                var test = DogeNetwork.GetAddressBalance(new { addresses = "2MwbpV2CDMah58EvnTREjcXAux9BrAYBKyF" });
                if (test.Status == "success")
                {
                    string st = test.Data.available_balance;
                    Console.WriteLine(st);
                }
            } catch
            {
                Console.WriteLine("\nUnable to grab wallet balance.\n");
            }
            
            //Console.WriteLine(DogeNetwork.GetAddressBalance(new { addresses = "2MwbpV2CDMah58EvnTREjcXAux9BrAYBKyF" }).Data);
            
        }

    }
}
