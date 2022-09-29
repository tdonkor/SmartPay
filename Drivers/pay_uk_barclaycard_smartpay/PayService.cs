﻿using Acrelec.Library.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UK_BARCLAYCARD_SMARTPAY.Communicator;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UK_BARCLAYCARD_SMARTPAY
{
    public class PayService : ICommunicatorCallbacks
    {
        public const string PAY_SERVICE_LOG = "Pay_Service";

        /// <summary>
        /// Object that is used for the communication with the Core Payment Driver
        /// </summary>
        private CoreCommunicator coreCommunicator;
          
        /// <summary>
        /// Property that will give access to the callback methods
        /// </summary>
        private ICommunicatorCallbacks CommunicatorCallbacks { get; set; }

        /// <summary>
        /// Flag that will be used to prevent 2 or more callback methods simultaneous execution
        /// </summary>
        public bool IsCallbackMethodExecuting;

        /// <summary>
        /// Duration of payment in seconds
        /// </summary>
        private int paymentDuration;

        /// <summary>
        /// Flag based on which the result of the "Cancel" will be given
        /// </summary>
        private bool isPaymentCancelSuccessful;

        /// <summary>
        /// Flag based on which the result of the "Cancel" will be given
        /// </summary>
        private bool isPaymentExecuteCommandSuccessful;

        /// <summary>
        /// option that will control the result of the payment.
        /// 0 - always succesfull
        /// 1 - always failure
        /// </summary>
        private string paymentResult;

        /// <summary>
        /// The type of the credit card that was used for the payment
        /// </summary>
        private string paymentTenderMediaID;

        /// <summary>
        /// The full path to the ticket.
        /// The ticket will be created and updated after each successful payment
        /// </summary>
        private string ticketPath;

        /// <summary>
        /// Flag which will be used to identify if the cancel was triggered
        /// </summary>
        private bool wasCancelTigged;

        public PayService(CoreCommunicator coreCommunicator)
        {

            ticketPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ticket");

            //init the core communicator
            this.coreCommunicator = coreCommunicator;

            // Hook the callback methods of the communicator to the ones of current class
            coreCommunicator.CommunicatorCallbacks = this;
        }

        /// <summary>
        /// Callback method that is triggered when the init message is received from the Core
        /// </summary>
        /// <param name="parameters"></param>
        public void InitRequest(object parameters)
        {
            Task.Run(() => { Init(parameters); });
        }

        /// <summary>
        /// Method will be executed in a separate thread that will execute and 
        /// Echo and Paring of the device
        /// </summary>
        /// <param name="parameters">
        /// Examle : {
        ///            {
        ///                PaymentDuration = 10
        ///                PaymentResult = 2
        ///                TenderMedia = Visa
        ///             }
        ///           }
        /// </param>
        private void Init(object parameters)
        {
            Log.Info(PAY_SERVICE_LOG, "call Initialize");

            //Check if another method is executing
            if (IsCallbackMethodExecuting)
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Init, new { Status = 1 });

                Log.Info(PAY_SERVICE_LOG, "        another method is executing.");

                Log.Info(PAY_SERVICE_LOG, "endcall Initialize");

                return;
            }
            else
                IsCallbackMethodExecuting = true;

            //Get the needed parameters to make a connection
            if (!GetInitParameters(parameters.ToString()))
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Init, new { Status = 1 });

                Log.Info(PAY_SERVICE_LOG, "        failed to deserialize the init parameters.");
            }
            else
            {
                // If paymentDuration received is less than 12 seconds we set at default value 12 so we can send progress messages
                if (paymentDuration < 12)
                    paymentDuration = 12;

                coreCommunicator.SendMessage(CommunicatorMethods.Init, new { Status = 0 });
                Log.Info(PAY_SERVICE_LOG, "        success.");
            }

            IsCallbackMethodExecuting = false;

            Log.Info(PAY_SERVICE_LOG, "endcall Initialize");
        }

        /// <summary>
        /// Callback method that is triggered when the test message is received from the Core
        /// </summary>
        /// <param name="parameters"></param>
        public void TestRequest(object parameters)
        {
            Task.Run(() => { Test(parameters); });
        }

        /// <summary>
        /// Method will be executed in a separate thread and will send Echo command and analyze the response 
        /// </summary>
        /// <param name="parameters"></param>
        public void Test(object parameters)
        {
            Log.Info(PAY_SERVICE_LOG, "call Test");

            //Check if another method is executing
            if (IsCallbackMethodExecuting)
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Test, new { Status = 1 });

                Log.Info(PAY_SERVICE_LOG, "        another method is executing.");

                Log.Info(PAY_SERVICE_LOG, "endcall Test");

                return;
            }
            else
                IsCallbackMethodExecuting = true;

            coreCommunicator.SendMessage(CommunicatorMethods.Test, new { Status = 0 });
            Log.Info(PAY_SERVICE_LOG, "        success.");

            IsCallbackMethodExecuting = false;

            Log.Info(PAY_SERVICE_LOG, "endcall Test");
        }

        /// <summary>
        /// Callback method that is triggered when the pay message is received from the Core
        /// </summary>
        /// <param name="parameters"></param>
        public void PayRequest(object parameters)
        {
            Task.Run(() => { Pay(parameters); });
        }

        /// <summary>
        /// Method will be executed in a separate thread and will send Echo command and analyze the response 
        /// </summary>
        /// <param name="parameters"></param>
        public void Pay(object parameters)
        {
            Log.Info(PAY_SERVICE_LOG, "call Pay");

            //Check if another method is executing
            if (IsCallbackMethodExecuting)
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Pay, new { Status = 297, Description = "Another method is executing." });

                Log.Info(PAY_SERVICE_LOG, "        another method is executing.");
                Log.Info(PAY_SERVICE_LOG, "endcall Pay");

                return;
            }
            else
                IsCallbackMethodExecuting = true;
            try
            {
                //Get the pay request object that will be sent to the fiscal printer
                Log.Info(PAY_SERVICE_LOG, "        deserialize the pay request parameters.");
                PayRequest payRequest = GetPayRequest(parameters.ToString());

                //Check if the pay deserialization was successful
                if (payRequest == null)
                {
                    coreCommunicator.SendMessage(CommunicatorMethods.Pay, new { Status = 331, Description = "Failed to deserialize the pay request parameters." });
                    Log.Info(PAY_SERVICE_LOG, "        failed to deserialize the pay request parameters.");
                    return;
                }

                if (!wasCancelTigged || (wasCancelTigged && !isPaymentCancelSuccessful))
                { 
                    //Send payment progres to the Core.
                    Log.Info(PAY_SERVICE_LOG, "        inserted card");
                    coreCommunicator.SendMessage(CommunicatorMethods.ProgressMessage, new { PayProgress = new PayProgress { MessageClass = "CheckPINPADDisplay", Message = "Insert Card" } });

                    Thread.Sleep(3000);
                }

                if (!wasCancelTigged || (wasCancelTigged && !isPaymentCancelSuccessful))
                {
                    //Send payment progres to the Core.
                    Log.Info(PAY_SERVICE_LOG, "        insert pin");
                    coreCommunicator.SendMessage(CommunicatorMethods.ProgressMessage, new { PayProgress = new PayProgress { MessageClass = "CheckPINPADDisplay", Message = "Insert Pin" } });

                    Thread.Sleep(3000);
                }

                if (!wasCancelTigged || (wasCancelTigged && !isPaymentCancelSuccessful))
                {
                    //Send payment progres to the Core.
                    Log.Info(PAY_SERVICE_LOG, "        processing");
                    coreCommunicator.SendMessage(CommunicatorMethods.ProgressMessage, new { PayProgress = new PayProgress { MessageClass = "CheckPINPADDisplay", Message = "Processing" } });

                    Thread.Sleep((paymentDuration - 9) * 1000);
                }

                if (!wasCancelTigged || (wasCancelTigged && !isPaymentCancelSuccessful))
                {
                    //Send payment progres to the Core.
                    Log.Info(PAY_SERVICE_LOG, "        retract card");
                    coreCommunicator.SendMessage(CommunicatorMethods.ProgressMessage, new { PayProgress = new PayProgress { MessageClass = "CheckPINPADDisplay", Message = "Retract Card" } });

                    Thread.Sleep(5000);
                }

                PayDetailsExtended payDetails = new PayDetailsExtended();
                 
                //treat answer type
                if (wasCancelTigged && isPaymentCancelSuccessful)
                {
                    Log.Info(PAY_SERVICE_LOG, "        payment failed.");
                    coreCommunicator.SendMessage(CommunicatorMethods.Pay, new { Status = 335, Description = "Failed payment. Canceled by user.", PayDetailsExtended = payDetails });
                }
                //always success
                else if (paymentResult == "success")
                {
                    payDetails.TenderMediaId = paymentTenderMediaID;
                    payDetails.PaidAmount = payRequest.Amount;
                    payDetails.HasClientReceipt = true;
                    payDetails.AuthorizationCode = "123456789";
                    payDetails.CardholderName = "Test";
                    payDetails.TransactionReference = "554878845568";

                    //create receipt
                    SaveTicket(string.Format("\r\n\r\n Payment Simulator Ticket\r\n\r\n   Amount:{0}\r\n   TenderID: {1}\r\n\r\n   {2}\r\n\r\n", payRequest.Amount, paymentTenderMediaID, DateTime.Now.ToString()));

                    Log.Info(PAY_SERVICE_LOG, "        credit card payment succeeded.");
                    coreCommunicator.SendMessage(CommunicatorMethods.Pay, new { Status = 0, Description = "Successful payment", PayDetailsExtended = payDetails });
                }
                //always failure
                else if (paymentResult == "failure")
                {
                    Log.Info(PAY_SERVICE_LOG, "        payment failed.");
                    coreCommunicator.SendMessage(CommunicatorMethods.Pay, new { Status = 334, Description = "Failed payment", PayDetailsExtended = payDetails });                 
                }

                return;
            }
            catch (Exception ex)
            {
                Log.Info(PAY_SERVICE_LOG, string.Format("        {0}", ex.ToString()));
            }
            finally
            {
                wasCancelTigged = false;
                IsCallbackMethodExecuting = false;
                Log.Info(PAY_SERVICE_LOG, "endcall Pay");
            }
        }

        /// <summary>
        /// Callback method that is triggered when the cancel message is received from the Core
        /// </summary>
        /// <param name="parameters"></param>
        public void CancelRequest(object parameters)
        {
            Task.Run(() => { Cancel(parameters); });
        }

        /// <summary>
        /// Callback method that is triggered when the cancel message is received from the Core on a separate thread
        /// </summary>
        /// <param name="parameters"></param>
        public void Cancel(object parameters)
        {
            Log.Info(PAY_SERVICE_LOG, "call Cancel");

            wasCancelTigged = true; 

            if (isPaymentCancelSuccessful)
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Cancel, new { Status = 0 });
                Log.Info(PAY_SERVICE_LOG, "        successful cancelation.");
            }
            else
            {
                coreCommunicator.SendMessage(CommunicatorMethods.Cancel, new { Status = 1 });
                Log.Info(PAY_SERVICE_LOG, "        failed cancelation.");
            }

            Log.Info(PAY_SERVICE_LOG, "endcall Cancel");
        }

        /// <summary>
        /// Method that is executed if the ExecuteCommand event is raised by the communicator.
        /// </summary>
        /// <param name="parameters">Parameters that are send by the event</param>
        public void ExecuteCommandRequest(object parameters)
        {
            Task.Run(() => { ExecuteCommand(parameters); });
        }

        private void ExecuteCommand(object executeCommandRequestJson)
        {
            try
            {
                Log.Info(PAY_SERVICE_LOG, "call ExecuteCommand");

                //Check if another method is executing
                if (IsCallbackMethodExecuting)
                {
                    coreCommunicator.SendMessage(CommunicatorMethods.ExecuteCommand, new { Status = 297, Description = "Another method is executing", ExecuteCommandResponse = "Command not executed. Reason: busy" });
                    Log.Info(PAY_SERVICE_LOG, "        another method is executing.");

                    return;
                }
                else
                    IsCallbackMethodExecuting = true;

                //Deserialize execute command parameters
                ExecuteCommandRequest executeCommandRequest = GetExecuteCommandRequest(executeCommandRequestJson.ToString());

                //Check if the execute command deserialization was successful
                if (executeCommandRequest == null)
                {
                    coreCommunicator.SendMessage(CommunicatorMethods.ExecuteCommand, new { Status = 331, Description = "failed to deserialize the execute command request parameters", ExecuteCommandResponse = "Command not executed. Reason: params" });
                    Log.Info(PAY_SERVICE_LOG, "        failed to deserialize the execute command request parameters");
                    return;
                }

                Log.Info(PAY_SERVICE_LOG, $"     Command name to be executed: {executeCommandRequest.Command}");
                Log.Info(PAY_SERVICE_LOG, $"     Command body to be executed: {executeCommandRequest.CommandInfo}");

                //success
                if (isPaymentExecuteCommandSuccessful)
                {
                    Log.Info(PAY_SERVICE_LOG,  "     ExecuteCommand succeeded.");
                    coreCommunicator.SendMessage(CommunicatorMethods.ExecuteCommand, new { Status = 0, Description = "ExecuteCommand succeeded", ExecuteCommandResponse = "Command executed" });
                    Log.Info(PAY_SERVICE_LOG, $"     Command Executed: {executeCommandRequest.Command}, ReturnedText: Command executed");
                }
                //failure
                else
                {
                        Log.Info(PAY_SERVICE_LOG,  "     ExecuteCommand failed.");
                        coreCommunicator.SendMessage(CommunicatorMethods.ExecuteCommand, new { Status = 442, Description = "ExecuteCommand failed", ExecuteCommandResponse = "Command not executed. Reason: willingly" });

                        Log.Info(PAY_SERVICE_LOG, $"     Command Executed: {executeCommandRequest.Command}, Command not executed. Reason: willingly");
                }                
            }
            catch (Exception ex)
            {
                Log.Error(PAY_SERVICE_LOG, $"ExecuteCommand exception: {ex.ToString()}");
            }
            finally
            {
                Log.Info(PAY_SERVICE_LOG, "endcall ExecuteCommand");
                IsCallbackMethodExecuting = false;
            }
        }

        /// <summary>
        /// Deserialize the received json string and extract all the parameter used for the initialization
        /// </summary>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        private bool GetInitParameters(string initJson)
        {
            try
            {
                JObject jObject = JObject.Parse(initJson);

                if (jObject == null)
                    return false;

                if (jObject["PaymentDuration"] == null ||
                    jObject["PaymentResult"] == null ||
                    jObject["TenderMedia"] == null ||
                    jObject["IsPaymentExecuteCommandSuccessful"] == null ||
                    jObject["IsPaymentCancelSuccessful"] == null)

                    
                    return false;

                paymentDuration = Convert.ToInt32(jObject["PaymentDuration"].ToString());
                isPaymentCancelSuccessful = bool.Parse(jObject["IsPaymentCancelSuccessful"].ToString());
                isPaymentExecuteCommandSuccessful = bool.Parse(jObject["IsPaymentExecuteCommandSuccessful"].ToString());
                paymentResult = jObject["PaymentResult"].ToString();
                paymentTenderMediaID = jObject["TenderMedia"].ToString();

                return true;
            }
            catch (Exception ex)
            {
                Log.Info(PAY_SERVICE_LOG, string.Format("        {0}", ex.ToString()));
            }
            return false;
        }

        /// <summary>
        /// Deserialize the received json string into a PayRequest object
        /// </summary>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        private PayRequest GetPayRequest(string payRequestJSonString)
        {
            try
            {
                return JsonConvert.DeserializeObject<PayRequest>(payRequestJSonString);
            }
            catch (Exception ex)
            {
                Log.Info(PAY_SERVICE_LOG, string.Format("        {0}", ex.ToString()));
            }

            return null;
        }

        /// <summary>
        /// Save te information received in the <paramref name="ticketDetails"/> in a file
        /// </summary>
        /// <param name="ticketDetails"></param>
        private void SaveTicket(string ticketDetails)
        {
            try
            {

                //Delete the old ticket
                if (File.Exists(ticketPath))
                    File.Delete(ticketPath);

                //Write the new ticket
                File.WriteAllText(ticketPath, ticketDetails);
            }
            catch (Exception ex)
            {
                Log.Info(PAY_SERVICE_LOG, string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Deserialize the received json string into a ExecuteCommandRequest object
        /// </summary>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        private ExecuteCommandRequest GetExecuteCommandRequest(string executeCommandRequestJsonString)
        {
            try
            {
                ExecuteCommandRequest returnObject = JsonConvert.DeserializeObject<ExecuteCommandRequest>(executeCommandRequestJsonString.ToString());

                return returnObject;
            }
            catch (Exception ex)
            {
                Log.Error(PAY_SERVICE_LOG, $"GetExecuteCommandRequest exception: {ex.ToString()}");
            }
            return null;
        }
    }
}