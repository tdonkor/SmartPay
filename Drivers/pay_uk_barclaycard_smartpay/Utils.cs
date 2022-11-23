using Acrelec.Library.Logger;


namespace UK_BARCLAYCARD_SMARTPAY
{
    public enum DiagnosticErrMsg : short
    {
        OK = 1,
        NOTOK = 0
    }
    public class Utils
    {
        /// <summary>
        /// Check the numeric value of the amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static int GetNumericAmountValue(int amount)
        {

            if (amount <= 0)
            {
                Log.Info("Invalid pay amount");
                amount = 0;
            }

            return amount;
        }
    }
}
