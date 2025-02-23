using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechPlayground.Helpers
{
    public class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> waiters = new Queue<TaskCompletionSource<bool>>();
        private bool isSignaled;

        public AsyncAutoResetEvent(bool initialState)
        {
            isSignaled = initialState;
        }

        public Task WaitAsync()
        {
            lock (waiters)
            {
                if (isSignaled)
                {
                    isSignaled = false;
                    return Task.CompletedTask;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    waiters.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (waiters)
            {
                if (waiters.Count > 0)
                    toRelease = waiters.Dequeue();
                else if (!isSignaled)
                    isSignaled = true;
            }
            toRelease?.SetResult(true);
        }

        public void Reset()
        {
            lock (waiters)
            {
                isSignaled = false;
                while (waiters.Count > 0)
                {
                    var tcs = waiters.Dequeue();
                    tcs.SetCanceled();
                }
            }

        }
    }
}
