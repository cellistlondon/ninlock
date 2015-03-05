using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Diagnostics;

namespace Ninlock
{
    public class DistributedLock
    {
        private readonly IDatabaseAsync _db;
        private readonly RedisKey _key;
        //private readonly RedisValue _value;
        public DistributedLock(IDatabaseAsync db, string key)//, string value=null)
        {
            _db = db;
            _key = key;
            //_value = value ?? Environment.MachineName;
        }
        public async Task EnterAsync(string token, TimeSpan? expiry=null, TimeSpan? retryTimeout=null)
        {
            Func<Task<bool>> task = () =>
            {
                try
                {
                    Debug.WriteLine("enter:" + token, "ninlock");
                    return _db.LockTakeAsync(_key, token, expiry ?? TimeSpan.MaxValue);
                }
                catch
                {
                    return Task.FromResult(false);
                }
            };
            await RetryUntilTrueAsync(task, retryTimeout);
        }
        public async Task ExitAsync(string token)
        {
            Debug.WriteLine("exit:"+token, "ninlock");
            await _db.LockReleaseAsync(_key, token);
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
