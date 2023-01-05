using BankContracts;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicator
{
    internal class ReplicatorService : IReplicatorService
    {
        public void SaveDataForUser(User user)
        {
            JSONReader.SaveUser(user);
            Console.WriteLine("[" + DateTime.Now.ToString() + "] User created succesfully!");
            Console.WriteLine("\t Username: " + user.Username);
            Console.WriteLine("\t Amount: " + user.Amount);
            Console.WriteLine("\t PIN: " + user.Pin);
        }

        public void SaveUserKey(string key, string username)
        {
            SecretKey.StoreKey(key, username);
        }
    }
}
