using System.Windows;
using System.Windows.Threading;

namespace Wordle {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        [STAThread]
        public static void Main() {
            Console.WriteLine("Starting...");
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}