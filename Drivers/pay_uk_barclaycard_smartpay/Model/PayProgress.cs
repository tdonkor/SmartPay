using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UK_BARCLAYCARD_SMARTPAY.Model
{
    /// <summary>
    /// Class that is used to serialize the pay progress notification that is sent to the application
    /// </summary>
    public class PayProgress
    {
        /// <summary>
        /// The type of message
        /// </summary>
        public string MessageClass { get; set; }

        /// <summary>
        /// The message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The current amount payed (added for Cash Payments)
        /// </summary>
        public int CurrentPaidAmount { get; set; }
    }
}
