using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Bank
{
    public class BankService : IBankService
    {
        public bool CheckIfRegistered()
        {
            IIdentity identity = Thread.CurrentPrincipal.Identity;
            WindowsIdentity windowsIdentity = identity as WindowsIdentity;

            string korisnickoIme = Formatter.ParseName(windowsIdentity.Name);

            List<User> usersList = JSONReader.ReadUsers();
           

            if(usersList != null)
            {
                foreach (User u in usersList)
                {
                    if (u.Username == korisnickoIme)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public void TestCommunication()
        {
            Console.WriteLine("Communication established.");
        }

        public string Registration()
        {
            IIdentity identity = Thread.CurrentPrincipal.Identity;
            WindowsIdentity windowsIdentity = identity as WindowsIdentity;

            string username = Formatter.ParseName(windowsIdentity.Name);

            try
            {
                string pin = Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
                pin = pin.Substring(0, 4);

                Console.WriteLine(username + " registered with pin code:" + pin + ".");

                string cmd = "/c makecert -sv " + username + ".pvk -iv RootCA.pvk -n \"CN=" + username + "\" -pe -ic RootCA.cer " + username + ".cer -sr localmachine -ss My -sky exchange";
                System.Diagnostics.Process.Start("cmd.exe", cmd).WaitForExit();

                string cmd2 = "/c pvk2pfx.exe /pvk " + username + ".pvk /pi " + pin + " /spc " + username + ".cer /pfx " + username + ".pfx";
                System.Diagnostics.Process.Start("cmd.exe", cmd2).WaitForExit();

                string cmdSign1 = "/c makecert -sv " + username + "_sign.pvk -iv RootCA.pvk -n \"CN=" + username + "_sign" + "\" -pe -ic RootCA.cer " + username + "_sign.cer -sr localmachine -ss My -sky signature";
                System.Diagnostics.Process.Start("cmd.exe", cmdSign1).WaitForExit();

                string cmdSign2 = "/c pvk2pfx.exe /pvk " + username + "_sign.pvk /pi " + pin + " /spc " + username + "_sign.cer /pfx " + username + "_sign.pfx";
                System.Diagnostics.Process.Start("cmd.exe", cmdSign2).WaitForExit();


                byte[] textData = System.Text.Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHelp = sha256.ComputeHash(textData);

                User u = new User(username, System.Text.Encoding.UTF8.GetString(pinHelp));

                string secretKey = SecretKey.GenerateKey();
               
                SecretKey.StoreKey(secretKey, username);

                JSONReader.SaveUser(u);

                return pin;
            }
            catch (Exception e)
            {
                Console.WriteLine("Registration failed!" + e.StackTrace);
                return null;
            }
        }

        public void Deposit(string message, byte[] sign)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);
            byte[] signature;

            string clienName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clienName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);

            Console.WriteLine(message);
            /// Verify signature using SHA1 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, sign, certificate))
            {
                Console.WriteLine("Sign is valid");
                //proveriti pin kod
                string bankResponse = $"User {clienName} successfully deposited money";
                //string bankResponse = $"Pin is invalid. User {clienName} can't deposite money";
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

            }
            else
            {
                //vratiti poruku da nije moguce uraditi transakciju jer sertifikat nije validan
                Console.WriteLine("Sign is invalid");
                string bankResponse = $"Sign is invalid. User {clienName} can't deposit money";

                //Ovaj potpis kriptovati i vratiti ga kao povratnu vrednost metode
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);
            }
        }

        public void Withdraw(string message, byte[] sign)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);
            byte[] signature;

            string clienName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clienName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);

            Console.WriteLine(message);
            /// Verify signature using SHA1 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, sign, certificate))
            {
                Console.WriteLine("Sign is valid");
                string bankResponse = $"User {clienName} successfully withdrew money";
                //string bankResponse = $"Pin is invalid. User {clienName} can't withdraw money";

                //Ovaj potpis kriptovati i vratiti ga kao povratnu vrednost metode
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);
            }
            else
            {
                Console.WriteLine("Sign is invalid");
                string bankResponse = $"Sign is invalid. User {clienName} can't withdraw money";
                //Ovaj potpis kriptovati i vratiti ga kao povratnu vrednost metode
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);
            }
        }

        public void ChangePin(string message, byte[] sign)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);
            byte[] signature;

            string clienName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clienName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);


            Console.WriteLine(message);
            /// Verify signature using SHA1 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, sign, certificate))
            {
                Console.WriteLine("Sign is valid");
                string bankResponse = $"User {clienName} successfully changed pin";
                //string bankResponse = $"Pin is invalid. User {clienName} can't change pin";
                //Ovaj potpis kriptovati i vratiti ga kao povratnu vrednost metode
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);
            }
            else
            {
                Console.WriteLine("Sign is invalid");
                string bankResponse = $"Sign is invalid. User {clienName} can't change pin";
                //Ovaj potpis kriptovati i vratiti ga kao povratnu vrednost metode
                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);
            }

            //return signature;
        }
    }
}
