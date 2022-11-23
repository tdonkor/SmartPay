using Acrelec.Library.Pipes;
using Acrelec.Mockingbird.Interfaces.Peripherals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;

namespace Acrelec.Mockingbird.Barclaycard_Smartpay
{
    /// <summary>
    /// Class in charge with the communication between the PAY_ES_YOMANI.exe and the Acrelec.Mockingbird.Core.Service.exe via Pipes
    /// </summary>
    public class Communicator
    {
        #region Constants
        /// <summary>
        /// Log name
        /// </summary>
        private const string COMMUNICATOR_LOG = "CoreCommunicator";

        /// <summary>
        /// The name of the Pipe server that the PAY_SIMULATOR.exe will open to receive messages from the Acrelec.Mockingbird.Core.Service.exe
        /// </summary>
        private const string PAY_SERVICE_PIPE_SERVER_NAME = "UK_BARCLAYCARD_SMARTPAY_ExePipeServer";

        /// <summary>
        /// The name of the Pipe server that the Acrelec.Mockingbird.Core.Service.exe will open to receive messages from the PAY_SIMULATOR.exe
        /// </summary>
        private const string CORE_PIPE_SERVER_NAME = "UK_BARCLAYCARD_SMARTPAY_CorePipeServer";
        #endregion

        public ICommunicatorCallbacks CommunicatorCallbacks { get; set; }
        
        /// <summary>
        /// Pipe server that will be used to receive messages
        /// </summary>
        private PipeServer pipeServer;

        /// <summary>
        /// Pipe server that will be used to send messages
        /// </summary>
        private PipeClient pipeClient;

        /// <summary>
        /// Object in charge with log saving
        /// </summary>
        ILogger logger;

        /// <summary>
        /// Constructor
        /// </summary>
        [ImportingConstructor]
        public Communicator(ILogger logger = null)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Start the Pipe server that will listen for messages comming from the payment driver
        /// </summary>
        public void StartListening()
        {
            logger.Info(COMMUNICATOR_LOG, "Started communication server.");

            pipeServer = new PipeServer(CORE_PIPE_SERVER_NAME);

            pipeClient = new PipeClient();

            pipeServer.OnReceiveMessage += DoOnReceiveMessage;        
        }

        /// Method that is called each time a message is sent by the Acrelec.Mockingbird.Core.Service.exe payment 
        /// is received
        /// </summary>
        /// <param name="message">The message content</param>
        public void DoOnReceiveMessage(string message)
        {
            logger.Info(COMMUNICATOR_LOG, "Received: " + message);

            //Try to deserialize the received message string into a CommunicatorMessage
            PipeMessage receivedMessage = GetMessage(message);

            //If deserialization fails stop processing the message
            if (receivedMessage == null)
                return;

            //Based on the method in the message raise the proper event

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.Init.ToString().ToLower()))
            {
                CommunicatorCallbacks.InitResponse(receivedMessage.Params);
                return;
            }

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.Test.ToString().ToLower()))
            {
                CommunicatorCallbacks.TestResponse(receivedMessage.Params); 
                return;
            }

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.Pay.ToString().ToLower()))
            {
                CommunicatorCallbacks.PayResponse(receivedMessage.Params); 
                return;
            }

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.ProgressMessage.ToString().ToLower()))
            {
                CommunicatorCallbacks.ProgressMessageResponse(receivedMessage.Params);
                return;
            }

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.Cancel.ToString().ToLower()))
            {
                CommunicatorCallbacks.CancelResponse(receivedMessage.Params);
                return;
            }

            if (receivedMessage.Method.ToLower().Equals(CommunicatorMethods.ExecuteCommand.ToString().ToLower()))
            {
                CommunicatorCallbacks.ExecuteCommandResponse(receivedMessage.Params);
                return;
            }
        }

        /// <summary>
        /// Send a message using pipes to the Acrelec.Mockingbird.Core.Service.exe payment driver
        /// </summary>
        /// <param name="message">The message to be sent</param>
        /// <returns>True - If the message was send successfully</returns>
        public bool SendMessage(CommunicatorMethods method, object parameters)
        {
            //Generate the message string that will be sent via pipes
            string message = CreateMessage(method, parameters);

            //Check if the message was successfully generated
            if (string.IsNullOrEmpty(message))
                return true;
            
            //Send the message and check it the send was successful
            if (pipeClient.SendMessage(PAY_SERVICE_PIPE_SERVER_NAME, message))
            {
                logger.Info(COMMUNICATOR_LOG, "Sent (OK): " + message);
                return true;
            }
            else
            {
                logger.Info(COMMUNICATOR_LOG, "Sent (NOK): " + message);
                return false;
            }
        }

        /// <summary>
        /// Close Pipe server.
        /// </summary>
        public void Close()
        {
            //Close the pipe server if it exists
            if (this.pipeServer != null)
                pipeServer.Terminate();

            logger.Info(COMMUNICATOR_LOG, "Stopped communication server.");
        }

        /// <summary>
        /// Deserialize the json string <paramref name="jsonString"/> into a CommunicatorMessage object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        private PipeMessage GetMessage(string jsonString)
        {
            try
            {
                PipeMessage deserializedMessage = JsonConvert.DeserializeObject<PipeMessage>(jsonString);
                return deserializedMessage;
            }
            catch (Exception ex)
            {
                logger.Error(COMMUNICATOR_LOG, string.Format("Failed to parse the deserialize the received message.\r\n{0}", ex.ToString()));
            }
            return null;
        }

        /// <summary>
        /// Create the Pipe Message using the received method and parameters
        /// </summary>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string CreateMessage(CommunicatorMethods method, object parameters)
        {
            try
            {
                PipeMessage pipeMessage = new PipeMessage
                {
                    Method = method.ToString().ToLower(),
                    Params = parameters
                };
                return JObject.FromObject(pipeMessage).ToString();
            }
            catch (Exception ex)
            {
                logger.Info(COMMUNICATOR_LOG, string.Format("Sent (NOK): {0} Message Creation failure.\r\n{0}", method.ToString().ToUpper(), ex.ToString()));
                return string.Empty;
            }
        }
    }
}

