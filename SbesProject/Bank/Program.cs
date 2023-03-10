using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
    internal class Program
    {
        public static BankProxyReplication proxyReplication = null;
        public static BankProxyAudit proxyAudit = null;
        static void Main(string[] args)
        {
            //Main host
            NetTcpBinding binding = new NetTcpBinding();
            string address = "net.tcp://localhost:9999/BankService";
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            binding.OpenTimeout = new TimeSpan(0, 10, 0);
            binding.CloseTimeout = new TimeSpan(0, 10, 0);
            binding.SendTimeout = new TimeSpan(0, 10, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
            ServiceHost host = new ServiceHost(typeof(BankService));
            host.AddServiceEndpoint(typeof(IBankService), binding, address);

            ServiceSecurityAuditBehavior newAudit = new ServiceSecurityAuditBehavior(); //logovanje se vrsi sa ova hosta, pa u oba dodajemo novo podesavanje za audit
            //oba hosta u smislu da se loguju transakcije sa hostTransaction i izdavanje sertifikata za host-a
            newAudit.AuditLogLocation = AuditLogLocation.Application;
            newAudit.ServiceAuthorizationAuditLevel = AuditLevel.SuccessOrFailure;
            host.Description.Behaviors.Remove<ServiceSecurityAuditBehavior>();
            host.Description.Behaviors.Add(newAudit);

            //Transaction host
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
            NetTcpBinding bindingTransaction = new NetTcpBinding();
            string addressTransaction = "net.tcp://localhost:9999/TransactionService";
            bindingTransaction.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            bindingTransaction.OpenTimeout = new TimeSpan(0, 10, 0);
            bindingTransaction.CloseTimeout = new TimeSpan(0, 10, 0);
            bindingTransaction.SendTimeout = new TimeSpan(0, 10, 0);
            bindingTransaction.ReceiveTimeout = new TimeSpan(0, 10, 0);

            ServiceHost hostTransaction = new ServiceHost(typeof(BankService));
            hostTransaction.AddServiceEndpoint(typeof(IBankService), bindingTransaction, addressTransaction);
            hostTransaction.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            hostTransaction.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new ServiceCertValidation();
            hostTransaction.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            hostTransaction.Credentials.ServiceCertificate.Certificate = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);
            //Za audit podesavanja
            hostTransaction.Description.Behaviors.Remove<ServiceSecurityAuditBehavior>();
            hostTransaction.Description.Behaviors.Add(newAudit);

            //Replication proxy
            NetTcpBinding bindingReplication = new NetTcpBinding();
            string addressReplication = "net.tcp://localhost:9997/ReplicatorService";

            bindingReplication.Security.Mode = SecurityMode.Transport;
            bindingReplication.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            bindingReplication.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            EndpointAddress endpointAddress = new EndpointAddress(new Uri(addressReplication));
            proxyReplication = new BankProxyReplication(binding, endpointAddress);

            //Audit service
            NetTcpBinding bindingAudit = new NetTcpBinding();
            string bankAuditAddress = "net.tcp://localhost:9996/BankingAuditServis";

            bindingAudit.Security.Mode = SecurityMode.Transport;
            bindingAudit.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            bindingAudit.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            EndpointAddress auditEndpointAddress = new EndpointAddress(new Uri(bankAuditAddress));
            proxyAudit = new BankProxyAudit(bindingAudit, auditEndpointAddress);

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
