using Silk.NET.OpenGL;
using System.Diagnostics;

namespace EditorToolkit.OpenGL
{
    public class GLTaskScheduler
    {
        private List<(TaskCompletionSource promise, Action<GL> task)> mPending = [];

        public async Task<TResult> Schedule<TResult>(Func<GL, TResult> task)
        {
            TResult result = default!;

            await Schedule(gl =>
            {
                result = task(gl);
            });
            return result;
        }

        public Task Schedule(Action<GL> task)
        {
            var promise = new TaskCompletionSource();

            lock (mPending)
            {
                mPending.Add((promise, task));
            }

            return promise.Task;
        }

        /// <summary>
        /// Executes all scheduled tasks and marks their tasks as completed
        /// <para>After finishing a task it only continues with the next if <paramref name="maxContinueTime"/> is not exceeded</para>
        /// <param name="gl"></param>
        /// <param name="maxContinueTime"></param>
        public void ExecutePending(GL gl, TimeSpan maxContinueTime)
        {
            var startTime = Stopwatch.GetTimestamp();
            int count;
            lock (mPending)
                count = mPending.Count;

            if (count == 0)
                return;

            int i = 0;
            while (i < count)
            {
                (TaskCompletionSource promise, Action<GL> task) = mPending[i++];
                try
                {
                    task.Invoke(gl);
                    promise.SetResult();
                }
                catch (Exception ex)
                {
                    promise.SetException(ex);
                }

                lock (mPending)
                    count = mPending.Count;

                if (Stopwatch.GetElapsedTime(startTime) > maxContinueTime)
                    break;
            }

            Debug.WriteLine($"Completed {i} gl tasks");

            lock (mPending)
            {
                mPending.RemoveRange(0, i);
            }
        }
    }
}
