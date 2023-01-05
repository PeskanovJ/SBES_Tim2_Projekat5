using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BankContracts
{
    [ServiceContract]
    public interface IReplicatorService
    {
        [OperationContract]
        void SaveDataForUser(User user);
        [OperationContract]
        void SaveUserKey(string key, string username);
    }
}
