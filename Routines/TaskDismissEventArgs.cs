using System;

namespace DCIMIngester.Routines
{
    public class TaskDismissEventArgs : EventArgs
    {
        public IngesterTask Task { get; private set; }

        public TaskDismissEventArgs(IngesterTask task)
        {
            Task = task; ;
        }
    }
}
