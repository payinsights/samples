using System;

using Android.App;

namespace PI.Samples.BasicDroid
{
    /// <summary>
    /// Just a dumb helper class to avoid duplicated code on the sample.
    /// </summary>
    public static class Helpers
    {
        public static void Alert(Activity context, string title, string message, bool CancelButton, Action<Result> callback = null)
        {
            context.RunOnUiThread(() =>
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(context);
                builder.SetTitle(title);
                builder.SetMessage(message);

                if (callback != null)
                {
                    builder.SetPositiveButton("Ok", (sender, e) =>
                    {
                        callback(Result.Ok);
                    });

                    if (CancelButton)
                    {
                        builder.SetNegativeButton("Cancel", (sender, e) =>
                        {
                            callback(Result.Canceled);
                        });
                    }
                }
                else
                {
                    builder.SetPositiveButton("OK", (sender, e) => { });
                }

                builder.Show();
            });
        }

        public static void AlertYesOrNo(Activity context, string title, string message, Action<Result> callback)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(context);
            builder.SetTitle(title);
            builder.SetMessage(message);

            if (callback != null)
            {
                builder.SetPositiveButton("Yes", (sender, e) =>
                {
                    callback(Result.Ok);
                });

                builder.SetNegativeButton("No", (sender, e) =>
                {
                    callback(Result.Canceled);
                });
            }
            else
            {
                builder.SetPositiveButton("OK", (sender, e) => { });
            }

            builder.Show();
        }
    }
}