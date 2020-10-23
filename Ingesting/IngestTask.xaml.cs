using System;
using System.Windows.Controls;

namespace DCIMIngester.Ingesting
{
    public partial class IngestTask : UserControl
    {
        public IngestTaskContext Context { get; private set; }
        public TaskStatus Status { get; set; } = TaskStatus.Prompting;

        public event EventHandler Dismissed;


        public IngestTask(IngestTaskContext context)
        {
            Context = context;
            InitializeComponent();
        }

        public void Load()
        {
            Pages.Prompt promptPage = new Pages.Prompt(this);
            promptPage.Dismissed += Page_Dismissed;
            FramePageHost.Navigate(promptPage);
        }
        private void Page_Dismissed(object sender, Pages.PageDismissedEventArgs e)
        {
            switch (e.Action)
            {
                case Pages.PageDismissedEventArgs.PageDismissAction.PromptCancel:
                    Dismissed?.Invoke(this, new EventArgs());
                    break;

                case Pages.PageDismissedEventArgs.PageDismissAction.PromptIngest:
                    Status = TaskStatus.Ingesting;

                    // Swap out prompt page for ingest page to carry out the ingest operation
                    Pages.Ingest ingestPage = new Pages.Ingest(this);
                    ingestPage.Dismissed += Page_Dismissed;
                    FramePageHost.Navigate(ingestPage);
                    break;


                case Pages.PageDismissedEventArgs.PageDismissAction.IngestDismiss:
                    Dismissed?.Invoke(this, new EventArgs());
                    break;
            }
        }


        public enum TaskStatus { Prompting, Ingesting, Completed, Failed, Cancelled };
    }
}
