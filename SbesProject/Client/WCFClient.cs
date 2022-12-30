using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class WCFClient : ChannelFactory<IBankService>, IBankService, IDisposable
    {
        IBankService factory;
        public WCFClient(NetTcpBinding binding, EndpointAddress address) : base(binding, address) 
        {
            factory = this.CreateChannel();
        }

        public string Registration()
        {
            string pin = factory.Registration();

            if (pin == null)
            {
                Console.WriteLine("Registration failed. Try again!");
            }
            else
            {
                Console.WriteLine("Certificate installation. Please wait...");
                Thread.Sleep(5000);
                Console.WriteLine("Successful registration! Your PIN code is: " + pin);
            }

            return pin;
        }
    }
}