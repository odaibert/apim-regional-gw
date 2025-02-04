using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DeviceEmulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// OnStartup handler.
        /// </summary>
        /// <param name="args">Event args</param>
        protected override void OnStartup(StartupEventArgs args)
        {
            // Global exception handling  
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            DispatcherUnhandledException += HandleDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_HandleUnobservedTaskException;
        }

        /// <summary>
        /// Event handler for the &quot;AppDomain.CurrentDomain.UnhandledException&quot;.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="args">Event args</param>
        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            ShowUnhandledException((Exception) args.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
        }

        /// <summary>
        /// Event handler for the &quot;DispatcherUnhandledExceptionn&quot;.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="args">Event args</param>
        private void HandleDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            ShowUnhandledException(args.Exception, "DispatcherUnhandledException");
            args.Handled = true;
        }

        /// <summary>
        /// Event handler for the &quot;TaskScheduler.UnobservedTaskException&quot;.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="args">Event args</param>
        private void TaskScheduler_HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            args.SetObserved();
        }

        /// <summary>
        /// Handling global exception/error.
        /// </summary>
        /// <param name="ex">Exception that occured</param>
        /// <param name="event">Event data</param>
        private void ShowUnhandledException(Exception ex, string @event)
        {
            string errorMessage = $"An unhandled application error '{@event}' occurred [{ex.Message + (ex.InnerException != null ? "\n" + ex.InnerException.Message : null)}]";

            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Current.Shutdown();
            }
        }
    }
}
