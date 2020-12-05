using System;
using System.Threading;
using System.Threading.Tasks;

namespace lib
{
    public static class AsyncWrapper
    {
        public static void Wait(Task task, CancellationToken cancellationToken)
        {
            SemaphoreSlim Semaphore = new SemaphoreSlim(1,1);
            Semaphore.Wait(cancellationToken);
            try
            {
                task.ContinueWith(t => Semaphore.Release(), cancellationToken);
            }
            catch (OperationCanceledException e)
            {
            }
            if (task.IsCompleted)
            {
                throw new Exception("Task not complete. Synchonization failed.");
            }
            else if (task.IsFaulted)
            {
                throw new Exception("Task faulted", task.Exception);
            }
            
            Semaphore.Wait(cancellationToken);
        }
    }
}