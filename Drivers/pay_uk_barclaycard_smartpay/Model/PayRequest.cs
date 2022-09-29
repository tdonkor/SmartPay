namespace UK_BARCLAYCARD_SMARTPAY.Model
{
    /// <summary>
    /// Class that will be used to deserialize a json string given as a parameter to the "Pay" method
    /// </summary>
    public class PayRequest
    {
        /// <summary>
        /// The Total of the payment
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// The transaction reference number. 
        /// Usually this is the order number given by the POS
        /// </summary>
        public string TransactionReference { get; set; }
    }
}
