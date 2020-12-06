﻿using System;
using System.Threading;
using System.Threading.Tasks;
using lib;

namespace runner
{
    class Program
    {
        private static string ConnectionString = Environment.GetEnvironmentVariable("SQLTEST_CONNECTIONSTRING");
        private const int ThreadCount = 1000000;
        private static WaitHandle[] WaitHandles = new WaitHandle[ThreadCount];
        
        static void Main(string[] args)
        {
             Run();
        }

        public static void Run()
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5000));
            var runner = new SqlTest(ConnectionString, cancellation.Token);

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    Console.WriteLine($"Good {runner.Successes} Bad {runner.Failures} Error {runner.Exceptions}");
                }
            });

            for (int i = 0; i < ThreadCount; i++)
            {
                try
                {
                    WaitHandles[i] = new AutoResetEvent(false);
                    ThreadPool.QueueUserWorkItem(async s => await runner.Run(s), 
                        new State{ Id = i, 
                            AutoResetEvent = (AutoResetEvent)WaitHandles[i]});
                }
                catch (OperationCanceledException e)
                {
                    throw new Exception("Cancelled", e);
                }
            }

            try
            {
                Console.WriteLine("Waiting for end");
                foreach (var handle in WaitHandles)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancelled");
                        break;
                    }
                    handle.WaitOne();
                }
                Console.WriteLine("All threads terminated");
            }
            catch (OperationCanceledException e)
            {
            }
        }
    }
}
