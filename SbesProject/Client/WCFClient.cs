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

        public bool Registration(out string message)
        {
            try
            {
                bool registration= factory.Registration(out message);

                if (!registration)
                {
                    throw new Exception(message);
                }
                else
                {
                    Console.WriteLine("Certificate installation...");
                    Console.ReadLine();

                    string cltCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                    X509Certificate2 cert = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, cltCertCN);
                    string decrypted = Manager.RSA.Decrypt(message, cert.GetRSAPrivateKey().ToXmlString(true));

                    string pin = decrypted.Substring(decrypted.Length - 4, 4);
                    string secretKey = decrypted.Substring(0, decrypted.Length - 4);
                    SecretKey.StoreKey(secretKey, cltCertCN);

                    Console.WriteLine("Successful registration! Your PIN code is: " + pin);

                    message = decrypted;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Registration] Error: "+ex.Message);
                Console.WriteLine("\n\n The connection with server is terminated. Press any key to exit the program");
                Console.Read();
                message=ex.Message;
                return false;
            }
        }

        public bool CheckIfRegistered()
        {
            return factory.CheckIfRegistered();
        }

        public bool Deposit(byte[] encryptedMessage,out byte[] response)
        {
            try
            {
                if (!factory.Deposit(encryptedMessage, out response))
                    throw new Exception(Encoding.UTF8.GetString(response));
                try
                {
                    Audit.RequestTransactionSuccess("Deposit");  //Try to log transaction success 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
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
                    return true;
                }
                else
                    throw new Exception("Bank sign is invalid");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Deposit] ERROR = {0}", e.Message);
                try
                {
                    Audit.RequestTransactionFailure("Deposit", e.Message);  //Try to log transaction faliure 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                response =Encoding.UTF8.GetBytes(e.Message);
                return false;
            }
        }

        public bool Withdraw(byte[] encryptedMessage,out byte[] response)
        {
            try
            {
                if (!factory.Withdraw(encryptedMessage, out response))
                    throw new Exception(Encoding.UTF8.GetString(response));
                try
                {
                    Audit.RequestTransactionSuccess("Withdraw");  //Try to log transaction success 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
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
                    return true;
                }
                else
                    throw new Exception("Bank sign is invalid");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Withdraw] ERROR = {0}", e.Message);
                try
                {
                    Audit.RequestTransactionFailure("Withdraw", e.Message);  //Try to log transaction faliure 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                response = Encoding.UTF8.GetBytes(e.Message);
                return false;
            }
        }

        public bool ChangePin(byte[] encryptedMessage,out byte[] response)
        {
            try
            {
                if (!factory.ChangePin(encryptedMessage, out response))
                    throw new Exception(Encoding.UTF8.GetString(response));

                try
                {
                    Audit.RequestTransactionSuccess("Pin Reset");  //Try to log transaction success 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }

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
                    return true;
                }
                else
                    throw new Exception("Bank sign is invalid");
            }
            catch (Exception e)
            {
                Console.WriteLine("[ChangePin] ERROR = {0}", e.Message);
                try
                {
                    Audit.RequestTransactionFailure("Pin Reset",e.Message);  //Try to log transaction faliure 
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                response = Encoding.UTF8.GetBytes(e.Message);
                return false;
            }
        }

        public string RenewCertificate()
        {
            string pin = factory.RenewCertificate();

            if(pin == null)
            {
                Console.WriteLine("Certificate renew failed. Try again.");
            }
            else
            {
                Console.WriteLine("Certificate installation...");
                Console.ReadLine();

                string cltCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                X509Certificate2 cert = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, cltCertCN);
                string decryptedPin = RSA.Decrypt(pin, cert.GetRSAPrivateKey().ToXmlString(true));

                Console.WriteLine("Successfully renewed certificate.\nYour new pin code: " + decryptedPin);
            }

            return pin;
        }
    }
}