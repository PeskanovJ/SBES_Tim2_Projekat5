using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
        public static string srvCertCN = "bank";
        public static X509Certificate2 srvCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, srvCertCN);
        public static string addressTransaction = "net.tcp://localhost:9999/TransactionService";
        public static NetTcpBinding bindingTransaction = new NetTcpBinding();

        static void Main(string[] args)
        {
            
            NetTcpBinding binding = new NetTcpBinding();
            string address = "net.tcp://localhost:9999/BankService";
            
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            
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
            string signCertCN = Manager.Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
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
                switch (option)
                {
                    case "1":
                        {
                            Console.Write("Enter pin: ");
                            string pin = Console.ReadLine();

                            Console.Write("Enter amount of money to deposit: ");
                            string amount = Console.ReadLine();

                            string depositRequest = amount + '_' + pin;

                            X509Certificate2 certificateSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, signCertCN);
                            byte[] signature = DigitalSignature.Create(depositRequest, HashAlgorithm.SHA1, certificateSign);

                            byte[] depositRequestBytes = System.Text.Encoding.UTF8.GetBytes(depositRequest);

                            byte[] message = new byte[256 + depositRequestBytes.Length];

                            Buffer.BlockCopy(signature, 0, message, 0, 256);
                            Buffer.BlockCopy(depositRequestBytes, 0, message, 256, depositRequestBytes.Length);

                            string username = Manager.Formatter.ParseName(WindowsIdentity.GetCurrent().Name);

                            string secretKey = SecretKey.LoadKey(username);

                            byte[] encryptedMessage = _3DES_Symm_Algorithm.Encrypt(message, secretKey);

                            CertificateProxy.Deposit(encryptedMessage);
                            
                        }
                        break;

                    case "2":
                        {
                            Console.Write("Enter pin: ");
                            string pin = Console.ReadLine();

                            Console.Write("Enter amount of money to withdraw: ");
                            string amount = Console.ReadLine();

                            string withdrawRequest = amount + '_' + pin; 

                            X509Certificate2 certificateSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, signCertCN);
                            byte[] signature = DigitalSignature.Create(withdrawRequest, HashAlgorithm.SHA1, certificateSign);

                            byte[] withdrawRequestBytes = System.Text.Encoding.UTF8.GetBytes(withdrawRequest);
                            byte[] message = new byte[256 + withdrawRequestBytes.Length];
                            Buffer.BlockCopy(signature, 0, message, 0, 256);
                            Buffer.BlockCopy(withdrawRequestBytes, 0, message, 256, withdrawRequestBytes.Length);

                            string username = Manager.Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                            string secretKey= SecretKey.LoadKey(username);

                            byte[] encryptedMessage = _3DES_Symm_Algorithm.Encrypt(message, secretKey);                            

                            CertificateProxy.Withdraw(encryptedMessage);
                        }
                        break;

                    case "3":
                        {
                            Console.Write("Enter pin: ");
                            string pin = Console.ReadLine();

                            Console.Write("Enter new pin: ");
                            string newPin = Console.ReadLine();

                            string withdrawRequest = newPin + '_' + pin;

                            X509Certificate2 certificateSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, signCertCN);
                            byte[] signature = DigitalSignature.Create(withdrawRequest, HashAlgorithm.SHA1, certificateSign);

                            byte[] withdrawRequestBytes = System.Text.Encoding.UTF8.GetBytes(withdrawRequest);
                            byte[] message = new byte[256 + withdrawRequestBytes.Length];
                            Buffer.BlockCopy(signature, 0, message, 0, 256);
                            Buffer.BlockCopy(withdrawRequestBytes, 0, message, 256, withdrawRequestBytes.Length);

                            string username = Manager.Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                            string secretKey = SecretKey.LoadKey(username);

                            byte[] encryptedMessage = _3DES_Symm_Algorithm.Encrypt(message, secretKey);

                            CertificateProxy.ChangePin(encryptedMessage);
                        }
                        break;

                    case "4":
                        {
                            proxyWcf.RenewCertificate();

                            EndpointAddress endpointAddressTransaction = new EndpointAddress(new Uri(addressTransaction), new X509CertificateEndpointIdentity(srvCert));
                            CertificateProxy = new WCFClient(bindingTransaction, endpointAddressTransaction, false);
                        }
                        break;

                    default:
                        break;
                }
            }
            while (option != "5");
        }

    }
}
