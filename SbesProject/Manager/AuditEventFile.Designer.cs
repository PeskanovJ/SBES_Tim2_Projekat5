//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Manager {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class AuditEventFile {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal AuditEventFile() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Manager.AuditEventFile", typeof(AuditEventFile).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} failed to changed pin. Reason: {1}..
        /// </summary>
        internal static string ChangePinFailure {
            get {
                return ResourceManager.GetString("ChangePinFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} successfully changed pin..
        /// </summary>
        internal static string ChangePinSuccess {
            get {
                return ResourceManager.GetString("ChangePinSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} failed to deposit money. Reason: {1}..
        /// </summary>
        internal static string PaymentFailure {
            get {
                return ResourceManager.GetString("PaymentFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} successfully deposit {1} dinars..
        /// </summary>
        internal static string PaymentSuccess {
            get {
                return ResourceManager.GetString("PaymentSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} failed to withdraw money. Reason: {1}..
        /// </summary>
        internal static string PayoutFailure {
            get {
                return ResourceManager.GetString("PayoutFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} successfully withdraw {1} dinars..
        /// </summary>
        internal static string PayoutSuccess {
            get {
                return ResourceManager.GetString("PayoutSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} failed to registed. Reason: {1}..
        /// </summary>
        internal static string RegistrationCertFailure {
            get {
                return ResourceManager.GetString("RegistrationCertFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} successfully registered..
        /// </summary>
        internal static string RegistrationCertSuccess {
            get {
                return ResourceManager.GetString("RegistrationCertSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}&apos;s certificate failed to renew. Reason: {1}.
        /// </summary>
        internal static string RenewalCertFailure {
            get {
                return ResourceManager.GetString("RenewalCertFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}&apos;s certificate renew successfully.
        /// </summary>
        internal static string RenewalCertSuccess {
            get {
                return ResourceManager.GetString("RenewalCertSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request for transaction &apos;{0}&apos; failed..
        /// </summary>
        internal static string RequestTransactionFailure {
            get {
                return ResourceManager.GetString("RequestTransactionFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Transaction: {0} successfully requested..
        /// </summary>
        internal static string RequestTransactionSuccess {
            get {
                return ResourceManager.GetString("RequestTransactionSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}&apos;s certificate failed to revocate. Reason: {1}.
        /// </summary>
        internal static string RevocationCertFailure {
            get {
                return ResourceManager.GetString("RevocationCertFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}&apos;s certificate revocate successfully..
        /// </summary>
        internal static string RevocationCertSuccess {
            get {
                return ResourceManager.GetString("RevocationCertSuccess", resourceCulture);
            }
        }
    }
}
