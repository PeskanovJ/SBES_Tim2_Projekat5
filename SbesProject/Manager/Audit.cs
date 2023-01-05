using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    public class Audit : IDisposable
    {

       private static EventLog customLog = null;
       const string SourceName = "Manager.Audit";
       const string LogName = "AuditSBES";

       static Audit()
       {
            try
            {
                if (!EventLog.SourceExists(SourceName))
                {
                    EventLog.CreateEventSource(SourceName, LogName);
                }
                customLog = new EventLog(LogName,
                    Environment.MachineName, SourceName);
            }
            catch (Exception e)
            {
                customLog = null;
                Console.WriteLine("Error while trying to create log handle. Error = {0}", e.Message);
            }
       }

       public static void PaymentSuccess(string userName, string amount)
       {
            if (customLog != null)
            {
                string PaymentSuccess =
                    AuditEvents.PaymentSuccess;
                string message = String.Format(PaymentSuccess,
                    userName, amount);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.PaymentSuccess));
            }
       }

       public static void PaymentFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string PaymentFailure =
                    AuditEvents.PaymentFailure;
                string message = String.Format(PaymentFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.PaymentFailure));
            }
       }

        public static void PayoutSuccess(string userName, string amount)
       {
            if (customLog != null)
            {
                string PayoutSuccess =
                    AuditEvents.PayoutSuccess;
                string message = String.Format(PayoutSuccess,
                    userName, amount);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.PayoutSuccess));
            }
       }

       public static void PayoutFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string PayoutFailure =
                    AuditEvents.PayoutFailure;
                string message = String.Format(PayoutFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.PayoutFailure));
            }
       }

        public static void ChangePinSuccess(string userName)
       {
            if (customLog != null)
            {
                string ChangePinSuccess =
                    AuditEvents.ChangePinSuccess;
                string message = String.Format(ChangePinSuccess,
                    userName);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.ChangePinSuccess));
            }
       }

       public static void ChangePinFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string ChangePinFailure =
                    AuditEvents.ChangePinFailure;
                string message = String.Format(ChangePinFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.ChangePinFailure));
            }
       }

        public static void RegistrationCertSuccess(string userName)
       {
            if (customLog != null)
            {
                string RegistrationCertSuccess =
                    AuditEvents.RegistrationCertSuccess;
                string message = String.Format(RegistrationCertSuccess,
                    userName);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RegistrationCertSuccess));
            }
       }

       public static void RegistrationCertFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string RegistrationCertFailure =
                    AuditEvents.RegistrationCertFailure;
                string message = String.Format(RegistrationCertFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RegistrationCertFailure));
            }
       }

        public static void RevocationCertSuccess(string userName)
       {
            if (customLog != null)
            {
                string RevocationCertSuccess =
                    AuditEvents.RevocationCertSuccess;
                string message = String.Format(RevocationCertSuccess,
                    userName);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RevocationCertSuccess));
            }
       }

       public static void RevocationCertFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string RevocationCertFailure =
                    AuditEvents.RevocationCertFailure;
                string message = String.Format(RevocationCertFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RevocationCertFailure));
            }
       }

        public static void RenewalCertSuccess(string userName)
       {
            if (customLog != null)
            {
                string RenewalCertSuccess =
                    AuditEvents.RenewalCertSuccess;
                string message = String.Format(RenewalCertSuccess,
                    userName);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RenewalCertSuccess));
            }
       }

       public static void RenewalCertFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string RenewalCertFailure =
                    AuditEvents.RenewalCertFailure;
                string message = String.Format(RenewalCertFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RenewalCertFailure));
            }
       }

        public static void RequestTransactionSuccess(string transactionName)
       {
            if (customLog != null)
            {
                string RequestTransactionSuccess =
                    AuditEvents.RequestTransactionSuccess;
                string message = String.Format(RequestTransactionSuccess,
                    transactionName);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RequestTransactionSuccess));
            }
       }

       public static void RequestTransactionFailure(string userName, string reason)
       {
            if (customLog != null)
            {
                string RequestTransactionFailure =
                    AuditEvents.RequestTransactionFailure;
                string message = String.Format(RequestTransactionFailure,
                    userName, reason);
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.RequestTransactionFailure));
            }
       }

        public void Dispose()
        {
            if (customLog != null)
            {
                customLog.Dispose();
                customLog = null;
            }
        }
    }
}
