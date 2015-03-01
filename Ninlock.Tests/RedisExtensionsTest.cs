using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using StackExchange.Redis;
using Ninlock;
using Moq;
using System.Diagnostics;

namespace Ninlock.Tests
{
    [TestClass]
    public class RedisExtensionsTest
    {
        [TestMethod]
        public async Task BasicLockTestAsync()
        {
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost");
            IDatabaseAsync db = redis.GetDatabase();
            int counter = 0;
            Func<Task> t1 = async () =>
            {
                using (await db.AcquireLockAsync("Lock2",TimeSpan.FromSeconds(2)))
                {
                    //await Task.Delay(TimeSpan.FromSeconds(0.1));
                    Debug.WriteLine("tick1");
                    counter = counter+1;
                    Debug.WriteLine("tock1");
                }
            };
            Func<Task> t2 = async () =>
            {
                using (await db.AcquireLockAsync("Lock2",TimeSpan.FromSeconds(2)))
                {
                    Debug.WriteLine("tick2");
                    counter = counter + 5;
                    Debug.WriteLine("tock2");
                }
            };
            await Task.WhenAll(t1(), t2());
            Assert.AreEqual(6, counter);
        }

    }
}
