using System.Windows;
using STR_ART_V.ViewModel;

namespace STR_ART_V.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Połączenie ViewModelu z View
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
        }
    }
}