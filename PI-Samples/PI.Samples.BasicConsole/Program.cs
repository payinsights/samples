using PI.SDK;
using PI.SDK.Devices;
using PI.SDK.Devices.BC;
using PI.SDK.Devices.Mobile;
using PI.SDK.Services.Auth;
using PI.SDK.Services.Switch;
using PI.SDK.Shared;
using System;
using System.Configuration;

namespace PI.Samples.BasicConsole
{

    /// <summary>
    /// This is a sample basic card transaction application/console.
    /// THe purpuse
    /// </summary>
    class Program
    {
        #region Fields
        //AppId identify an application on a given customer. Must be unique per customer and you get this from Pay Insights Team
        private static string _appId = string.Empty;
        //Application Secret works somehow as a password for the application. Must be unique per customer and you get this from Pay Insights Team
        private static string _appSecret = string.Empty;
        //The customerId identifies the Pay Insights customers. You get this from Pay Insights Team
        private static string _customerId = string.Empty;

        //This is your access token. You will get this from AuthService.Login() call. This is used to authenticate yourself on Pay Insights Platform as a valid customer.
        private static string _token = string.Empty;

        //Transaction orchestrator is the easiest way to perform a credit/debit card transaction. 
        //Use it as your starting point since it altomatically deal with the whole EMV process, as long as Magnetic transactions.
        private static ITransactionOrchestrator _transactionOrchestrator;

        //This interface is a contract that manages the hardware implementation of your payment terminal.
        private static IDeviceManager _deviceManager;
        //This interface is a contract that describes the common communication channel between your application and payment terminals which support "Biblioteca Compartilhada"(BC) from ABECS.
        //Any device that implements BC, should use this interface to implement its communication channel.
        private static IBCDeviceConnectionChannel _channel;

        //The serial port where the terminal was detected. It can be connected to the PC from usb, serial-RS232, bluetooth, or any other way, as long as the operation system detects it as a Serial Port.
        //On Windows it is usually some COM port like COM8. On Mac/Linux, you must check which /dev/ device pointer your terminal was connected.
        //For example on Mac OSX it can be /dev/cu.MYTERMINAL or /dev/tty.MYTERMINAL, on Linux it has a similar file for it.
        private static string _port = string.Empty; 
        #endregion

