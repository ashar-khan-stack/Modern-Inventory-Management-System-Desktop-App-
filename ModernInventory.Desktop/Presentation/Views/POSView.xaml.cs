using System.Windows.Controls;
using System.Windows.Input;
using ModernInventory.Desktop.Presentation.ViewModels;

namespace ModernInventory.Desktop.Presentation.Views
{
    public partial class POSView : UserControl
    {
        public POSView()
        {
            InitializeComponent();
        }

        private void BarcodeScannerBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is POSViewModel vm)
                {
                    vm.HandleBarcodeOrSearchCommand.Execute(null);
                }
            }
        }
    }
}
