using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace lib
{
    public static class AsyncWrapper
    {
        public static void Wait(Func<Task> task, CancellationToken cancellationToken)
        {
            Wait(task(), cancellationToken);
        }
        
        public static void Wait(Task task, CancellationToken cancellationToken)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(1,1);
            semaphore.Wait(cancellationToken);
            try
            {
                task.ContinueWith(t =>
                {
                    semaphore.Release();
                }, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
            }
            
            semaphore.Wait(cancellationToken);
            
            if (task.IsFaulted)
            {
                Console.WriteLine($"Faulted {task.Exception.Message}");
                throw new Exception("Task faulted", task.Exception);
            }
            if (!task.IsCompleted)
            {
                throw new Exception($"Task not complete. Synchronization failed. State {task.Status.ToString()}");
            }
        }
    }
}