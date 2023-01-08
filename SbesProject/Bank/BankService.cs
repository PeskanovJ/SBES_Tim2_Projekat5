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
using System.Configuration;

namespace Bank
{
    public class BankService : IBankService
    {
        public bool CheckIfRegistered()
        {
            IIdentity identity = Thread.CurrentPrincipal.Identity;
            WindowsIdentity windowsIdentity = identity as WindowsIdentity;

            string username = Formatter.ParseName(windowsIdentity.Name);

            List<User> usersList = JSONReader.ReadUsers();

            if (usersList != null)
            {
                User user = usersList.Find(u => u.Username == username);

                if (user != null)
                    return true;
            }

            return false;
        }

        public bool Registration(out string encrypted)
        {
            IIdentity identity = Thread.CurrentPrincipal.Identity;
            WindowsIdentity windowsIdentity = identity as WindowsIdentity;

            string username = Formatter.ParseName(windowsIdentity.Name);

            try
            {
                string pin = Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
                pin = pin.Substring(0, 4);

                Console.WriteLine("Registered new user: " + username + " with pin code:" + pin + ".");

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

                encrypted = Manager.RSA.Encrypt(message, certClient.GetRSAPublicKey().ToXmlString(false));

                JSONReader.SaveUser(u);
                try
                {
                    Audit.RegistrationCertSuccess(username); //Try to log successful registration
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Audit] Error: " + e.Message);
                }

                try
                {
                    Program.proxyReplication.SaveDataForUser(u);    //Try to replicate new user info
                    Program.proxyReplication.SaveUserKey(secretKey, username);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Replicator] Error: " + e.Message);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Registration failed: "+e.Message+"\n" + e.StackTrace);
                encrypted = e.Message;
                try
                {
                    Audit.RegistrationCertFailure(username, e.StackTrace); //Try to log failed registration
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                return false;
            }
        }

        public bool Deposit(byte[] encryptedMessage, out byte[] response)
        {
            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            try
            {
                string bankCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
                X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, bankCertCN);

                string clientNameSign = clientName + "_sign";
                X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, clientNameSign);

                if (certificate == null)
                    throw new Exception("Could not find client sign certificate");

                List<User> users = JSONReader.ReadUsers();
                User user = users.Find(u => u.Username == clientName);
                if (user == null)
                    throw new Exception("Client not found in database");

                string secretKey = SecretKey.LoadKey(clientName);
                if (secretKey == null)
                    throw new Exception("Clients secret key could not be loaded");

                byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);
                byte[] signature = new byte[256];
                byte[] messageBytes = new byte[decrypted.Length - 256];

                Buffer.BlockCopy(decrypted, 0, signature, 0, 256);  //Get decrypted signature
                Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256); //Get the rest of the message

                string message = Encoding.UTF8.GetString(messageBytes); //Get Message 

                string bankResponse = "";

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

                        try
                        {
                            Audit.PaymentSuccess(clientName, amount.ToString()); //Try to log successful payment
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"User {clientName} failed to deposit {amount}.");
                        bankResponse = "Failed to deposit money, incorrect pin.";
                        throw new Exception(bankResponse);
                    }
                }
                else
                {
                    Console.WriteLine("Sign is invalid");
                    bankResponse = "Failed to deposit money, certificate sign is invalid.";
                    throw new Exception(bankResponse);
                }

                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

                byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);

                byte[] responseMessage = new byte[256 + bankResponseBytes.Length];

                Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
                Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

                response = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Deposit] ERROR = {0}", e.Message);
                try
                {
                    Audit.PaymentFailure(clientName, e.Message); //Try to log failed payment
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                response = Encoding.UTF8.GetBytes(e.Message);
                return false;
            }
        }

        public bool Withdraw(byte[] encryptedMessage, out byte[] response)
        {
            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            try
            {
                string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
                X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

                string clientNameSign = clientName + "_sign";
                X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, clientNameSign);

                if (certificate == null)
                    throw new Exception("Could not find client sign certificate");

                List<User> users = JSONReader.ReadUsers();
                User user = users.Find(u => u.Username == clientName);
                if (user == null)
                    throw new Exception("Client not found in database");

                string secretKey = SecretKey.LoadKey(clientName);
                if (secretKey == null)
                    throw new Exception("Clients secret key could not be loaded");

                byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);
                byte[] signature = new byte[256];
                byte[] messageBytes = new byte[decrypted.Length - 256];

                Buffer.BlockCopy(decrypted, 0, signature, 0, 256); //Get decrypted signature
                Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256); //Get the rest of the message

                string message = Encoding.UTF8.GetString(messageBytes); //Get Message 

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
                        if (user.Amount > Double.Parse(amount))
                        {
                            user.Amount -= Double.Parse(amount);

                            JSONReader.SaveUser(user);
                            Program.proxyReplication.SaveDataForUser(user);
                            Console.WriteLine($"User {clientName} successfully withdrew {amount}.");
                            bankResponse = $"You successfully withdrew {amount}.";
                        }
                        else
                        {
                            Console.WriteLine($"User {clientName} failed to withdraw {amount}.\nReason: not enough money.");
                            bankResponse = "You dont have enough money on your account.";
                            throw new Exception(bankResponse);
                        }

                        try
                        {
                            Audit.PayoutSuccess(clientName, amount.ToString());  //Try to log successful payout
                            Task.Run(() => CheckLogs(clientName));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"User {clientName} failed to withdraw {amount}.");
                        bankResponse = "Failed to withdraw money, incorrect pin.";
                        throw new Exception(bankResponse);
                    }
                }
                else
                {
                    Console.WriteLine("Sign is invalid");
                    bankResponse = "Failed to withdraw money, certificate sign is invalid.";
                    throw new Exception(bankResponse);
                }

                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

                byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);
                byte[] responseMessage = new byte[256 + bankResponseBytes.Length];
                Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
                Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

                response = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Withdraw] ERROR = {0}", e.Message);
                try
                {
                    Audit.PayoutFailure(clientName, e.Message); //Try to log failed payout
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                response = Encoding.UTF8.GetBytes(e.Message);
                return false;
            }
        }

        public bool ChangePin(byte[] encryptedMessage, out byte[] response)
        {
            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            try
            {
                string srvCertCN = Formatter.ParseName(WindowsIdentity.GetCurrent().Name) + "_sign";
                X509Certificate2 bankCertSign = CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, srvCertCN);

                string clientNameSign = clientName + "_sign";
                X509Certificate2 certificate = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople,
                    StoreLocation.LocalMachine, clientNameSign);

                if (certificate == null)
                    throw new Exception("Could not find client sign certificate");

                List<User> users = JSONReader.ReadUsers();
                User user = users.Find(u => u.Username == clientName);
                if (user == null)
                    throw new Exception("Client not found in database");

                string secretKey = SecretKey.LoadKey(clientName);
                if (secretKey == null)
                    throw new Exception("Clients secret key could not be loaded");

                byte[] decrypted = _3DES_Symm_Algorithm.Decrypt(encryptedMessage, secretKey);
                byte[] signature = new byte[256];
                byte[] messageBytes = new byte[decrypted.Length - 256];

                Buffer.BlockCopy(decrypted, 0, signature, 0, 256);
                Buffer.BlockCopy(decrypted, 256, messageBytes, 0, decrypted.Length - 256);

                string message = Encoding.UTF8.GetString(messageBytes);
                string bankResponse = "";

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

                        try
                        {
                            Audit.ChangePinSuccess(clientName); //Try to log successful pin reset
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"User {clientName} failed to change pin.");
                        bankResponse = "Failed to change pin, old pin is wrong.";
                        throw new Exception(bankResponse);
                    }
                }
                else
                {
                    Console.WriteLine("Sign is invalid");
                    bankResponse = "Failed to change pin, sign is invalid";
                    throw new Exception(bankResponse);
                }

                signature = DigitalSignature.Create(bankResponse, Manager.HashAlgorithm.SHA1, bankCertSign);

                byte[] bankResponseBytes = Encoding.UTF8.GetBytes(bankResponse);
                byte[] responseMessage = new byte[256 + bankResponseBytes.Length];
                Buffer.BlockCopy(signature, 0, responseMessage, 0, 256);
                Buffer.BlockCopy(bankResponseBytes, 0, responseMessage, 256, bankResponseBytes.Length);

                response = _3DES_Symm_Algorithm.Encrypt(responseMessage, secretKey);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[PinReset] ERROR = {0}", e.Message);
                try
                {
                    Audit.ChangePinFailure(clientName, e.Message); //Try to log failed pin reset
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
            string username = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);
            try
            {

                List<User> users = JSONReader.ReadUsers();
                User user = users.Find(u => u.Username == username);
                if (user == null)
                    throw new Exception("Client not found in database");
               
                File.Delete(username + ".pvk");
                File.Delete(username + "_sign.pvk");
                File.Delete(username + ".pfx");
                File.Delete(username + "_sign.pfx");
                File.Delete(username + ".cer");
                File.Delete(username + "_sign.cer");

                try
                {
                    Audit.RevocationCertSuccess(username); //Try to log certificate revocation
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

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

                try
                {
                    Audit.RenewalCertSuccess(username); //Try to log certificate revocation
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

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
                Console.WriteLine("Certificate renew failed: "+e.Message+"\n" + e.StackTrace);
                try
                {
                    Audit.RenewalCertFailure(username,e.Message); //Try to log certificate renewal failure
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine(auditEx.Message);
                }
                return null;
            }
        }

        public void CheckLogs(string clientName)
        {
            int time = Int32.Parse(ConfigurationManager.AppSettings["Time"]);
            int numberOfAccesses = Int32.Parse(ConfigurationManager.AppSettings["NumberOfAccesses"]);

            EventLogEntryCollection events = Audit.customLog.Entries;
            List<EventLogEntry> listEntry = new List<EventLogEntry>();

            foreach (EventLogEntry e in events)
            {
                if (e.EventID == (int)AuditEventTypes.PayoutSuccess)
                {
                    string user = e.Message.Split(' ')[1].Split(' ')[0];

                    if (user == clientName)
                    {
                        listEntry.Add(e);
                    }
                }
            }

            if (listEntry.Count >= numberOfAccesses)
            {
                listEntry = listEntry.OrderByDescending(a => a.TimeGenerated).ToList();

                DateTime dt = listEntry[0].TimeGenerated;
                DateTime dt2 = listEntry[numberOfAccesses - 1].TimeGenerated;

                TimeSpan t = dt.Subtract(dt2);
                double seconds = t.TotalSeconds;

                List<string> logovi = new List<string>();

                if (time >= seconds)
                {
                    for (int i = 0; i < numberOfAccesses; i++)
                    {
                        logovi.Add(listEntry[i].Message);
                    }

                    try
                    {
                        Program.proxyAudit.AccessLog("banka", clientName, dt, logovi);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}
