﻿namespace Governor
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
    /// </summary>
    internal sealed class TokenBucket : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenBucket"/> class.
        /// </summary>
        /// <param name="capacity">The bucket capacity.</param>
        /// <param name="interval">The interval at which tokens are replenished.</param>
        public TokenBucket(long capacity, int interval)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than or equal to 1");
            }

            Capacity = capacity;
            CurrentCount = Capacity;

            Clock = new System.Timers.Timer(interval);
            Clock.Elapsed += (sender, e) => Reset();
            Clock.Start();
        }

        /// <summary>
        ///     Gets the bucket capacity.
        /// </summary>
        public long Capacity { get; private set; }

        private System.Timers.Timer Clock { get; set; }
        private long CurrentCount { get; set; }
        private bool Disposed { get; set; }
        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> WaitForReset { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Asynchronously retrieves the specified token <paramref name="count"/> from the bucket.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the requested <paramref name="count"/> exceeds the bucket <see cref="Capacity"/>, the request is lowered to
        ///         the capacity of the bucket.
        ///     </para>
        ///     <para>If the bucket has tokens available, but fewer than the requested amount, the available tokens are returned.</para>
        ///     <para>
        ///         If the bucket has no tokens available, execution waits for the bucket to be replenished before servicing the request.
        ///     </para>
        /// </remarks>
        /// <param name="count">The number of tokens to retrieve.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when tokens have been provided.</returns>
        public Task<int> GetAsync(int count, CancellationToken cancellationToken = default)
        {
            return GetInternalAsync(Math.Min(count, (int)Math.Min(int.MaxValue, Capacity)), cancellationToken);
        }

        /// <summary>
        ///     Returns the specified token <paramref name="count"/> to the bucket.
        /// </summary>
        /// <remarks>
        ///     <para>This method should only be called if tokens were retrieved from the bucket, but were not used.</para>
        ///     <para>
        ///         If the specified count exceeds the bucket capacity, the count is lowered to the capacity. Effectively this
        ///         allows the bucket to 'burst' up to 2x capacity to 'catch up' to the desired rate if tokens were wastefully
        ///         retrieved.
        ///     </para>
        ///     <para>If the specified count is negative, no change is made to the available count.</para>
        /// </remarks>
        /// <param name="count">The number of tokens to return.</param>
        public void Return(int count)
        {
            CurrentCount += Math.Min(Math.Max(count, 0), Capacity);
        }

        /// <summary>
        ///     Sets the bucket capacity to the supplied <paramref name="capacity"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="capacity">The bucket capacity.</param>
        public void SetCapacity(long capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            Capacity = capacity;
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Clock.Dispose();
                    SyncRoot.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<int> GetInternalAsync(int count, CancellationToken cancellationToken = default)
        {
            Task waitTask = Task.CompletedTask;

            await SyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // if the bucket is empty, wait for a reset, then replenish it before continuing
                // this ensures 
                if (CurrentCount == 0)
                {
                    await WaitForReset.Task;
                    WaitForReset = new TaskCompletionSource<bool>();

                    CurrentCount = Capacity;
                }

                // take the minimum of requested count or CurrentCount, deduct it from
                // CurrentCount (potentially zeroing the bucket), and return it
                var availableCount = Math.Min(CurrentCount, count);
                CurrentCount -= availableCount;
                return (int)availableCount;
            }
            finally
            {
                SyncRoot.Release();
            }
        }

        private void Reset() => WaitForReset.SetResult(true);
    }
}