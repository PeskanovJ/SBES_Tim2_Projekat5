using BankContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Manager;
using System.Security.Principal;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace Bank
{
    public class BankProxyAudit : ChannelFactory<IBankAudit>, IBankAudit, IDisposable
    {
        IBankAudit factory;

        public BankProxyAudit(NetTcpBinding binding, EndpointAddress address) : base(binding, address)
        {
            factory = this.CreateChannel();
        }

        public void AccessLog(string bank, string account, DateTime accessTime, List<string> logs)
        {
            factory.AccessLog(bank, account, accessTime, logs);
        }
    }
}
