using System.Windows;
using System.Windows.Controls;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Desktop.Presentation.ViewModels;

namespace ModernInventory.Desktop.Presentation.Views
{
    public partial class PurchasesView : UserControl
    {
        public PurchasesView()
        {
            InitializeComponent();
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ProductListItemDto product && DataContext is PurchasesViewModel vm)
            {
                vm.AddToCart(product);
                cb.SelectedItem = null;
            }
        }
    }
}
