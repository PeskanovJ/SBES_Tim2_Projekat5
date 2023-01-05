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
using System.Diagnostics;
using System.IO;
using System.ServiceModel.Channels;

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


                byte[] textData = Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHelp = sha256.ComputeHash(textData);

                User u = new User(username, Encoding.UTF8.GetString(pinHelp));

                string secretKey = SecretKey.GenerateKey();
               
                SecretKey.StoreKey(secretKey, username);

                string message = secretKey + pin;
                X509Certificate2 certClient = CertManager.GetCertificateFromFile(username);

                string encrypted = Manager.RSA.Encrypt(message, certClient.GetRSAPublicKey().ToXmlString(false));

                JSONReader.SaveUser(u);
                //audit za registraciju success
                try
                {
                    Audit.RegistrationCertSuccess(username);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                Program.proxyReplication.SaveDataForUser(u);
                Program.proxyReplication.SaveUserKey(secretKey, username);

                return encrypted;
            }
            catch (Exception e)
            {
                Console.WriteLine("Registration failed!" + e.StackTrace);
                //audit za registraciju failed
                try
                {
                    Audit.RegistrationCertFailure(username, e.StackTrace);
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.Message);
                }
                return null;
            }
        }

        public byte[] Deposit(byte[] encryptedMessage)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clientName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);

            List<User> users = JSONReader.ReadUsers();
            User user = null;

            foreach (User u in users)
            {
                if (u.Username == clientName)
                {
                    user = u;
                    break;
                }
            }

            string secretKey = SecretKey.LoadKey(clientName);
            byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);

            byte[] signature = new byte[256];
            byte[] messageBytes = new byte[decrypted.Length - 256];

            Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
            Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

            string message = Encoding.UTF8.GetString(messageBytes);

            string bankResponse = "";

            /// Verify signature using SHA256 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, signature, certificate))
            {
                Console.WriteLine("Sign is valid");

                string amount = message.Split('_')[0];
                string pin = message.Split('_')[1];

                byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHash = sha256.ComputeHash(pinBytes);

                if (user.Pin == Encoding.UTF8.GetString(pinHash))
                {
                    user.Amount += Double.Parse(amount);
                    JSONReader.SaveUser(user);
                    Program.proxyReplication.SaveDataForUser(user);
                    Console.WriteLine($"User {clientName} successfully deposited {amount}.");
                    bankResponse = $"You successfully deposited {amount}.";

                    //audit za uplatu success
                    try
                    {
                        Audit.PaymentSuccess(clientName, amount.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"User {clientName} failed to deposit {amount}.");
                    bankResponse = "Failed to deposit money.";

                    //audit za uplatu failed
                    try
                    {
                        Audit.PaymentFailure(clientName, "Failed to deposit money, wrong pin");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                //vratiti poruku da nije moguce uraditi transakciju jer sertifikat nije validan
                Console.WriteLine("Sign is invalid");
                bankResponse = $"Sign is invalid. User {clientName} can't deposit money.";

                //audit za uplatu failed
                try
                {
                    Audit.PaymentFailure(clientName, "Failed to deposit money, sign is invalid");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

            byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);

            byte[] responseMessage = new byte[256 + bankResponseBytes.Length];

            Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
            Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

            byte[] encryptedResponse = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);

            return encryptedResponse;
        }

        public byte[] Withdraw(byte[] encryptedMessage)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clientName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);

            List<User> users = JSONReader.ReadUsers();
            User user = null;
            foreach(User u in users)
            {
                if (u.Username == clientName)
                {
                    user = u;
                    break;
                }
            }

            string secretKey = SecretKey.LoadKey(clientName);
            byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);
            
            byte[] signature = new byte[256];
            byte[] messageBytes = new byte[decrypted.Length - 256];
            Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
            Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

            string message=Encoding.UTF8.GetString(messageBytes);

            string bankResponse = "";

            /// Verify signature using SHA1 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, signature, certificate))
            {
                Console.WriteLine("Sign is valid");

                string amount = message.Split('_')[0];
                string pin = message.Split('_')[1];

                byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHash = sha256.ComputeHash(pinBytes);

                if (user.Pin == Encoding.UTF8.GetString(pinHash))
                {
                    if(user.Amount>Double.Parse(amount))
                        user.Amount -= Double.Parse(amount);
                    else
                        bankResponse = $"You dont have enough money on your account.";
                    JSONReader.SaveUser(user);
                    Program.proxyReplication.SaveDataForUser(user);
                    Console.WriteLine($"User {clientName} successfully withdrew {amount}.");
                    bankResponse = $"You successfully withdrew {amount}.";

                    //audit za isplatu success
                    try
                    {
                        Audit.PayoutSuccess(clientName, amount.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"User {clientName} failed to withdraw {amount}.");
                    bankResponse = "Failed to withdraw money.";

                    //audit za isplatu failed
                    try
                    {
                        Audit.PaymentFailure(clientName, "Failed to withdraw money, wrong pin");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Sign is invalid"); 
                bankResponse = $"Sign is invalid. User {clientName} can't withdraw money";

                //audit za uplatu failed
                try
                {
                    Audit.PaymentFailure(clientName, "Failed to withdraw money, sign is invalid");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

            byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);
            byte[] responseMessage = new byte[256 + bankResponseBytes.Length];
            Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
            Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

            byte[] encryptedResponse = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);

            return encryptedResponse;
        }

        public byte[] ChangePin(byte[] encryptedMessage)
        {
            string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
            X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            string clientNameSign = clientName + "_sign";
            X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                StoreLocation.LocalMachine, clientNameSign);

            List<User> users = JSONReader.ReadUsers();
            User user = null;
            foreach (User u in users)
            {
                if (u.Username == clientName)
                {
                    user = u;
                    break;
                }
            }

            string secretKey = SecretKey.LoadKey(clientName);
            byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);

            byte[] signature = new byte[256];
            byte[] messageBytes = new byte[decrypted.Length - 256];
            Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
            Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

            string message = Encoding.UTF8.GetString(messageBytes);

            string bankResponse = "";

            /// Verify signature using SHA1 hash algorithm
            if (DigitalSignature.Verify(message, Manager.HashAlgorithm.SHA1, signature, certificate))
            {
                Console.WriteLine("Sign is valid");

                string newPin = message.Split('_')[0];
                string pin = message.Split('_')[1];

                byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHash = sha256.ComputeHash(pinBytes);

                if (user.Pin == Encoding.UTF8.GetString(pinHash))
                {
                    byte[] newPinBytes = Encoding.UTF8.GetBytes(newPin);
                    byte[] newPinHash = sha256.ComputeHash(newPinBytes);
                    user.Pin = Encoding.UTF8.GetString(newPinHash);
                    JSONReader.SaveUser(user);
                    Program.proxyReplication.SaveDataForUser(user);
                    Console.WriteLine($"User {clientName} successfully changed pin.");
                    bankResponse = $"You successfully changed pin please do not forget it.";

                    //audit za promenu pina success
                    try
                    {
                        Audit.ChangePinSuccess(clientName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"User {clientName} failed to change pin.");
                    bankResponse = "Failed to change pin.";

                    //audit za promenu pina failed
                    try
                    {
                        Audit.ChangePinFailure(clientName, "Failed to change pin, old pin is wrong");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Sign is invalid");
                bankResponse = $"Sign is invalid. User {clientName} can't change pin";

                //audit za promenu pina failed
                try
                {
                    Audit.ChangePinFailure(clientName, "Failed to change pin, sign is invalid");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

            byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);
            byte[] responseMessage = new byte[256 + bankResponseBytes.Length];
            Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
            Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

            byte[] encryptedResponse = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);

            return encryptedResponse;
        }

        public string RenewCertificate()
        {
            string username = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);

            List<User> users = JSONReader.ReadUsers();
            User user = null;

            foreach (User u in users)
            {
                if (u.Username == username)
                {
                    user = u;
                    break;
                }
            }

            try
            { 
                File.Delete(username + ".pvk");
                File.Delete(username + "_sign.pvk");
                File.Delete(username + ".pfx");
                File.Delete(username + "_sign.pfx");
                File.Delete(username + ".cer");
                File.Delete(username + "_sign.cer");

                string pin = Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
                pin = pin.Substring(0, 4);

                Console.WriteLine("Certificate renewed.New pin: " + pin);

                string cmd1 = "/c makecert -sv " + username + ".pvk -iv RootCA.pvk -n \"CN=" + username + "\" -pe -ic RootCA.cer " + username + ".cer -sr localmachine -ss My -sky exchange";
                Process.Start("cmd.exe", cmd1).WaitForExit();

                string cmd2 = "/c pvk2pfx.exe /pvk " + username + ".pvk /pi " + pin + " /spc " + username + ".cer /pfx " + username + ".pfx";
                Process.Start("cmd.exe", cmd2).WaitForExit();

                string cmdSign1 = "/c makecert -sv " + username + "_sign.pvk -iv RootCA.pvk -n \"CN=" + username + "_sign" + "\" -pe -ic RootCA.cer " + username + "_sign.cer -sr localmachine -ss My -sky signature";
                Process.Start("cmd.exe", cmdSign1).WaitForExit();

                string cmdSign2 = "/c pvk2pfx.exe /pvk " + username + "_sign.pvk /pi " + pin + " /spc " + username + "_sign.cer /pfx " + username + "_sign.pfx";
                Process.Start("cmd.exe", cmdSign2).WaitForExit();

                
                byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHash = sha256.ComputeHash(pinBytes);

                user.Pin = Encoding.UTF8.GetString(pinHash);
                JSONReader.SaveUser(user);
                Program.proxyReplication.SaveDataForUser(user);

                X509Certificate2 certClient = CertManager.GetCertificateFromFile(username);

                string encrypted = Manager.RSA.Encrypt(pin, certClient.GetRSAPublicKey().ToXmlString(false));

                return encrypted;
            }
            catch (Exception e)
            {
                Console.WriteLine("Certificate renew failed!" + e.StackTrace);
                return null;
            }
        }
    }
}
