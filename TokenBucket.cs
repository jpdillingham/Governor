namespace Governor
{
    public class TokenBucket
    {
        public TokenBucket(int count, int interval)
        {
            Count = count;
            CurrentCount = Count;

            Clock = new System.Timers.Timer(interval);
            Clock.Elapsed += (sender, e) =>
            {
                CurrentCount = Count;
                Report?.Invoke();
                WaitForReset?.SetResult();
            };

            Clock.Start();
        }

        private int Count { get; set; }
        private int CurrentCount { get; set; }
        private System.Timers.Timer Clock { get; set; }
        public Action Report { get; set; }
        private SemaphoreSlim SyncRoot { get; set; } = new SemaphoreSlim(1, 1);
        private TaskCompletionSource WaitForReset { get; set; }

        public async Task WaitAsync(int count)
        {
            if (count > Count)
            {
                throw new ArgumentException("Requested count exceeds max; this will deadlock");
            }

            await SyncRoot.WaitAsync();

            try
            {
                if (CurrentCount >= count)
                {
                    CurrentCount -= count;
                    return;
                }

                WaitForReset = new TaskCompletionSource();
                await WaitForReset.Task;
            }
            finally
            {
                SyncRoot.Release();
            }
        }
    }
}
