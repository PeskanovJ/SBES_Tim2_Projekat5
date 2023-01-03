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
    }
}
