using DCIMIngester.Routines;
using System;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Ingesting.Pages.PageDismissedEventArgs;

namespace DCIMIngester.Ingesting.Pages
{
    public partial class Prompt : Page
    {
        private readonly IngestTask IngestTask;

        public event EventHandler<PageDismissedEventArgs> Dismissed;


        public Prompt(IngestTask ingestTask)
        {
            IngestTask = ingestTask;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (IngestTask.Context.VolumeLabel == "")
            {
                LabelCaption.Text = string.Format("Unnamed volume contains {0} files ({1}). Do you want to ingest them?",
                    IngestTask.Context.FilesToIngest.Count, Helpers.FormatBytes(IngestTask.Context.TotalIngestSize));
            }
            else
            {
                LabelCaption.Text = string.Format("Volume '{0}' contains {1} files ({2}). Do you want to ingest them?",
                    IngestTask.Context.VolumeLabel, IngestTask.Context.FilesToIngest.Count,
                    Helpers.FormatBytes(IngestTask.Context.TotalIngestSize));
            }

            CheckBoxDelete.IsChecked = Properties.Settings.Default.ShouldDeleteAfter;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShouldDeleteAfter = (bool)CheckBoxDelete.IsChecked;
            Properties.Settings.Default.Save();

            Dismissed?.Invoke(this, new PageDismissedEventArgs(PageDismissAction.PromptIngest));
        }
        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Dismissed?.Invoke(this, new PageDismissedEventArgs(PageDismissAction.PromptCancel));
        }
    }
}
