using BankContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
    public class BankProxyReplication : ChannelFactory<IReplicatorService>, IReplicatorService, IDisposable
    {
        IReplicatorService factory;

        public BankProxyReplication(NetTcpBinding binding, EndpointAddress address) : base(binding, address)
        {
            factory = this.CreateChannel();
        }

        public void SaveDataForUser(User user)
        {
            factory.SaveDataForUser(user);
        }

        public void SaveUserKey(string key, string username)
        {
            factory.SaveUserKey(key, username);
        }
    }
}
