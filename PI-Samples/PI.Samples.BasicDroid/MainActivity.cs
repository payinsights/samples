using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using PI.SDK.Services.Switch;
using PI.SDK;
using System.Collections.Generic;
using PI.SDK.Devices;
using PI.SDK.Devices.BC;
using PI.SDK.Services.Auth;
using PI.SDK.Shared;
using PI.SDK.Devices.Mobile;

namespace PI.Samples.BasicDroid
{
    //Most of the code here are the same as the PI.Samples.BasicConsole, so we will skip commenting everything. For a full comment code, look at PI.Samples.BasicConsole
    //In this sample, we assume that you have a terminal already paired with the Android device.
    //Since there are several methods to discovery/scan/pair a bluetooth device by code, and there are plenty of examples over the web, we will just assume it was paired using the Android Settings > Bluetooth.
    //Remember that in order to the application and the SDK use Bluetooth, it must have have permissions to access the Bluetooth hardware.
    //In order to give access to BT for the application/SDK, add the BLUETOOTH and BLUETOOTH_ADMIN permissions to the Android Manifest
    [Activity(Label = "PI.Samples.BasicDroid", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        private string _appId = "<ADD YOUR APPLICATION ID HERE>";
        private string _appSecret = "<ADD YOUR APPLICATION SECRET HERE>";
        private string _customerId = "<ADD YOUR CUSTOMER ID HERE>";
        private string _token;

        private ITransactionOrchestrator _transactionOrchestrator;
        private IMobileDeviceManager _device;
        private IBCDeviceConnectionChannel _channel;
        private string _deviceAddress = "<ADD YOUR PINPAD/TERMINAL MAC ADDRESS HERE>"; 

        private Spinner _spinnerTransactionType;
        private Spinner _spinnerParcels;
        private EditText _editTextValue;
        private ProgressDialog _progressDialog;
        private AlertDialog dialog;
        private TransactionResult _result;

        protected override void OnCreate(Bundle bundle)
        {
            try
            {
                base.OnCreate(bundle);
                SetContentView(Resource.Layout.Main);
                SetupViewComponents();

                //Since the only way to connect to a terminal/pinpad from an Android Phone/Tablet is over Bluetooth, we use the BluetoothDeviceConnectionChannel.
                //It has the same implementation as SerialDeviceConnectionChannel(both implement IBCDeviceConnectionChannel) from the console sample but uses only Bluetooth to connect to the device. 
                _channel = new BluetoothDeviceConnectionChannel(_deviceAddress);
                //The BCDeviceManager is the same as in the console sample
                _device = new BCDeviceManager(_channel);
                //You can listen to errors form the device only, just wire up this event
                _device.OnError += DeviceManagerOnError;

                //The same platform initialization as in console
                PlatformContext.Initialize(this, _customerId, _appId, _appSecret, _device, PIEnvironment.Simulator, "guto_surface3");

                _progressDialog = ProgressDialog.Show(this, "PI", "Logging in...", true);

                //Login works the same way as console sample
                _token = PlatformContext.GetService<IAuthService>().Login();

                //Wire up the transaction flow events
                _transactionOrchestrator = PlatformContext.GetService<ITransactionOrchestrator>();
                _transactionOrchestrator.OnError += TransactionOrchestratorOnError;
                _transactionOrchestrator.OnProgressChanged += TransactionOrchestratorOnProgressChanged;
                _transactionOrchestrator.OnTransactionFinished += TransactionOrchestratorOnTransactionFinished;
                _transactionOrchestrator.OnDataRequested += TransactionOrchestratorOnDataRequested;
            }
            catch (Exception ex)
            {
                Helpers.Alert(this, "PI - OnCreate - Error", ex.Message, false);
            }
            finally
            {
                if (_progressDialog.IsShowing)
                {
                    _progressDialog.Dismiss();
                }
            }
        }

        private void SetupViewComponents()
        {
            _spinnerTransactionType = FindViewById<Spinner>(Resource.Id.spinnerTransactionType);
            var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.transaction_type_items, Android.Resource.Layout.SimpleSpinnerItem);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _spinnerTransactionType.Adapter = adapter;

            _spinnerParcels = FindViewById<Spinner>(Resource.Id.spinnerParcels);
            var adapterParcels = ArrayAdapter.CreateFromResource(this, Resource.Array.transaction_parcels, Android.Resource.Layout.SimpleSpinnerItem);
            adapterParcels.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _spinnerParcels.Adapter = adapterParcels;

            _editTextValue = FindViewById<EditText>(Resource.Id.editText1);

            var buttonSend = FindViewById<Button>(Resource.Id.buttonSend);
            buttonSend.Click += SendClick;
        }

        private void DeviceManagerOnError(PIException obj)
        {
            if (_progressDialog.IsShowing)
            {
                _progressDialog.Dismiss();
            }
            Helpers.Alert(this, "PI - DeviceManagerOnError - Error", obj.Message, false);
        }

        //Push data when the platform requests, works the same way as in console.
        //For the sake of this sample, we added here an Android fragment(a popup) that ask the user to enter the CVV when using a Magnetic stripe card.
        private void TransactionOrchestratorOnDataRequested(CollectInputParameters obj)
        {
            try
            {
                if (_progressDialog.IsShowing)
                {
                    _progressDialog.Dismiss();
                }

                switch (obj.Reference)
                {
                    case "CVV":
                        var cvvDialog = InputDialogFragment.NewInstance(this, "PI", obj.Message, (cvvValue) =>
                        {
                            string result = cvvValue == 0 ? "" : cvvValue.ToString();
                            var cvvParameter = new CollectOutputParameters() { Reference = obj.Reference, Value = result };
                            _transactionOrchestrator.PushData(cvvParameter);
                        });
                        cvvDialog.Show(FragmentManager, "fragment_cvv");
                        break;
                    case "CVVState":
                        var items = new List<string>();
                        foreach (var item in obj.Options)
                        {
                            items.Add(item.Value);
                        }

                        AlertDialog.Builder builder = new AlertDialog.Builder(this);
                        builder.SetTitle(obj.Message)
                               .SetItems(items.ToArray(), (sender, e) =>
                               {
                                   var selectedValue = items[e.Which];
                                   foreach (var item in obj.Options)
                                   {
                                       if (item.Value == selectedValue)
                                       {
                                           var cvvStateParameter = new CollectOutputParameters() { Reference = obj.Reference, Value = item.Key.ToString() };
                                           _transactionOrchestrator.PushData(cvvStateParameter);
                                           break;
                                       }
                                   }
                               });

                        dialog = builder.Create();
                        dialog.Show();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Helpers.Alert(this, "PI - TransactionOrchestratorOnDataRequested - Error", ex.Message, false);
            }
        }

        private void CancelClick(object sender, EventArgs e)
        {
            try
            {
                _transactionOrchestrator.CancelTransaction(Guid.Empty, "");
            }
            catch (Exception ex)
            {
                Helpers.Alert(this, "PI - CancelClick - Error", ex.Message, false);
            }
        }

        private void SendClick(object sender, EventArgs e)
        {
            try
            {
                _result = null;
                if (string.IsNullOrWhiteSpace(_editTextValue.Text))
                {
                    Toast.MakeText(this, "The field value is required.", ToastLength.Short).Show();
                    return;
                }

                decimal value = decimal.Parse(_editTextValue.Text);
                var mode = _spinnerTransactionType.SelectedItemPosition == 0 ? TransactionMode.Credit : TransactionMode.Debit;
                short parcel = short.Parse((_spinnerParcels.SelectedItemPosition + 1).ToString());

                //Just send the transaction the same way you do on console
                var parameters = new TerminalTransactionParameters();
                parameters.Mode = mode;
                parameters.Amount = value;
                parameters.CashbackValue = 0;
                parameters.Parcel = parcel;
                parameters.Type = TransactionType.Authorization;

                _transactionOrchestrator.ExecuteTransaction(parameters, _token);
            }
            catch (Exception ex)
            {
                _progressDialog.Dismiss();
                Helpers.Alert(this, "PI - SendClick - Error", ex.Message, false);
            }
        }

        private void TransactionOrchestratorOnTransactionFinished(TransactionResult obj)
        {
            _result = obj;
        }

        private void TransactionOrchestratorOnProgressChanged(TransactionProgress obj)
        {
            try
            {
                string message = string.Empty;
                if (_progressDialog.IsShowing)
                {
                    _progressDialog.Dismiss();
                }

                switch (obj)
                {
                    case TransactionProgress.InsertOrSwipeCard:
                        message = "Insira ou passe o cartão";
                        break;
                    case TransactionProgress.EnterPassword:
                        message = "Digite a senha";
                        break;
                    case TransactionProgress.RemoveCard:
                        message = "Retire o cartão";
                        if (_result != null)
                        {
                            Helpers.Alert(this, "PI - " + _result.OperatorMessage, _result.RAWClientReceipt.Replace('~','\r'), false, null);
                            return;
                        }
                        break;
                    case TransactionProgress.UpdateTable:
                        message = "Atualizando tabelas...";
                        break;
                    case TransactionProgress.Connecting:
                        message = "Conectando ao servidor...";
                        break;
                    case TransactionProgress.Processing:
                        message = "Processando...";
                        break;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    _progressDialog = ProgressDialog.Show(this, "PI - Transaction", message, true);
                }
            }
            catch (Exception ex)
            {
                Helpers.Alert(this, "PI - TransactionOrchestratorOnProgressChanged - Error", ex.Message, false);
            }
        }

        private void TransactionOrchestratorOnError(PIException exception)
        {
            _progressDialog.Dismiss();

            if (exception is PITransactionTimeoutException)
            {
                Helpers.Alert(this, "PI - Transaction Error", "Servidor não responde.", false);
            }
            else
            {
                Helpers.Alert(this, "PI - Transaction Error", exception.Message, false);
            }
        }
    }
}

