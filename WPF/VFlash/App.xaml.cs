using System.Windows;
using System.Windows.Threading;

namespace VFlash {
    public partial class App : Application {
        private void AppStartup(object sender, StartupEventArgs e) {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            if(e.Args.Length > 0)
                new MainWindow(e.Args[0]).Show();
            else
                new MainWindow(null).Show();
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            SystemErrorWindow.ShowDialog(e.Exception);
            e.Handled = true;
            App.Current.Shutdown();
        }
    }
}
