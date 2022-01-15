using System.Collections.Concurrent;

namespace Governor
{
    public class AsyncAutoResetEvent
    {
        private ConcurrentQueue<TaskCompletionSource> Waits { get; set; } = new ConcurrentQueue<TaskCompletionSource>();
        private bool Signaled { get; set; }
        private object SyncRoot { get; set; } = new object();

        public Task WaitAsync()
        {
            lock (SyncRoot)
            {
                if (Signaled)
                {
                    Signaled = false;
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource();
                Waits.Enqueue(tcs);
                return tcs.Task;
            }
        }

        public void Set()
        {
            TaskCompletionSource toRelease = null;

            lock (SyncRoot)
            {
                if (!Waits.TryDequeue(out toRelease) && !Signaled)
                {
                    Signaled = true;
                }
            }

            toRelease?.SetResult();
        }
    }
}
