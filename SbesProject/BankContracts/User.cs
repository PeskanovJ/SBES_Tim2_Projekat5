using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BankContracts
{
    [DataContract]
    public class User
    {
        
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Pin { get; set; }
        [DataMember]
        public double Amount { get; set; }

        public User()
        {
        }

        public User(string username, string pin)
        {
            Username = username;
            Pin = pin;
            Amount = 0;
        }
    }
}
