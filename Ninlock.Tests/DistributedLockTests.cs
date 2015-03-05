using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using Ninlock;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ninlock.Tests
{
    [TestClass]
    public class DistributedLockTests
    {
        [TestMethod]
        public async Task DistributedLockTestAsync()
        {
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost");
            IDatabaseAsync db = redis.GetDatabase();
            var distributedLock = new DistributedLock(db, "DistLock");
            int counter = 0;
            Func<Task> t1 = async () =>
            {
                var token = Guid.NewGuid().ToString();
                await distributedLock.EnterAsync(token);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                Debug.WriteLine("tick1", "ninlock");
                counter = counter + 1;
                Debug.WriteLine("tock1", "ninlock");
                await distributedLock.ExitAsync(token);
            };
            Func<Task> t2 = async () =>
            {
                //await distributedLock.ExitAsync();
                var token = Guid.NewGuid().ToString();
                await distributedLock.EnterAsync(token, TimeSpan.FromSeconds(20));
                {
                    Debug.WriteLine("tick2", "ninlock");
                    counter = counter + 5;
                    Debug.WriteLine("tock2", "ninlock");
                }
                await distributedLock.ExitAsync(token);
            };
            await Task.WhenAll(t1(), t2());
            Assert.AreEqual(6, counter);
        }
    }
}
