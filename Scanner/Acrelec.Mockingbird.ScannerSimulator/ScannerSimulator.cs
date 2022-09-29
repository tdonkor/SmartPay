using System;
using System.Timers;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Acrelec.Mockingbird.Feather.Peripherals;
using Acrelec.Mockingbird.Interfaces.Peripherals;
using Acrelec.Mockingbird.Feather.Peripherals.Enums;
using Acrelec.Mockingbird.Feather.Peripherals.Models;
using Acrelec.Mockingbird.Feather.Peripherals.Scanner;
using Acrelec.Mockingbird.Feather.Peripherals.Settings;
using Acrelec.Mockingbird.Feather.Peripherals.Scanner.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Acrelec.Mockingbird.ScannerSimulator
{
    [Export(typeof(IScanner))]
    public class ScannerSimulator : IScanner, IScannerExtendsContinuousScan, IScannerExtendsStatusChangedCallbacks 
    {
        private const string SCANNER_LOG = "ScannerSimulator";

        private const string SCANNER_NAME = "SIMULATOR_SCANNER";

        private const string SCANNER_ID = "0";

        private const PeripheralType SCANNER_TYPE = Feather.Peripherals.Enums.PeripheralType.scanner;

        /// <summary>
        /// Object that is in charge with the error and info logs
        /// </summary>
        private ILogger _logger { get; set; }

        /// <summary>
        /// Object that will lock the Scanned Value object until it is updated by the thread that is using it
        /// </summary>
        private object _lockScannedValueObject { get; set; }

        /// <summary>
        /// Object that will lock the Scanned Date object until it is updated by the thread that is using it
        /// </summary>
        private object _lockScannedDateObject { get; set; }

        /// <summary>
        /// Object that will lock the Scan Counter object until it is updated by the thread that is using it
        /// </summary>
        private object _lockScanCounter { get; set; }

        /// <summary>
        /// The timeout that the NewScan method will wait for a scanned data.
        /// This value will be decremented when each second passes
        /// and will be incremented when a new scan is triggered before the last one elapsed
        /// </summary>
        int _scanSecondsInterval;

        /// <summary>
        /// Timer that will be executed in a loop until a barcode is scanned or until the scan counter reaches 0
        /// </summary>
        System.Timers.Timer _scanElapsedInterval;

        /// <summary>
        /// Timer that will be used to simulate a random data scan
        /// </summary>
        System.Timers.Timer _continuousScanTimer;

        /// <summary>
        /// Data received from scanner
        /// </summary>
        private string _receivedData;

        /// <summary>
        /// Object will hold the current status of the scanner
        /// </summary>
        private ScannerStatus _scannerStatus;

        /// <summary>
        /// Flag used to keep the result of the init method.
        /// It is used usually by the Test.
        /// </summary>
        private bool _wasInitSuccessfull;

        /// <summary>
        /// The name of the scanner
        /// </summary>
        private AdminPeripheralSetting _scannerName;

        /// <summary>
        /// The type of connection with the scanner
        /// </summary>
        private AdminPeripheralSetting _connectionType;

        /// <summary>
        /// Scan duration in seconds
        /// </summary>
        private AdminPeripheralSetting _scanDuration;

        /// <summary>
        /// Object containg the scanned value that will be retured
        /// </summary>
        private AdminPeripheralSetting _scanResult;

        /// <summary>
        /// Flag used to control the scan result.
        /// true - the scan will return the given specified value
        /// False - the scan will return no value (fail scan)
        /// </summary>
        private AdminPeripheralSetting _isScanSuccessful;

        /// <summary>
        /// Flag used to alternate the status of the scanner. 
        /// By macking the flag "true" each 5 seconds the scanner status will change from OK to NOK and vise versa
        /// </summary>
        private AdminPeripheralSetting _hasLiveStatusUpdate;

        /// <summary>
        /// Timer used to alternate the scanner status
        /// </summary>
        private System.Timers.Timer _alternateStatusTimer;

        /// <summary>
        /// Object containing the current scanner configuration. 
        /// Object contains the scanner details as well as the list of the ATP Admin settings
        /// </summary>
        private Scanner _currentScannerInitConfig;

        /// <summary>
        /// Flag that will make te scanner send data that was scanned via Callback
        /// and not wait for the "Scan" method to be triggered
        /// </summary>
        public bool IsContinuouScanEnabled { get; set; }

        /// <summary>
        /// Object containing notification methods used to send scanned data to the Core.
        /// </summary>
        public IScannerCallback Callbacks { get; set; }

        /// <summary>
        /// Object containing notification methods used to send status updates to the Core as they happen.
        /// This will result in faster more realistic status updates because the updates happens as the status is 
        /// changed and not after the "Test" method is triggered by the Core
        /// </summary>
        public IPeripheralStatusChangedCallbacks StatusChangedCallbacks { get; set; }

        /// <summary>
        /// Scanner status object.
        /// Used by the core and the applications to know the current state of the device
        /// </summary>
        public PeripheralStatus LastStatus => _scannerStatus.ToPeripheralStatus();

        /// <summary>
        /// The Core API required for all the scanner methods to work.
        /// This is used by the Core to show
        /// </summary>
        public int MinAPILevel => 9;

        /// <summary>
        /// The version of the driver
        /// </summary>
        public string DriverVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// The ID of the scanner.
        /// Value is provided when creating the driver entry in the Library web interface.
        /// </summary>
        public string DriverId => SCANNER_ID;

        /// <summary>
        /// The name of the scanner. 
        /// This value will be the name used by the core applications, web interfaces to address the current driver
        /// </summary>
        public string PeripheralName => SCANNER_NAME;

        /// <summary>
        /// The type of the scanner
        /// </summary>
        public string PeripheralType => SCANNER_TYPE.ToString();

        /// <summary>
        /// Contains properties specific for this type of peripheral.
        /// Currently an empty object (No such properties are currently available for a scanner).
        /// </summary>
        public ScannerCapability Capability
        {
            get;
        }

        /// <summary>
        /// Property that represents the time when the last scan event was finished.
        /// </summary>
        public DateTime ScannedDate
        {
            get
            {
                lock (_lockScannedDateObject)
                {
                    return scannedDate;
                }
            }
        }
        private DateTime scannedDate;

        /// <summary>
        /// Property that represents the value of the scanned barcode.
        /// </summary>
        public string ScannedValue
        {
            get
            {
                lock (_lockScannedValueObject)
                {
                    return scannedValue;
                }
            }
        }
        private string scannedValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        [ImportingConstructor]
        public ScannerSimulator(ILogger logger = null)
        {
            _logger = logger;        
            _scannerStatus = new ScannerStatus();

            _alternateStatusTimer = new System.Timers.Timer();
            _alternateStatusTimer.Interval = 5000;
            _alternateStatusTimer.Elapsed += AlternateStatusTimer_Elapsed;
            _alternateStatusTimer.Stop();

            _lockScannedValueObject = new object();
            _lockScannedDateObject = new object();
            _lockScanCounter = new object();

            _scanElapsedInterval = new System.Timers.Timer(1000);
            _scanElapsedInterval.Elapsed += ScanElapsedInterval_Elapsed;

            _continuousScanTimer = new System.Timers.Timer();
            _continuousScanTimer.Elapsed += ScannedDataTimer_Elapsed;
            
            _scannerName = new AdminPeripheralSetting();
            _scannerName.ControlType = SettingDataType.String;
            _scannerName.ControlName = "Scanner Name";
            _scannerName.CurrentValue = SCANNER_NAME;
            _scannerName.IsReadOnly = true;
            _scannerName.ControlDescription = "The name of the scanner.";

            _connectionType = new AdminPeripheralSetting();
            _connectionType.ControlType = SettingDataType.String;
            _connectionType.ControlName = "Connection Type";
            _connectionType.CurrentValue = "None";
            _connectionType.IsReadOnly = true;
            _connectionType.ControlDescription = "The connection type of the scanner.";

            _scanDuration = new AdminPeripheralSetting();
            _scanDuration.ControlType = SettingDataType.Int;
            _scanDuration.ControlName = "Scan Duration";
            _scanDuration.CurrentValue = "5";
            _scanDuration.ControlDescription = "Duration of scan until a successful or failed result is returned. The minimum value you can set is 5 seconds.";

            _scanResult = new AdminPeripheralSetting();
            _scanResult.ControlType = SettingDataType.String;
            _scanResult.ControlName = "Barcode Content";
            _scanResult.CurrentValue = "12345678932";
            _scanResult.ControlDescription = "The content that will be returned by the scan call.";

            _isScanSuccessful = new AdminPeripheralSetting();
            _isScanSuccessful.ControlType = SettingDataType.Bool;
            _isScanSuccessful.ControlName = "Scan Result";
            _isScanSuccessful.CurrentValue = true;
            _isScanSuccessful.IsVisible = true;
            _isScanSuccessful.ControlDescription = "Flag that will determine if the scan will be successful or not.";

            _hasLiveStatusUpdate = new AdminPeripheralSetting();
            _hasLiveStatusUpdate.ControlType = SettingDataType.Bool;
            _hasLiveStatusUpdate.ControlName = "Has Live Status Update";
            _hasLiveStatusUpdate.RealName = "HasLiveStatusUpdate";
            _hasLiveStatusUpdate.CurrentValue = "False";
            _hasLiveStatusUpdate.ControlDescription = "Check if you would like for the driver to have alternating status at a 5 seconds interval";

            _currentScannerInitConfig = new Scanner
            {
                ScannerName = SCANNER_NAME,
                ConfigurationSettings = new List<AdminPeripheralSetting>(),
                Id = SCANNER_ID,
            };
        }

        /// <summary>
        /// Method called to initialize the connection with the scanner.
        /// It receives an object containinng all the settings updated from the ATP Admin which can be used to 
        /// configure the scanner connection for example enabling it.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public bool Init(string jsonString)
        {
            _logger.Info(SCANNER_LOG, "call Init");
            try
            {
                _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.Undefined;
                _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.Undefined.ToString();

                UpdateConfigSettings(jsonString);

                if (Convert.ToBoolean(_hasLiveStatusUpdate.CurrentValue))
                    _alternateStatusTimer.Start();
                else
                    _alternateStatusTimer.Stop();

                _wasInitSuccessfull = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(SCANNER_LOG, string.Format("Init : Failed initialize scanner.\r\n{0}", ex.ToString()));
                return false;
            }
            finally
            {
                _logger.Info(SCANNER_LOG, "end call Init");
            }
        }

        /// <summary>
        /// Method called when the scanner is unloaded.
        /// Here you can implement your disconnect and clean your connections.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public bool Unload()
        {
            _logger.Info(SCANNER_LOG, "call Unload");
            try
            {
                _continuousScanTimer.Stop();
                _alternateStatusTimer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(SCANNER_LOG, string.Format("Unload : Failed to unload scanner.\r\n{0}", ex.ToString()));
                return false;
            }
            finally
            {
                _logger.Info(SCANNER_LOG, "end call Unload");
            }
        }

        /// <summary>
        /// Method called by the Core to update and get the scanner status.
        /// This method is called each: 
        /// - 20 sec (when scanner is OK)
        /// - 10 sec (when scanner is NOK)
        /// -  1 min (when core is in Quite Mode)
        /// (if this is to 'stressful' for the scanner an internal check can be made to update the status).
        /// </summary>
        /// <returns></returns>
        public bool Test()
        {
            _logger.Info(SCANNER_LOG, "call Test");

            try
            {
                if (!_wasInitSuccessfull)
                {
                    _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.GeneralError;
                    _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.GeneralError.ToString();

                    return false;
                }

                _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.Ready;
                _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.Ready.ToString();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(SCANNER_LOG, string.Format("Test: Failed to test scanner.\r\n{0}", ex.ToString()));

                _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.GeneralError;
                _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.GeneralError.ToString();

                return false;
            }
            finally
            {
                _logger.Info(SCANNER_LOG, "end call Test");
            }
        }

        /// <summary>
        /// Method that will start the scanning.
        /// If another scanning process is running will add the seconds to scan to the current scan counter
        /// </summary>
        /// <param name="secondsToScan"></param>
        public void Scan(int secondsToScan)
        {
            try
            {
                lock (_lockScanCounter)
                {
                    //Multiple applications can request scan at the same time so the first thing is to check if this is a new scan
                    if (_scanSecondsInterval == 0)
                    {
                        //Get the seconds to scan sent by the application (validate that is not less then 5 seconds)
                        int secondsToPerformTheScan = Convert.ToInt32(_scanDuration.CurrentValue) > secondsToScan ? secondsToScan : Convert.ToInt32(_scanDuration.CurrentValue);                        
                        if (secondsToPerformTheScan < 5)
                            secondsToPerformTheScan = 5;

                        lock (_lockScannedValueObject)
                        {
                            scannedValue = "";
                        }

                        _receivedData = "";

                        //This task will simulate the scanning duration and response string based on the settings received on initialization
                        //(when using a real scanner this senction will not be used)
                        Task.Run(() =>
                        {
                            //wait for the time to scan to finish
                            DateTime startTime = DateTime.Now;
                            while (DateTime.Now.Subtract(startTime).TotalSeconds < secondsToPerformTheScan)
                            {
                                Thread.Sleep(1000); // wait 1 second
                            }

                            //Check if property _isScanSuccessful is set to true or false so we know what to return
                            if (Convert.ToBoolean(_isScanSuccessful.CurrentValue))
                            {
                                _receivedData = _scanResult.CurrentValue.ToString();
                            }
                        });

                        _scanSecondsInterval = secondsToScan;

                        _scanElapsedInterval.Start();

                        _logger.Info(SCANNER_LOG, "Scan was started.");
                    }
                    else
                    //If this is a scan during another scan (usually sent by a different application)
                    {
                        //Add the extra seconds to the counter 
                        if (secondsToScan > _scanSecondsInterval)
                            _scanSecondsInterval += secondsToScan - _scanSecondsInterval;

                        _logger.Info(SCANNER_LOG, string.Format("Added {0} more seconds to the counter.", _scanSecondsInterval));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info(SCANNER_LOG, string.Format("Error on method Scan.\r\n {0}.", ex.ToString()));
            }
        }

        /// <summary>
        /// Update the current settings.
        /// This method is called at device initialization (init method) to overwrite the 
        /// default settings with the ones saved in the account
        /// </summary>
        /// <param name="configJson"></param>
        /// <returns></returns>
        private bool UpdateConfigSettings(string configJson)
        {
            try
            {
                _logger.Info(SCANNER_LOG, "UpdateSettings: Starting to update settings.");
                Scanner scanner = JsonConvert.DeserializeObject<Scanner>(configJson);

                foreach (AdminPeripheralSetting scannerSetting in scanner.ConfigurationSettings)
                {
                    switch (scannerSetting.ControlName)
                    {
                        case "Scanner Name":
                            _scannerName = scannerSetting;
                            break;

                        case "Connection Type":
                            _connectionType = scannerSetting;
                            break;

                        case "Scan Duration":
                            _scanDuration = scannerSetting;
                            break;

                        case "Barcode Content":
                            _scanResult = scannerSetting;
                            break;

                        case "Scan Result":
                            _isScanSuccessful = scannerSetting;
                            break;

                        case "Has Live Status Update":
                            _hasLiveStatusUpdate = scannerSetting;
                            break;
                    }
                }
                _logger.Info(SCANNER_LOG, "UpdateSettings: Finished to update settings.");
            }
            catch (Exception ex)
            {
                _logger.Error(SCANNER_LOG, string.Format("Failed to update scanner settings.\r\n{0}", ex.ToString()));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Scan timer elapsed method that will check each second if something was scanned.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanElapsedInterval_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //Lock the counter
                lock (_lockScanCounter)
                {
                    //Exausted counter seconds and no Data was scanned
                    if (_scanSecondsInterval - 1 < 0 && string.IsNullOrEmpty(_receivedData))
                    {
                        //Lock the ScannedDate object for update
                        lock (_lockScannedDateObject)
                        {
                            scannedDate = DateTime.Now;
                        }
                         
                        _receivedData = ""; 

                        _logger.Info(SCANNER_LOG, "Nothing was scanned during the given interval.");

                        _scanSecondsInterval = 0;
                        return;
                    }
                    //Scanned in the last second
                    else if (_scanSecondsInterval - 1 < 0 && !string.IsNullOrEmpty(_receivedData))
                    {
                        //Lock the ScannedDate object for update
                        lock (_lockScannedDateObject)
                        {
                            scannedDate = DateTime.Now;
                        }
                        lock (_lockScannedValueObject)
                        {
                            scannedValue = _receivedData;
                        }
                          
                        _receivedData = "";
                          
                        _scanSecondsInterval = 0;

                        _logger.Info(SCANNER_LOG, "Something was scanned right before the counter elapsed.");
                        return;
                    }
                    //Nothing was scanned and the counter is not elapsed. Reset and check again next second
                    else
                    {
                        //If no data was scanned try check again
                        if (string.IsNullOrEmpty(_receivedData))
                        {
                            _logger.Info(SCANNER_LOG, "Check again in the next second.");
                            _scanSecondsInterval--;
                            _scanElapsedInterval.Start();
                        }
                        //If data was scanned update and stop scanning
                        else
                        {
                            //Update the value 
                            lock (_lockScannedValueObject)
                            {
                                scannedValue = _receivedData;
                            }
                            //Update the date
                            lock (_lockScannedDateObject)
                            {
                                scannedDate = DateTime.Now;
                            } 

                            _logger.Info(SCANNER_LOG, "Something was scanned.");

                            _receivedData = "";

                            _scanSecondsInterval = 0;
                             
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info(SCANNER_LOG, "Error during scanning process.\r\n{0}", ex.ToString());

                //Update the date
                lock (_lockScannedDateObject)
                {
                    scannedDate = DateTime.Now;
                } 
                _logger.Info(SCANNER_LOG, "Something was scanned.");

                _receivedData = "";

                _scanSecondsInterval = 0;                 
            }
        }

        /// <summary>
        /// Method providing detailed information about the scanner driver.
        /// It is usually used by the ATP Admin to get the details and settings of a scanner driver
        /// </summary>
        /// <returns></returns>
        public string GetScannerFactoryDetails()
        {
            JObject result = new JObject();
            JArray scannerArray = new JArray();

            //clean the payment configuration
            _currentScannerInitConfig.ConfigurationSettings.Clear();
            
            //Add the scanner duration, scanValue and successsful scan properties                
            _currentScannerInitConfig.ConfigurationSettings.Add(_scannerName);
            _currentScannerInitConfig.ConfigurationSettings.Add(_connectionType);
            _currentScannerInitConfig.ConfigurationSettings.Add(_scanDuration);
            _currentScannerInitConfig.ConfigurationSettings.Add(_scanResult);
            _currentScannerInitConfig.ConfigurationSettings.Add(_isScanSuccessful);
            _currentScannerInitConfig.ConfigurationSettings.Add(_hasLiveStatusUpdate);

            scannerArray.Add(JObject.FromObject(_currentScannerInitConfig));

            result["Scanners"] = scannerArray;
            return result.ToString();           
        }

        /// <summary>
        /// Method used to start a continous scanning
        /// </summary>
        public void StartContinuousScan()
        {
            //Start the continuous scan timer
            _continuousScanTimer.Interval = int.Parse(_scanDuration.CurrentValue.ToString()) * 1000;
            _continuousScanTimer.Start();

            IsContinuouScanEnabled = true;
        }

        /// <summary>
        /// Method used to stop a continous scan
        /// </summary>
        public void StopContinuousScan()
        {
            _continuousScanTimer.Stop();

            IsContinuouScanEnabled = false;
        }

        /// <summary>
        /// Continuous scan timer elapsed method used to simulate periodical barcode scan.
        /// And will send the result to the Core
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScannedDataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Notify the core about the data that was scanned
            Callbacks.ScannedData(_scanResult.CurrentValue.ToString());
        }

        /// <summary>
        /// Status timer elapsed method that will simulate the status change of a scanner driver.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlternateStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Alternate status
            if (_scannerStatus.CurrentStatus == (int)ScannerErrors.UnifiedErrorCodes.Ready)
            {
                _scannerStatus.Reset();
                _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.GeneralError;
                _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.GeneralError.ToString();

                if (StatusChangedCallbacks != null)
                    StatusChangedCallbacks.StatusChanged(false);
            }
            else
            {
                _scannerStatus.Reset();
                _scannerStatus.CurrentStatus = (int)ScannerErrors.UnifiedErrorCodes.Ready;
                _scannerStatus.ErrorCodesDescription = ScannerErrors.UnifiedErrorCodes.Ready.ToString();

                if (StatusChangedCallbacks != null)
                    StatusChangedCallbacks.StatusChanged(true);
            }
        }
    }
}
