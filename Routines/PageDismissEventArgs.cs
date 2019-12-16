using System;

namespace DCIMIngester.Routines
{
    public class PageDismissEventArgs : EventArgs
    {
        public string DismissMessage { get; private set; }
        public string Extra { get; set; } = null;

        public PageDismissEventArgs(string dismissMessage)
        {
            DismissMessage = dismissMessage;
        }
    }
}
