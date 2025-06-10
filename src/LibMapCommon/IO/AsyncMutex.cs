using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LibMapCommon.IO
{
    /// <summary>
    /// A cross-platform, asynchronous mutual exclusion lock provider.
    /// It provides a unique lock for each key (name), ensuring thread safety for resource access within this process.
    /// </summary>
    internal static class AsyncMutex
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Acquires a lock for a given name. The returned IAsyncDisposable should be disposed to release the lock.
        /// Best used with 'await using'.
        /// </summary>
        /// <param name="name">A unique name to identify the resource to be locked.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An IAsyncDisposable object that releases the lock upon disposal.</returns>
        public static async Task<IAsyncDisposable> AcquireAsync(string name, CancellationToken cancellationToken = default)
        {
            // Get or create a semaphore for the given name. This ensures that for each unique name,
            // we are always using the same semaphore instance.
            var semaphore = Semaphores.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));

            // Asynchronously wait to enter the semaphore.
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Return a new 'Releaser' disposable object. When it's disposed (at the end of a 'using' block),
            // it will release the semaphore.
            return new Releaser(semaphore);
        }

        /// <summary>
        /// A private helper class that handles releasing the semaphore when disposed.
        /// </summary>
        private sealed class Releaser : IAsyncDisposable
        {
            private readonly SemaphoreSlim _semaphoreToRelease;

            internal Releaser(SemaphoreSlim semaphoreToRelease)
            {
                _semaphoreToRelease = semaphoreToRelease;
            }

            public ValueTask DisposeAsync()
            {
                _semaphoreToRelease.Release();
                return default;
            }
        }
    }
}
