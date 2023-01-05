using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
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

        public void TestCommunication()
        {
            try
            {
                factory.TestCommunication();
            }
            catch (Exception e)
            {
                Console.WriteLine("[TestCommunication] ERROR = {0}", e.Message);
            }
        }

        public string Registration()
        {
            string message = factory.Registration();

            if (message == null)
            {
                Console.WriteLine("Registration failed. Try again!");
            }
            else
            {
                Console.WriteLine("Please install your certificates in mmc and press enter to continue.");
                Console.ReadLine();

                string cltCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                X509Certificate2 cert = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, cltCertCN);
                string decrypted = Manager.RSA.Decrypt(message, cert.GetRSAPrivateKey().ToXmlString(true));

                string pin = decrypted.Substring(decrypted.Length - 4, 4);
                string secretKey = decrypted.Substring(0, decrypted.Length - 4);
                SecretKey.StoreKey(secretKey, cltCertCN);
                
                Console.WriteLine("Successful registration! Your PIN code is: " + pin);

                return pin;
            }

            return message;
        }

        public bool CheckIfRegistered()
        {
            return factory.CheckIfRegistered();
        }

        public byte[] Deposit(byte[] encryptedMessage)
        {
            try
            {
                byte[] response = factory.Deposit(encryptedMessage);

                string clientName = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                X509Certificate2 cert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, "bank_sign");
                string secretKey = SecretKey.LoadKey(clientName);

                byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(response, secretKey);

                byte[] signature = new byte[256];
                byte[] messageBytes = new byte[decrypted.Length - 256];

                Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
                Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

                string message = Encoding.UTF8.GetString(messageBytes);

                if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, signature, cert))
                {
                    Console.WriteLine(message);
                    return messageBytes;
                }
                else
                {
                    Console.WriteLine($"Bank sign is invalid.");
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Deposit] ERROR = {0}", e.Message);
                return null;
            }
        }

        public byte[] Withdraw(byte[] encryptedMessage)
        {
            try
            {
                byte[] response = factory.Deposit(encryptedMessage);

                string clientName = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                X509Certificate2 cert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, "bank_sign");
                string secretKey = SecretKey.LoadKey(clientName);

                byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(response, secretKey);
                byte[] signature = new byte[256];
                byte[] messageBytes = new byte[decrypted.Length - 256];
                Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
                Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

                string message = Encoding.UTF8.GetString(messageBytes);

                if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, signature, cert))
                {
                    Console.WriteLine(message);
                    return messageBytes;
                }
                else
                {
                    Console.WriteLine($"Bank sign is invalid.");
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Withdraw] ERROR = {0}", e.Message);
                return null;
            }
        }

        public void ChangePin(string message, byte[] sign)
        {
            try
            {
                factory.ChangePin(message, sign);
            }
            catch (Exception e)
            {
                Console.WriteLine("[TestCommunication] ERROR = {0}", e.Message);
            }
        }

    }
}