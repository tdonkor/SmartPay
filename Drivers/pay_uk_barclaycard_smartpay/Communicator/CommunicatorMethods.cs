using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UK_BARCLAYCARD_SMARTPAY.Communicator
{
    public enum CommunicatorMethods : int
    {
        /// <summary>
        /// The name of the init method that is send/received in a pipe message
        /// </summary>
        Init = 0,

        /// <summary>
        /// The name of the test method that is send/received in a pipe message
        /// </summary>
        Test = 1,

        /// <summary>
        /// The name of the start receiving money method that is send/received in a pipe message
        /// </summary>
        Pay = 2,

        /// <summary>
        /// The name of the progress message method that is send/received in a pipe message
        /// </summary>
        ProgressMessage = 3,
         
        /// <summary>
        /// The name of the cancel method that is send/received in a pipe message
        /// </summary>
        Cancel = 4,

        /// <summary>
        /// The name of the ExecuteCommand method that is send/received in a pipe message
        /// </summary>
        ExecuteCommand = 17
    }
}
