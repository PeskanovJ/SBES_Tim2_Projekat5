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
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class WCFClient : ChannelFactory<IBankService>, IBankService, IDisposable
    {
        IBankService factory;
        public WCFClient(NetTcpBinding binding, EndpointAddress address, bool wcf) : base(binding, address) 
        {
            if (!wcf)
            {
                string cltCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);

                this.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
                this.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = new ClientCertValidator();
                this.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
                this.Credentials.ClientCertificate.Certificate = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, cltCertCN);
            }

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

        public bool CheckIfRegistered()
        {
            return factory.CheckIfRegistered();
        }
    }
}