        static void Main(string[] args)
        {
            #region 1. Get the application settings
            //First lets read the value of those variables from the App.config file.
            //You can read it from whatever way you want. Just remember to keep your AppSecret safe.        
            _appId = ConfigurationManager.AppSettings["AppId"];
            _appSecret = ConfigurationManager.AppSettings["AppSecret"];
            _customerId = ConfigurationManager.AppSettings["CustomerId"];
            _port = ConfigurationManager.AppSettings["Port"];
            #endregion

            #region 2. Initialize the main objects
            //Now, lets create the BC Device channel. 
            //The default implementation for Windows, Mac, and Linux, uses a serial port to communicate with the terminal/pinpad. 
            //You must set the app.config with the correct port your system is using in order to this to work.
            _channel = new SerialDeviceConnectionChannel(_port);

            //Now, create an instance of IDeviceManager. 
            //The BCDeviceManager constructor has a IBCDeviceConnectionChannel parameter so it can be used to deal with underlying device communication protocol.
            //The Device Manager is implements all the steps required process a transaction from the terminal perspective. It includes deal with EMV, Magnetic and Contactless readers and the printer.
            //The BCDeviceManager also has the BC protocol defined by ABECS fully implemented, which means that you can use whatever device you want as long as it has internally implemented the BC protocol. 
            _deviceManager = new BCDeviceManager(_channel);

            //Now lets initialize PlatformContext with the variable you previously crated for the customerId, appId, appSecret and the deviceManager.
            //The PIEnvironment sets to which platform environment the SDK will point to. Currently only Dev and Prod are supported. Note that you have AppId and Secret for each one of those platforms. 
            //This class set a context for the whole transaction process and the Initialize() method must be called only once per application execution.
            //Usually you will call this at your application startup and never call it again no matter how much transactions you will perform.
            PlatformContext.Initialize(_customerId, _appId, _appSecret, _deviceManager, PIEnvironment.Simulator, "guto_surface3");
            #endregion

            #region 3. Login into the platform
            //Now lets login into the platform to acquire an access token.
            //From the PlatformContext you can call GetService<T> to get almost all services provided by the SDK. You don't have to instantiate by yourself. 
            //In the following line we are getting the IAuthService which is responsible for all the authentication process of the platform.
            //The Login() method without parameters uses the appId and appSecret to authenticate the application. 
            //So it will return a token for a specific application back to the _token variable so it can be used to perform transactions.
            _token = PlatformContext.GetService<IAuthService>().Login();

            //Just for the sake of information we are printing the token and customerId if everything is OK.
            if (!string.IsNullOrWhiteSpace(_token))
            {
                Console.WriteLine("---=== Login ===---");
                Console.WriteLine("Access Token: " + _token);
                Console.WriteLine("Customer Id: " + PlatformContext.CustomerToken);
            }
            else
            {
                Console.WriteLine("Unauthorized. Please check your application Id and secret!");
                Console.ReadLine();
                return;
            }
            #endregion

            #region 4. Setup the Transaction Orchestrator
            //Now the most important service on the platform...
            //TrasactionOrchestrator works as (the name already said that) an orchestrator for the transaction process.
            //It implements the default transaction process for a card transaction for EMV, Magnetic and Contactless transaction, and also provide some facilities to list transaction history and cancel/confirm a transaction.
            //Again, we get an instance of it using PlatformContext.GetService<T>() method, since it will deal with all the internal deals to create it.
            _transactionOrchestrator = PlatformContext.GetService<ITransactionOrchestrator>();

            //Now we need to hook 4 events to the TransactionOrchestrator intance. It will gives you an asynchronous way to handle some important events that happens during he transaction flow.

            //OnError event will be fired whenever an error happens inside the transaction process. Depending of the erro (in most of the cases) that transaction was aborted and depending of its state, reverter by the servers on the underlying acquirer.
            _transactionOrchestrator.OnError += OnError;
            //OnProgressChanged is basically fired when the state of the transaction changed. Usually the user interface of the payment application should present a message to the user based on it.
            _transactionOrchestrator.OnProgressChanged += TransactionOrchestratorOnProgressChanged;
            //Quite self-described, this event is fired when the transaction process is finished. The event subscriber receives a TransactionResult instance, which has the final result of a given transaction.
            //Note that not always all properties of this object are filled. It depends on wether the underlying acquirer returns or not the information when the transaction is completed.
            _transactionOrchestrator.OnTransactionFinished += TransactionOrchestratorOnTransactionFinished;
            //This event is fired when the transaction process requires some user input. One use case for this event is in the transactions made by Magnetic cards. The server can request the CVV code or the PIN.
            //When this event is fired, the transaction is put on hold and will wait until a call to _transactionOrchestrator.PushData(data) is made.
            //When this event is fired, it may provide some information about the requested data that must be pushed to the server.
            _transactionOrchestrator.OnDataRequested += TransactionOrchestratorOnDataRequested;
            #endregion

            #region 4.1 (Optional) Listen for BC events
            //Totatally optional, this event is in place to comply with BC implementation. It is fired whenever the pinpad/terminal wants to notify the application about some of their actions.
            //This is not required by any means and is just here for the sake of clarity and conformance with BC.
            _channel.Notify += OnBCChannelNotify;
            #endregion

            #region 5. Perform the transaction
            //Now, the best part... Send the transaction! :)
            //Just create a TerminalTransactionParameters and set its fields based on the transaction type you want.
            var parameters = new TerminalTransactionParameters
            {
                Mode = TransactionMode.Credit,
                Type = TransactionType.Authorization,
                //The amount is a decimals, do 20 would be equivalent to R$ 20,00, and 10.5 would be R$ 10,50.
                Amount = 20,
                Parcel = 1
            };

            //Call the ExecuteTransaction(TransactionParameters, AccessToken) method in order to execute the transaction.
            _transactionOrchestrator.ExecuteTransaction(parameters, _token); 
            #endregion

            //Lets just hold the console open simulating a real application running and then disconnect when the transaction finish.
            Console.ReadLine();

            (_deviceManager as IMobileDeviceManager).Disconnect();
        }
        
