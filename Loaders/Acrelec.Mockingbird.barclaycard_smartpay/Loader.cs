using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Acrelec.Mockingbird.Feather.Peripherals.Payment;
using Acrelec.Mockingbird.Feather.Peripherals.Payment.Model;
using Acrelec.Mockingbird.Feather.Peripherals.Models;
using Acrelec.Mockingbird.Feather.Peripherals.Settings;
using Acrelec.Mockingbird.Feather.Peripherals.Enums;
using Acrelec.Mockingbird.Interfaces.Peripherals;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Acrelec.Mockingbird.Feather.Peripherals.Peripherals.Payment.Model;

namespace Acrelec.Mockingbird.Barclaycard_Smartpay
{
    [Export(typeof(IPayment))]
    public class Loader : IPaymentCardExtendedDetails, ICommunicatorCallbacks, IPaymentExtendsCancelPayment, IPaymentExtendsExecuteCommand
    {
        private const string PAYMENT_ID = "0";

        private const string PAYMENT_LOG = "UK_BARCLAYCARD_SMARTPAY_SERVICE";

        private const string PAYMENT_NAME = "PAY_UK_BARCLAYCARD_SMARTPAY";

        private const string PAYMENT_APPLICATION_NAME = "PAY_UK_BARCLAYCARD_SMARTPAY.exe";

        private const string PAYMENT_APPLICATION_PROCESS_NAME = "PAY_UK_BARCLAYCARD_SMARTPAY";

        private const string DRIVER_FOLDER_NAME = "DriverExe";

        private string driverLocation;

        private const PeripheralType PAYMENT_TYPE = Feather.Peripherals.Enums.PeripheralType.card;

        /// <summary>
        /// Status of current payment
        /// </summary>
        private PaymentStatus paymentStatus;

        /// <summary>
        /// Object that will be used to communicate with the payment application
        /// </summary>
        private Communicator communicator;
  
        public string DriverId
        {
            get
            {
                return PAYMENT_ID;
            }
        }

        /// <summary>
        /// Method will return the get minimum API version of the selected peripheral driver
        /// </summary>
        public int MinAPILevel { get { return 3; } }

