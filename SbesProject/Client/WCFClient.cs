using BankContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class WCFClient : ChannelFactory<IBankService>, IBankService, IDisposable
    {
        IBankService factory;
        public WCFClient(NetTcpBinding binding, EndpointAddress address) : base(binding, address) 
        {
            factory = this.CreateChannel();
        }

        public string Registration()
        {
            return factory.Registration();
        }
    }
}