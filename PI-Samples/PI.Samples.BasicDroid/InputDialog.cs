using System;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace PI.Samples.BasicDroid
{
    public class InputDialogFragment : DialogFragment
    {
        private static Action<int> _actionOk;
        private View _view;
        private EditText _editTextCvvValue;
        private TextView _textViewMessage;
        private Button _buttonOk;
        private static Activity _context;

        public InputDialogFragment()
        {

        }

        public static InputDialogFragment NewInstance(Activity context, string title, string message, Action<int> okAction)
        {
            var frag = new InputDialogFragment();
            var args = new Bundle();
            args.PutString("title", title);
            args.PutString("message", message);
            frag.Arguments = args;
            _actionOk = okAction;
            _context = context;
            return frag;
        }
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            try
            {
                _view = inflater.Inflate(Resource.Layout.InputDialog, container);
                _editTextCvvValue = (EditText)_view.FindViewById(Resource.Id.editTextCvvValue);
                _textViewMessage = (TextView)_view.FindViewById(Resource.Id.textViewMessage);
                _buttonOk = _view.FindViewById<Button>(Resource.Id.buttonOk);
                _buttonOk.Click += ButtonOkClick;
                string title = this.Arguments.GetString("title", "PI");
                string message = this.Arguments.GetString("message", "Input the value");
                this.Dialog.SetTitle(title);
                _textViewMessage.Text = message;
                _editTextCvvValue.RequestFocus();
                this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);
            }
            catch (Exception ex)
            {
                Helpers.Alert(_context, "PI - CvvInputFragment - OnCreateView", ex.Message, false);
            }
            return _view;
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            int value = 0;
            int.TryParse(_editTextCvvValue.Text.Trim(), out value);
            this.Dismiss();
            _actionOk(value);
        }
    }
}