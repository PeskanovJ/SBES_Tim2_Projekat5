using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BankContracts
{
    [ServiceContract]
    public interface IBankService
    {
        [OperationContract]
        string Registration();

        [OperationContract]
        bool CheckIfRegistered();

        [OperationContract]
        void TestCommunication();

        [OperationContract]
        void Deposit(string message, byte[] sign);

        [OperationContract]
        void Withdraw(string message, byte[] sign);

        [OperationContract]
        void ChangePin(string message, byte[] sign);
    }
}
