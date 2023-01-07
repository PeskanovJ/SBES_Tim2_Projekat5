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
        bool Registration(out string encrypted);

        [OperationContract]
        bool CheckIfRegistered();

        [OperationContract]
        bool Deposit(byte[] encryptedMessage,out byte[] response);

        [OperationContract]
        bool Withdraw(byte[] encryptedMessage,out byte[] response);

        [OperationContract]
        bool ChangePin(byte[] encryptedMessage, out byte[] response);

        [OperationContract]
        string RenewCertificate();
    }
}
