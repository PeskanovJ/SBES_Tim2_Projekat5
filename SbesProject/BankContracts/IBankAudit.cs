using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BankContracts
{
    [ServiceContract]
    public interface IBankAudit
    {
        [OperationContract]
        void AccessLog(string bank, string account, DateTime accessTime, List<string> logs);
    }
}
