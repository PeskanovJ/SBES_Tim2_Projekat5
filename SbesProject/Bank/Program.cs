using BankContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Bank
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

            ServiceHost host = new ServiceHost(typeof(BankService));
            host.AddServiceEndpoint(typeof(IBankService), binding, address);

            host.Open();
            Console.ReadLine();
        }
    }
}
