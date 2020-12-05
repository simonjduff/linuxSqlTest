using System;
using System.Collections.Generic;
using System.Threading;
using lib;

namespace runner
{
    class Program
    {
        private static string ConnectionString = Environment.GetEnvironmentVariable("SQLTEST_CONNECTIONSTRING");
        private const int ThreadCount = 2000;
        
        static void Main(string[] args)
        {
             Run();
        }

        public static void Run()
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var runner = new SqlTest(ConnectionString, cancellation.Token);
            List<SemaphoreSlim> semaphores = new List<SemaphoreSlim>(ThreadCount);

            for (int i = 0; i < ThreadCount; i++)
            {
                var semaphore = new SemaphoreSlim(1, 1);
                semaphores.Add(semaphore);
                semaphore.Wait(cancellation.Token);
                Thread thread = new Thread(new ParameterizedThreadStart(runner.Run));
                thread.Start(semaphore);
            }
        }
    }
}
