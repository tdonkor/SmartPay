using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UK_BARCLAYCARD_SMARTPAY.Model
{
    public class PayDetails
    {
        public int PaidAmount { get; set; }
        public string TenderMediaId { get; set; }
        public string TenderMediaDetails { get; set; }
        public bool HasClientReceipt { get; set; }
        public bool HasMerchantReceipt { get; set; }
    }

    public class PayDetailsExtended : PayDetails
    {
        public string TerminalID { get; set; }
        public string PaymentMethod { get; set; }
        public string CardNumber { get; set; }
        public string AuthorizationCode { get; set; }
        public string TransactionReference { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string CardholderName { get; set; }
        public string TraceNumber { get; set; }
        public string TaxIdentificationNumber { get; set; }
        public object AdditionalDetails { get; set; }
    }
}
