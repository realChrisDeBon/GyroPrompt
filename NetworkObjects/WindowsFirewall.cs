using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt.NetworkObjects
{
    class WindowsFirewall
    {
        public void Initiate()
        {
            Type FWManagerType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");

            // Use the firewall type to create a firewall manager object.
            dynamic FWManager = Activator.CreateInstance(FWManagerType);

            // Check the status of the firewall.
            bool status = FWManager.FirewallEnabled(1);
            Console.WriteLine($"Firewall status: {status}.");
        }
    }
}
