﻿using BankContracts;
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

                byte[] textData = System.Text.Encoding.UTF8.GetBytes(pin);
                SHA256Managed sha256 = new SHA256Managed();
                byte[] pinHelp = sha256.ComputeHash(textData);

                User u = new User(username, System.Text.Encoding.UTF8.GetString(pinHelp));

                JSONReader.SaveUser(u);

                return pin;
            }
            catch (Exception e)
            {
                Console.WriteLine("Registration failed!" + e.StackTrace);
                return null;
            }
        }
    }
}
