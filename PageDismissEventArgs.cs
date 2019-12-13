using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dcim_ingester
{
    public class PageDismissEventArgs : EventArgs
    {
        public string DismissMessage { get; private set; }

        public PageDismissEventArgs(string dismissMessage)
        {
            DismissMessage = dismissMessage;
        }
    }
}
