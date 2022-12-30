using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetTcpBinding binding = new NetTcpBinding();

            string address = "net.tcp://localhost:9999/BankService";

            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            EndpointAddress endpointAddress = new EndpointAddress(new Uri(address));

            WCFClient proxyWcf = new WCFClient(binding, endpointAddress);

            string answer;
            do
            {
                Console.WriteLine("Do you want to registrate? y/n");
                answer = Console.ReadLine();

                if (answer.ToLower() == "y")
                {
                    if (proxyWcf.Registration() == null)
                    {
                        return;
                    }
                    else
                    {
                        UserInterface();
                    }
                }
                else if (answer.ToLower() == "n")
                {
                    Console.WriteLine("You rejected registration. Program is shutting down!");
                    Console.ReadLine();
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input!");
                }
            } while (answer.ToLower() != "y" && answer.ToLower() != "n");

            Console.ReadLine();
        }

        public static void UserInterface()
        {
            string option;

            do
            {
                Console.WriteLine("Choose an option: ");
                Console.WriteLine("\t1. Payment");
                Console.WriteLine("\t2. Payout");
                Console.WriteLine("\t3. Change PIN code");
                Console.WriteLine("\t4. Renew certificate");
                Console.WriteLine("\t5. The end");
                Console.Write("Your option: ");
                option = Console.ReadLine();
            }
            while (option != "5");
        }

    }
}
