using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace dcim_ingester.IngesterTaskPages
{
    public partial class IngesterPageStart : Page
    {
        public event EventHandler<PageDismissEventArgs> OnPageDismiss;
        private string VolumeLabel = "";

        public IngesterPageStart(string volumeLabel)
        {
            InitializeComponent();
            VolumeLabel = volumeLabel;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            OnPageDismiss?.Invoke(
                this, new PageDismissEventArgs("IngesterPageStart.Yes"));
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            OnPageDismiss?.Invoke(
                this, new PageDismissEventArgs("IngesterPageStart.No"));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            lab.Text = string.Format("A device ({0}) containing a DCIM folder has been connected. Do you want to ingest files from it?", VolumeLabel);
        }
    }
}
