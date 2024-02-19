using System;
using System.Windows;

namespace Wordle {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            HandleException(ex);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true; // Mark the exception as handled
        }

        private void HandleException(Exception ex)
        {
            Console.WriteLine($"Unhandled Exception: {ex.Message}");
            // 这里可以添加其他处理逻辑，比如记录日志或者弹出错误提示框
        }

        [STAThread]
        public static void Main() {
            Console.WriteLine("Starting...");
            try {
                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception e) {
                Console.WriteLine($"An exception occurred: {e.Message}");
                Console.WriteLine($"Exception details: {e.StackTrace}");
            }
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception e = (Exception)args.ExceptionObject;
                Console.WriteLine($"Unhandled Exception: {e.Message}");
                // 这里可以添加其他处理逻辑
            };
        }
    }
}