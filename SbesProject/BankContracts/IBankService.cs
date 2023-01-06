﻿using System;
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
        byte[] Deposit(byte[] encryptedMessage);

        [OperationContract]
        byte[] Withdraw(byte[] encryptedMessage);

        [OperationContract]
        byte[] ChangePin(byte[] encryptedMessage);

        [OperationContract]
        string RenewCertificate();
    }
}
