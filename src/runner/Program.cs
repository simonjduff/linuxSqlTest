using System;
using System.Threading;
using lib;

namespace runner
{
    class Program
    {
        private static string ConnectionString = Environment.GetEnvironmentVariable("SQLTEST_CONNECTIONSTRING");
        private const int ThreadCount = 50;
        
        static void Main(string[] args)
        {
             Run();
        }

        public static void Run()
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var runner = new SqlTest(ConnectionString, cancellation.Token);
            var semaphore = new SemaphoreSlim(ThreadCount, ThreadCount);

            for (int i = 0; i < ThreadCount; i++)
            {
                try
                {
                    semaphore.Wait(cancellation.Token);
                    Thread thread = new Thread(new ParameterizedThreadStart(runner.Run));
                    thread.Start(semaphore);
                }
                catch (OperationCanceledException e)
                {
                    throw new Exception("Cancelled", e);
                }
            }

            try
            {
                for (int i = 0; i < ThreadCount; i++)
                {
                    semaphore.Wait(cancellation.Token);
                }
            }
            catch (OperationCanceledException e)
            {
            }
        }
    }
}