        /// <summary>
        /// Method will check the get version of the Payment driver application
        /// </summary>
        /// <returns>
        /// A string representing the version of of the Payment driver application '.exe'
        /// </returns>
        public string DriverVersion
        {
            get
            {
                try
                {
                    return Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                catch (Exception ex)
                {
                    logger.Error(string.Format("Get Payment Driver Application Version: \r\n{0}", ex.Message));
                }
                return string.Empty;
            }
        }

        private SpecificStatusDetails specificStatusDetails;
          
        /// <summary>
        /// Duration of payment in seconds
        /// </summary>
        private AdminPeripheralSetting paymentDuration;

        /// <summary>
        /// Flag that will control the result of the payment.
        /// True - succesfull payment
        /// False - fail payment
        /// </summary>
        private AdminPeripheralSetting paymentResult;
       
        /// <summary>
        /// The type of the credit card that was used for the payment
        /// </summary>
        private AdminPeripheralSetting paymentTenderMediaID;

        private AdminPeripheralSetting paymentCancelResult;

        private AdminPeripheralSetting paymentExecuteCommandResult;

        private AdminPeripheralSetting comPort;

        /// <summary>
        /// The factory details of the Payment including a list of settings
        /// </summary>
        private Payment currentPaymentInitConfig;
        
        /// <summary>
        /// Object in charge of log saving
        /// </summary>
        ILogger logger;
        
        /// <summary>
        /// Flag used by the Init method know when the Init response was received
        /// </summary>
        private bool IsInitFinished { get; set; }

        /// <summary>
        /// Flag used by the Test method know when the Test response was received
        /// </summary>
        private bool IsTestFinished { get; set; }

        /// <summary>
        /// Flag used by the private method to know when the Pay response was received
        /// </summary>
        private bool IsPayFinished { get; set; }

        /// <summary>
        /// Flag used by the Cancel method know when the Cancel response was received
        /// </summary>
        private bool IsCancelFinished { get; set; }

        /// <summary>
        /// Flag used by the private method know when the ExecuteCommand response was received
        /// </summary>
        private bool IsExecuteCommandFinished { get; set; }
         
        /// <summary>
        /// Flag used to know if the Init Method was successful.
        /// The value of the flag is updated when the Init message response is received from the Payment Application
        /// </summary>
        private bool WasInitSuccessful { get; set; }

        /// <summary>
        /// Flag used to know if the Test Method was successful.
        /// The value of the flag is updated when the Test message response is received from the Payment Application
        /// </summary>
        private bool WasTestSuccessful { get; set; }

        /// <summary>
        /// Flag used to know if the Pay Method was successful.
        /// The value of the flag is updated when the Pay message response is received from the Payment Application
        /// </summary>
        private bool WasPaySuccessful { get; set; }
         
        /// <summary>
        /// Flag used to know if the Cancel Method was successful.        
        /// </summary>
        private bool WasCancelSuccessful { get; set; }
         
        /// <summary>
        /// Flag is used to know if the ExecuteCommand was successful
        /// The value of the flag is updated when the ExecuteCommand message response is received from the Payment Application
        /// </summary>
        private bool WasExecuteCommandSuccessful { get; set; }

        /// <summary>
        /// Object that is populated by the Pay Response Callback
        /// </summary>
        private PayDetailsExtended payDetails;

        /// <summary>
        /// Details that are send when a execute command is finalized
        /// </summary>
        private string executeCommandDetails;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        [ImportingConstructor]
        public Loader(ILogger logger = null)
        {
            this.logger = logger;

            driverLocation = $@"C:\Acrelec\Core\Peripherals\Payments\Drivers\{PAYMENT_NAME}\{DriverVersion}\Driver\{DRIVER_FOLDER_NAME}";

            paymentStatus = new PaymentStatus();

            communicator = new Communicator(logger);

            //Hook the payment callback to the current callback
            communicator.CommunicatorCallbacks = this;

            //Init the settings            
            Dictionary<string, object> paymentCancelDict = new Dictionary<string, object>();
            paymentCancelDict.Add("success", "always success");
            paymentCancelDict.Add("failure", "always failure");
            paymentCancelResult = new AdminPeripheralSetting();
            paymentCancelResult.ControlType = SettingDataType.SingleSelection;
            paymentCancelResult.ControlName = "Payment Cancel Result";
            paymentCancelResult.RealName = "PaymentCancelResult";
            paymentCancelResult.CurrentValue = "success";
            paymentCancelResult.PossibleValues = paymentCancelDict;
            paymentCancelResult.ControlDescription = "Response send when an Payment Cancel request is received.";

            Dictionary<string, object> paymentExecuteDict = new Dictionary<string, object>();
            paymentExecuteDict.Add("success", "always success");
            paymentExecuteDict.Add("failure", "always failure");
            paymentExecuteCommandResult = new AdminPeripheralSetting();
            paymentExecuteCommandResult.ControlType = SettingDataType.SingleSelection;
            paymentExecuteCommandResult.ControlName = "Payment Execute Command Result";
            paymentExecuteCommandResult.RealName = "PaymentExecuteCommandResult";
            paymentExecuteCommandResult.CurrentValue = "success";
            paymentExecuteCommandResult.PossibleValues = paymentExecuteDict;
            paymentExecuteCommandResult.ControlDescription = "Response send when an Payment Execute Command request is received.";

            paymentDuration = new AdminPeripheralSetting();
            paymentDuration.ControlType = SettingDataType.Int;
            paymentDuration.ControlName = "Payment Duration";
            paymentDuration.RealName = "PaymentDuration";
            paymentDuration.CurrentValue = "12";
            paymentDuration.ControlDescription = "Duration of payment in seconds";

            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("success", "always success");
            dict.Add("failure", "always failure");
            paymentResult = new AdminPeripheralSetting();
            paymentResult.ControlType = SettingDataType.SingleSelection;
            paymentResult.ControlName = "Payment Result";
            paymentResult.RealName = "PaymentResult";
            paymentResult.CurrentValue = "success";
            paymentResult.PossibleValues = dict;
            paymentResult.ControlDescription = "Flag that will control the result of the payment.";

            paymentTenderMediaID = new AdminPeripheralSetting();
            paymentTenderMediaID.ControlType = SettingDataType.String;
            paymentTenderMediaID.ControlName = "Tender Media";
            paymentTenderMediaID.RealName = "TenderMedia";
            paymentTenderMediaID.CurrentValue = "Visa";
            paymentTenderMediaID.ControlDescription = "The type of the credit card that was used for the payment";

            comPort = new AdminPeripheralSetting();
            comPort.ControlType = SettingDataType.SerialPortSelection;
            comPort.ControlName = "Com Port";
            comPort.RealName = "PORT";
            comPort.CurrentValue = "";
            comPort.ControlDescription = "The payment application COM Port.";

            currentPaymentInitConfig = new Payment
            {
                PaymentName = PAYMENT_NAME,
                DriverFolderName = DRIVER_FOLDER_NAME,
                ConfigurationSettings = new List<AdminPeripheralSetting>(),
                Id = PAYMENT_ID,
                Type = PAYMENT_TYPE.ToString()                
            };
        }

        #region IPayment    

        /// <summary>
        /// callbaks
        /// </summary>
        public IPaymentCallbacks PaymentCallbacks { get; set; }
        
        public PaymentCapability Capability
        {
            get
            {
                return new PaymentCapability
                {
                    ReceivePayProgressCalls = true,
                    IsCancelPaymentAvailable = true,
                    CanExecuteCommands = true,
                    Type = PAYMENT_TYPE.ToString(),
                    Name = PAYMENT_NAME
               };
            }
        }
        
        /// <summary>
        /// Returns the name of the current payment
        /// </summary>
        public string PeripheralName
        {
            get { return PAYMENT_NAME; }
        }
        
        /// <summary>
        /// Returns the type of the current payment
        /// </summary>
        public string PeripheralType
        {
            get { return PAYMENT_TYPE.ToString(); }
        }

        public PeripheralStatus LastStatus
        {
            get { return paymentStatus.ToPeripheralStatus(); }
        }
        
        public bool Init()
        {
            try
            {
                logger.Info(PAYMENT_LOG, "Init: Initializing payment.");

                //Start the pipe server so driver can wait for messages
                communicator.StartListening();

                //Start the driver Payment application (if it's already open try to close it before starting it)
                LaunchPaymentApplication();

                Thread.Sleep(2000);

                //Set the flag to false until a response is received from the payment application
                IsInitFinished = false;
                WasInitSuccessful = false;

                //Create the Init parameters object that will be sent to the payment application
                //If needed this properties can be configured as PaymentSettings and be set from Admin
                object initParameters = new
                {
                    PaymentDuration = paymentDuration.CurrentValue,
                    PaymentResult = paymentResult.CurrentValue,
                    IsPaymentCancelSuccessful = paymentCancelResult.CurrentValue.ToString() == "success" ? true : false,
                    IsPaymentExecuteCommandSuccessful = paymentExecuteCommandResult.CurrentValue.ToString() == "success" ? true : false,
                    TenderMedia = paymentTenderMediaID.CurrentValue
                };

                //If the message is not received by the payment application the method will fail
                if (!communicator.SendMessage(CommunicatorMethods.Init, initParameters))
                    return false;

                //Wait until the payment application responds to the init message
                while (!IsInitFinished)
                {
                    Thread.Sleep(0);
                    Thread.Sleep(50);
                }

                logger.Info(PAYMENT_LOG, "Init: Finished Initializing payment.");

                return WasInitSuccessful;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Init: Failed to initialize payment.\r\n{0}", ex.ToString()));
            }
            return false;
        }

        public bool Test()
        {
            try
            {
                logger.Info(PAYMENT_LOG, "Test: Started testing payment.");

                //Set the flag to false until a response is received from the payment application
                IsTestFinished = false;
                WasTestSuccessful = false;

                //If the message is not received by the payment application the method will fail
                if (!communicator.SendMessage(CommunicatorMethods.Test, new object()))
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
                    return false;
                }

                //Wait until the payment application responds to the test message
                while (!IsTestFinished)
                {
                    Thread.Sleep(0);
                    Thread.Sleep(50);
                }

                logger.Info(PAYMENT_LOG, "Test: Finished testing payment.");

                if (WasTestSuccessful)
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralOK().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralOK().Description;
                }
                else
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
                }
                return WasTestSuccessful;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Test: Failed to test payment.\r\n{0}", ex.ToString()));
            }

            paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
            paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
            return false;
        }

        public bool Pay(PayRequest payRequest, ref PayDetailsExtended payDetailsExtended, ref SpecificStatusDetails specificStatusDetails, ref bool wasUncertainPaymentDetected)
        {
            try
            {
                logger.Info(PAYMENT_LOG, "Pay: Started payment.");

                //Init the paid amount and Tender Media
                payDetails = new PayDetailsExtended();

                //Init the object that will be updated with the specific error code and description.
                this.specificStatusDetails = new SpecificStatusDetails();

                specificStatusDetails.StatusCode = PeripheralStatus.PeripheralGenericError().Status;
                specificStatusDetails.Description = PeripheralStatus.PeripheralGenericError().Description;

                //Set the flag to false until a response is received from the payment application
                IsPayFinished = false;
                WasPaySuccessful = false;

                //If the message is not received by the payment application the method will fail
                if (!communicator.SendMessage(CommunicatorMethods.Pay, payRequest))
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
                    return false;
                }

                //Wait until the payment application responds to the test message
                while (!IsPayFinished)
                {
                    Thread.Sleep(0);
                    Thread.Sleep(50);
                }

                logger.Info(PAYMENT_LOG, "Pay: Pay finished.");

                if (WasPaySuccessful)
                {
                    //Update the pay details
                    payDetailsExtended = this.payDetails;

                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralOK().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralOK().Description;
                }

                specificStatusDetails = this.specificStatusDetails;

                return WasPaySuccessful;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Pay: Failed payment.\r\n{0}", ex.ToString()));
            }
            
            return false;
        }

        public bool CancelPay(CancelPayRequest cancelPayRequest, ref SpecificStatusDetails specificStatusDetails)
        {
            try
            {
                logger.Info(PAYMENT_LOG, "Cancel: Started cancel payment.");

                //Set the flag to false until a response is received from the payment application
                IsCancelFinished = false;
                WasCancelSuccessful = false;

                //Init the object that will be updated with the specific error code and description.
                this.specificStatusDetails = new SpecificStatusDetails();

                specificStatusDetails.StatusCode = PeripheralStatus.PeripheralGenericError().Status;
                specificStatusDetails.Description = PeripheralStatus.PeripheralGenericError().Description;

                //If the message is not received by the payment application the method will fail
                if (!communicator.SendMessage(CommunicatorMethods.Cancel, new object()))
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
                    return false;
                }

                //Wait until the payment application responds to the test message
                while (!IsCancelFinished)
                {
                    Thread.Sleep(0);
                    Thread.Sleep(50);
                }

                logger.Info(PAYMENT_LOG, "Cancel: Finished canceling payment.");

                if (WasCancelSuccessful)
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralOK().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralOK().Description;
                }
                else
                {
                    paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
                    paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
                }

                specificStatusDetails = this.specificStatusDetails;

                return WasCancelSuccessful;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Cancel: Failed to cancel payment.\r\n{0}", ex.ToString()));
            }

            paymentStatus.CurrentStatus = PeripheralStatus.PeripheralGenericError().Status;
            paymentStatus.ErrorCodesDescription = PeripheralStatus.PeripheralGenericError().Description;
            return false;
        }

        public PeripheralStatus ExecuteCommand(PaymentExecuteCommandRequest commandRequest, ref string commandDetails, ref SpecificStatusDetails specificStatusDetails)
        {
            try
            {
                logger.Info(PAYMENT_LOG, "ExecuteCommand: Started.");

                //Set the flags to false until a response is received from the payment application
                IsExecuteCommandFinished = false;
                WasExecuteCommandSuccessful = false;

                this.executeCommandDetails = string.Empty;

                //Init the object that will be updated with the specific error code and description.
                this.specificStatusDetails = new SpecificStatusDetails();

                specificStatusDetails.StatusCode = PeripheralStatus.PeripheralGenericError().Status;
                specificStatusDetails.Description = PeripheralStatus.PeripheralGenericError().Description;

                //If the message is not received by the payment application the method will fail
                if (!communicator.SendMessage(CommunicatorMethods.ExecuteCommand, (object)commandRequest))
                    return new PeripheralStatus { Status = 1, Description = "ExecuteCommand failed" };

                //Wait until the payment application responds to the execute command message
                while (!IsExecuteCommandFinished)
                {
                    Thread.Sleep(0);
                    Thread.Sleep(50);
                }

                //Update response details with the ones received from the driver
                specificStatusDetails = this.specificStatusDetails;

                commandDetails = this.executeCommandDetails;

                //Check response
                if (WasExecuteCommandSuccessful)
                {
                    return new PeripheralStatus { Status = 0, Description = "" };
                }
                else
                {
                    return new PeripheralStatus { Status = 1, Description = "ExecuteCommand failed" };
                }

            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, $"ExecuteCommand exception: {ex.ToString()}");
                return new PeripheralStatus { Status = 1, Description = "ExecuteCommand failed" };
            }
            finally
            {
                logger.Info(PAYMENT_LOG, "ExecuteCommand: Finished.");
            }
        }

        /// <summary>
        /// Update all the settings that the driver needs. 
        /// This is done when the peripheral is fist loaded (core start) or when it is set from the Admin
        /// </summary>
        /// <param name="configJson">A json containing all the settings of the payment device</param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public bool UpdateSettings(string configJson, bool overwrite = false)
        {
            try
            {
                logger.Info(PAYMENT_LOG, "UpdateSettings: Starting to update settings.");
                Payment payment = JsonConvert.DeserializeObject<Payment>(configJson);

                //If the init was called and in the parameters the overwrite is True 
                //then modify all the settings to the new value
                foreach (AdminPeripheralSetting paymentSetting in payment.ConfigurationSettings)
                {
                    switch (paymentSetting.RealName)
                    {
                        case "PaymentDuration":
                            paymentDuration = paymentSetting;
                            break;
                        
                        case "PaymentExecuteCommandResult":
                            paymentExecuteCommandResult = paymentSetting;
                            break;

                        case "PaymentResult":
                            paymentResult = paymentSetting;
                            break;

                        case "TenderMedia":
                            paymentTenderMediaID = paymentSetting;
                            break;

                        case "PORT":
                            comPort = paymentSetting;
                            break;
                    }
                }
                logger.Info(PAYMENT_LOG, "UpdateSettings: Finished to update settings.");
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to update payment settings.\r\n{0}", ex.ToString()));
                return false;
            }

            return true;
        }
  
        /// <summary>
        /// Stop the current payment application
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            logger.Info(PAYMENT_LOG, "Unload: started unloading payment.");
            try
            {
                //Close the pipe server
                if (communicator != null)
                    communicator.Close();

                //Stop the payment application
                Process[] processesByName = Process.GetProcessesByName(PAYMENT_APPLICATION_PROCESS_NAME);
                if (processesByName.Length > 0)
                {
                    processesByName[0].Kill();
                    logger.Info(PAYMENT_LOG, "Unload: finished unloading payment.");
                    return true;
                }      
                else
                    return true;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("    {0}", ex.ToString()));
            }
            logger.Info(PAYMENT_LOG, "Unload: finished unloading payment.");
            return false;
        }

        /// <summary>
        /// Method providing factory details
        /// </summary>
        /// <returns></returns>
        public string GetPaymentFactoryDetails()
        {
            logger.Info(PAYMENT_LOG, "GetPaymentFactoryDetails: Started.");

            //clean the payment configuration
            currentPaymentInitConfig.ConfigurationSettings.Clear();
            //Add the payment duration, payment result and payment tender media
            currentPaymentInitConfig.ConfigurationSettings.Add(comPort);
            //currentPaymentInitConfig.ConfigurationSettings.Add(paymentDuration);
            //currentPaymentInitConfig.ConfigurationSettings.Add(paymentResult);
            //currentPaymentInitConfig.ConfigurationSettings.Add(paymentCancelResult);            
            //currentPaymentInitConfig.ConfigurationSettings.Add(paymentExecuteCommandResult);            
            //currentPaymentInitConfig.ConfigurationSettings.Add(paymentTenderMediaID);

            JObject result = new JObject();
            JArray printersArray = new JArray();

            printersArray.Add(JObject.FromObject(currentPaymentInitConfig));

            result["Payment"] = printersArray;

            logger.Info(PAYMENT_LOG, "GetPaymentFactoryDetails: Finished.");

            return result.ToString();
        }

        #endregion

        public void InitResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                //Check the status property of the parameters object to see if the Init was successful
                if (responseParameters.Status == 0)
                    WasInitSuccessful = true;
                else
                    WasInitSuccessful = false;

                //Notify the Init method that the message has stopped
                IsInitFinished = true;

            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to validate the InitResponse. \r\n{0}", ex.ToString()));
            }
        }

        public void TestResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                //Check the status property of the parameters object to see if the Test was successful
                if (responseParameters.Status == 0)
                    WasTestSuccessful = true;
                else
                    WasTestSuccessful = false;

                //Notify the Init method that the message has stopped
                IsTestFinished = true;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to validate the TestResponse. \r\n{0}", ex.ToString()));
            }
        }

        public void PayResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                //Check the status property of the parameters object to see if the Pay was successful
                if (responseParameters.Status == 0)
                {
                    //Update the payment details with the ones received from the payment terminal
                    payDetails = responseParameters.PayDetailsExtended;
                    WasPaySuccessful = true;
                }
                else
                    WasPaySuccessful = false;

                //Update the error code and error description
                specificStatusDetails.StatusCode = responseParameters.Status;
                specificStatusDetails.Description = responseParameters.Description;

                //Notify the Pay method that the message has stopped
                IsPayFinished = true;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to validate the PayResponse. \r\n{0}", ex.ToString()));
            }
        }
        
        public void ProgressMessageResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                PayProgress payProgress = responseParameters.PayProgress;

                if (PaymentCallbacks != null)
                    PaymentCallbacks.PayProgressChangedCallback(payProgress);
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to send Progress Message Response. \r\n{0}", ex.ToString()));
            }
        }

        public void CancelResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                //Check the status property of the parameters object to see if the Cancel was successful
                if (responseParameters.Status == 0)
                    WasCancelSuccessful = true;
                else
                    WasCancelSuccessful = false;

                //Update the error code and error description
                specificStatusDetails.StatusCode = responseParameters.Status;
                specificStatusDetails.Description = responseParameters.Description;

                //Notify the Cancel method that the message has stopped
                IsCancelFinished = true;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to validate the CancelResponse. \r\n{0}", ex.ToString()));
            }
        }

        /// <summary>
        /// Method that is triggered when the execute command response is received
        /// </summary>
        /// <param name="parameters"></param>
        public void ExecuteCommandResponse(object parameters)
        {
            try
            {
                ResponseParameters responseParameters = JsonConvert.DeserializeObject<ResponseParameters>(parameters.ToString());

                this.executeCommandDetails = responseParameters.ExecuteCommandResponse;

                //Check the status property of the parameters object to see if the ExecuteCommand call was successful
                if (responseParameters.Status == 0)
                    WasExecuteCommandSuccessful = true;
                else
                    WasExecuteCommandSuccessful = false;

                specificStatusDetails.StatusCode = responseParameters.Status;
                specificStatusDetails.Description = responseParameters.Description;

                //Notify the ExecuteCommand method that the message has stopped
                IsExecuteCommandFinished = true;
            }
            catch (Exception ex)
            {
                logger.Error(PAYMENT_LOG, string.Format("Failed to validate the ExecuteCommand response. \r\n{0}", ex.ToString()));
            }
        }

        #region Private Methods

        /// <summary>
        /// Check if the Payment Application is started and if it is stop it and start it again
        /// </summary>
        private void LaunchPaymentApplication()
        {
            try
            {
                //Stop the payment application
                Process[] processesByName = Process.GetProcessesByName(PAYMENT_APPLICATION_PROCESS_NAME);
                if (processesByName.Length > 0)
                    processesByName[0].Kill();

                //Start the application
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = true;
                startInfo.FileName = PAYMENT_APPLICATION_NAME;
                startInfo.WorkingDirectory = driverLocation;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("StartPaymentApplication: \r\n{0}", ex.ToString()));
            }
        }

        #endregion
    }
}
