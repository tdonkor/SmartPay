using Acrelec.Library.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UK_BARCLAYCARD_SMARTPAY.Communicator;

namespace UK_BARCLAYCARD_SMARTPAY
{
    class Program
    {
        static void Main(string[] args)
        {
            CoreCommunicator coreCommunicator = new CoreCommunicator();

            PayService payService = new PayService(coreCommunicator);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Log.Info(PayService.PAY_SERVICE_LOG, "Pay_Service started.");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Info(PayService.PAY_SERVICE_LOG, (e.ExceptionObject as Exception).Message);
        }
    }
}
