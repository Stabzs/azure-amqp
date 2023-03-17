﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Microsoft.Azure.Amqp
{
    using System;
    using System.Threading.Tasks;
    using global::Microsoft.Azure.Amqp;
    using TestAmqpBroker;
    using Xunit;

    [CollectionDefinition(nameof(SequentialTests), DisableParallelization = true)]
    public class SequentialTests { }

    [Collection(nameof(SequentialTests))]
    [Trait("Category", TestCategory.Current)]
    public class AmqpConnectionTests
    {
        [Fact]
        public async Task AmqpsConnectionLoopTest()
        {
            const string address = "amqps://[::1]:15672";
            var broker = new TestAmqpBroker(new string[] { address }, null, "localhost", null);
            broker.Start();

            for (int i = 0; i < 500; ++i)
            {
                AmqpConnectionFactory factory = new AmqpConnectionFactory();
                factory.TlsSettings.CertificateValidationCallback = (a, b, c, d) => true;
                var connection = await factory.OpenConnectionAsync(new Uri(address), TimeSpan.FromSeconds(30));
                await connection.CloseAsync(TimeSpan.FromSeconds(30));
            }

            broker.Stop();
        }

        [Fact]
        public void AmqpConcurrentConnectionsTest()
        {
            const string address = "amqp://localhost:15672";
            var broker = new TestAmqpBroker(new string[] { address }, "guest:guest", null, null);
            broker.Start();

            Exception lastException = null;
            Action action = () =>
            {
                try
                {
                    AmqpConnection connection = AmqpUtils.CreateConnection(
                        new Uri(address),
                        null,
                        false,
                        null,
                        (int)AmqpConstants.DefaultMaxFrameSize);
                    connection.Open();
                    connection.Close();
                }
                catch (Exception exp)
                {
                    lastException = exp;
                }
            };

            Task[] tasks = new Task[32];
            for (int i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = Task.Run(action);
            }

            Task.WaitAll(tasks);

            broker.Stop();

            Assert.True(lastException == null, string.Format("Failed. Last exception {0}", lastException == null ? string.Empty : lastException.ToString()));
        }
    }
}
