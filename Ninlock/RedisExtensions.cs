using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using StackExchange.Redis;
using System.Diagnostics;

namespace Ninlock
{
    //based on: https://codingsolution.wordpress.com/2013/07/30/distributed-lock-with-appfabric-caching/
    public static class RedisExtensions
    {
        public static Task<IDisposable> AcquireLockAsync(this IDatabaseAsync db, string key, TimeSpan? expiry = null, TimeSpan? retryTimeout = null)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            return DataCacheLock.AcquireAsync(db, key, expiry, retryTimeout);
        }

        private class DataCacheLock : IDisposable
        {
            private StackExchange.Redis.IDatabaseAsync _db;
            public readonly RedisKey Key;
            public readonly RedisValue Value;
            public readonly TimeSpan? Expiry;
            private DataCacheLock(IDatabaseAsync db, string key, TimeSpan? expiry)
            {
                _db = db;
                Key = "ninlock:" + key;
                Value = Guid.NewGuid().ToString();
                Expiry = expiry;
            }
            public static async Task<IDisposable> AcquireAsync(IDatabaseAsync db, string key, TimeSpan? expiry, TimeSpan? retryTimeout)
            {
                DataCacheLock dataCacheLock = new DataCacheLock(db, key, expiry);
                Func<Task<bool>> task = () =>
                {
                    try
                    {
                        return db.LockTakeAsync(dataCacheLock.Key, dataCacheLock.Value, dataCacheLock.Expiry ?? TimeSpan.MaxValue);
                    }
                    catch
                    {
                        return Task.FromResult(false);
                    }
                };

                await RetryUntilTrueAsync(task, retryTimeout);
                return dataCacheLock;
            }
            public void Dispose()
            {
                Debug.WriteLine("release the lock:" + Value);
                _db.LockReleaseAsync(Key, Value).Wait();
            }
        }
        private static readonly Random _random = new Random();
        private static async Task<bool> RetryUntilTrueAsync(Func<Task<bool>> task, TimeSpan? retryTimeout)
        {
            int i = 0;
            DateTime utcNow = DateTime.UtcNow;
            while (!retryTimeout.HasValue || DateTime.UtcNow - utcNow < retryTimeout.Value)
            {
                i++;
                if (await task())
                {
                    return true;
                }
                var waitFor = _random.Next((int)Math.Pow(i, 2), (int)Math.Pow(i + 1, 2) + 1);
                await Task.Delay(waitFor);
            }
            throw new TimeoutException(string.Format("Exceeded timeout of {0}", retryTimeout.Value));
        }

    }
}

