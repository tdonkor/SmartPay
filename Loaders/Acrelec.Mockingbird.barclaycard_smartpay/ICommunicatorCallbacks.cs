using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Barclaycard_Smartpay
{
    public interface ICommunicatorCallbacks
    {
        void InitResponse(object parameters);

        void TestResponse(object parameters);

        void PayResponse(object parameters);

        void ProgressMessageResponse(object parameters);

        void CancelResponse(object parameters);

        void ExecuteCommandResponse(object parameters);
    }
}