        #region Transaction lifecycle events
        //This sample shows how to push the CVV data requested for a given credit card using Magnetic stripe.
        //In a real application, it should ask the user in the payment application to type the CVV and then call PushData() method to send it. 
        private static void TransactionOrchestratorOnDataRequested(CollectInputParameters obj)
        {
            Console.WriteLine("-------- Transaction OnDataRequested --------");
            Console.WriteLine("-------- Reference: " + obj.Reference + " --------");
            Console.WriteLine("-------- Message: " + obj.Message + " --------");
            switch (obj.Reference)
            {
                case "CVV":
                    var cvvResponse = new CollectOutputParameters();
                    cvvResponse.Reference = obj.Reference;
                    cvvResponse.Value = "123";
                    _transactionOrchestrator.PushData(cvvResponse);
                    break;
                case "CVVState":
                    var cvvStateResponse = new CollectOutputParameters();
                    cvvStateResponse.Reference = obj.Reference;
                    cvvStateResponse.Value = "9";
                    _transactionOrchestrator.PushData(cvvStateResponse);
                    break;
                default:
                    break;
            }

        }

        //This sample demonstrate how to use the TransactionResult object returned when a transaction is finished.
        //In a real application you may want to print the receipt for example. For the sake of this sample, we are going to print it on the console output.
        private static void TransactionOrchestratorOnTransactionFinished(TransactionResult obj)
        {
            Console.WriteLine("-------- Transaction Finished --------");
            Console.WriteLine("-------- Transaction Status ----------");
            Console.WriteLine(obj.Status);
            if (obj.Status == TransactionStatus.Authorized || obj.Status == TransactionStatus.PreAuthorized)
            {
                Console.WriteLine("-------- Client Receipt --------");
                Console.WriteLine(obj.RAWClientReceipt);
                Console.WriteLine("-------- Merchant Receipt --------");
                Console.WriteLine(obj.RAWMerchantReceipt);
            }
            Console.WriteLine("-------- Done --------");
        }

        //When this event is fired, it means that the transaction progress has changed, and the developer has a chance to notify the user of the payment application;
        //In this case we are just printing on the console output but in real applications, you should just have a progress dialog/message to notify the user of your payment application.
        private static void TransactionOrchestratorOnProgressChanged(TransactionProgress obj)
        {
            switch (obj)
            {
                case TransactionProgress.Connecting:
                    Console.WriteLine("-------- Connecting --------");
                    break;
                case TransactionProgress.Processing:
                    Console.WriteLine("-------- Processing --------");
                    break;
                case TransactionProgress.InsertOrSwipeCard:
                    Console.WriteLine("-------- Insert or Swipe a Card --------");
                    break;
                case TransactionProgress.EnterPassword:
                    Console.WriteLine("-------- Enter PIN --------");
                    break;
                case TransactionProgress.RemoveCard:
                    Console.WriteLine("-------- Remove Card --------");
                    Console.WriteLine("Press ENTER to close...");
                    break;
                case TransactionProgress.UpdateTable:
                    Console.WriteLine("-------- Update Table --------");
                    break;
            }
        }

        //This event is called when some error happened. In almost all cases, this error means that something wrong happened and the transaction was aborted, so the developer can present a message to the user;
        //In this case lets just print it on the console output.
        private static void OnError(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        private static void OnBCChannelNotify(string obj)
        {
            Console.WriteLine("BC Notify: " + obj);
        } 
        #endregion
    }
}
