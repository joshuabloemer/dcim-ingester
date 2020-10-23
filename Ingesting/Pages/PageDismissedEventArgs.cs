using System;

namespace DCIMIngester.Ingesting.Pages
{
    public class PageDismissedEventArgs : EventArgs
    {
        public PageDismissAction Action { get; private set; }

        public PageDismissedEventArgs(PageDismissAction action)
        {
            Action = action;
        }

        public enum PageDismissAction { PromptCancel, PromptIngest, IngestDismiss }
    }
}
