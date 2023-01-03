using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Main host
            NetTcpBinding binding = new NetTcpBinding();
            string address = "net.tcp://localhost:9999/BankService";
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            ServiceHost host = new ServiceHost(typeof(BankService));
            host.AddServiceEndpoint(typeof(IBankService), binding, address);

            //Transaction host
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
            NetTcpBinding bindingTransaction = new NetTcpBinding();
            string addressTransaction = "net.tcp://localhost:9999/TransactionService";
            bindingTransaction.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            ServiceHost hostTransaction = new ServiceHost(typeof(BankService));
            hostTransaction.AddServiceEndpoint(typeof(IBankService), bindingTransaction, addressTransaction);
            hostTransaction.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            hostTransaction.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new ServiceCertValidation();
            hostTransaction.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            hostTransaction.Credentials.ServiceCertificate.Certificate = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

            try
            {
                host.Open();
                hostTransaction.Open();
                Console.WriteLine("WCFService is started.\nPress <enter> to stop ...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("[ERROR] {0}", e.Message);
                Console.WriteLine("[StackTrace] {0}", e.StackTrace);
                Console.ReadLine();
            }
            finally
            {
                host.Close();
                hostTransaction.Close();
            }
        }
    }
}
