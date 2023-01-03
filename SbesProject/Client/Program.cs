using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            string srvCertCN = "bank";
            X509Certificate2 srvCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, srvCertCN);

            NetTcpBinding binding = new NetTcpBinding();

            string address = "net.tcp://localhost:9999/BankService";
            string addressTransaction = "net.tcp://localhost:9999/TransactionService";

            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            NetTcpBinding bindingTransaction = new NetTcpBinding();
            bindingTransaction.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;

            EndpointAddress endpointAddress = new EndpointAddress(new Uri(address));

            WCFClient proxyWcf = new WCFClient(binding, endpointAddress, true);
            WCFClient CertificateProxy = null;

            if (proxyWcf.CheckIfRegistered())
            {
                EndpointAddress endpointAddressTransaction = new EndpointAddress(new Uri(addressTransaction), new X509CertificateEndpointIdentity(srvCert));
                CertificateProxy = new WCFClient(bindingTransaction, endpointAddressTransaction, false);

                CertificateProxy.TestCommunication(); //provera da li je uspesna autentifikacija preko sertifikata

                UserInterface(CertificateProxy, proxyWcf);

            }
            else
            {

                Console.WriteLine("Do you want to registrate? y/n");
                string answer = Console.ReadLine();

                if (answer.ToLower() == "y")
                {
                    if (proxyWcf.Registration() == null)
                    {
                        return;
                    }
                    else
                    {
                        EndpointAddress endpointAddressTransaction = new EndpointAddress(new Uri(addressTransaction), new X509CertificateEndpointIdentity(srvCert));
                        CertificateProxy = new WCFClient(bindingTransaction, endpointAddressTransaction, false);

                        CertificateProxy.TestCommunication(); //provera da li je uspesna autentifikacija preko sertifikata

                        UserInterface(CertificateProxy, proxyWcf);
                    }
                }
                else if (answer.ToLower() == "n")
                {
                    Console.WriteLine("You rejected registration. Program is shutting down!\nPress any key to exit");
                    Console.ReadLine();
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input!");
                }
            }

            Console.WriteLine("Connection terminated pres any key to exit");
            Console.ReadLine();
        }

        public static void UserInterface(WCFClient CertificateProxy, WCFClient proxyWcf)
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

                //switch
            }
            while (option != "5");
        }

    }
}
