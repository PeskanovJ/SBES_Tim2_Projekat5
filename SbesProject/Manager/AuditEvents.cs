using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    public enum AuditEventTypes
    {
        PaymentSuccess = 0,
        PaymentFailure = 1,
        PayoutSuccess = 2,
        PayoutFailure = 3,
        ChangePinSuccess = 4,
        ChangePinFailure = 5,
        RegistrationCertSuccess = 6,
        RegistrationCertFailure = 7,
        RevocationCertSuccess = 8,
        RevocationCertFailure = 9,
        RenewalCertSuccess = 10,
        RenewalCertFailure = 11,
        RequestTransactionSuccess = 12,
        RequestTransactionFailure = 13
    }

    public class AuditEvents
    {
        private static ResourceManager resourceManager = null;
        private static object resourceLock = new object();

        private static ResourceManager ResourceMgr
        {
            get
            {
                lock (resourceLock)
                {
                    if (resourceManager == null)
                    {
                        resourceManager = new ResourceManager
                            (typeof(AuditEventFile).ToString(),
                            Assembly.GetExecutingAssembly());
                    }
                    return resourceManager;
                }
            }
        }

        public static string PaymentSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.PaymentSuccess.ToString());
            }
        }

        public static string PaymentFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.PaymentFailure.ToString());
            }
        }

        public static string PayoutSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.PayoutSuccess.ToString());
            }
        }

        public static string PayoutFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.PayoutFailure.ToString());
            }
        }

        public static string ChangePinSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.ChangePinSuccess.ToString());
            }
        }

        public static string ChangePinFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.ChangePinFailure.ToString());
            }
        }

        public static string RegistrationCertSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RegistrationCertSuccess.ToString());
            }
        }

        public static string RegistrationCertFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RegistrationCertFailure.ToString());
            }
        }

        public static string RevocationCertSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RevocationCertSuccess.ToString());
            }
        }

        public static string RevocationCertFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RevocationCertFailure.ToString());
            }
        }

        public static string RenewalCertSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RenewalCertSuccess.ToString());
            }
        }

        public static string RenewalCertFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RenewalCertFailure.ToString());
            }
        }

        public static string RequestTransactionSuccess
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RequestTransactionSuccess.ToString());
            }
        }

        public static string RequestTransactionFailure
        {
            get
            {
                return ResourceMgr.GetString(AuditEventTypes.RequestTransactionFailure.ToString());
            }
        }
    }
}
