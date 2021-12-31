using System.Runtime.CompilerServices;

namespace BlazorTask.Tasks
{
    public abstract class WorkerTask
    {
        protected abstract void BeginAsyncInvoke(WorkerAwaiter workerAwaiter);

        protected abstract void BlockingInvoke();

        public WorkerAwaiter GetAwaiter()
        {
            var awaiter = new WorkerAwaiter();
            BeginAsyncInvoke(awaiter);
            return awaiter;
        }

        public void Wait()
        {
            BlockingInvoke();
        }
    }

    public abstract class WorkerTask<T>
    {
        protected abstract void BeginAsyncInvoke(WorkerAwaiter<T> workerAwaiter);

        protected abstract T BlockingInvoke();

        public WorkerAwaiter<T> GetAwaiter()
        {
            var awaiter = new WorkerAwaiter<T>();
            BeginAsyncInvoke(awaiter);
            return awaiter;
        }

        private T? syncResult;

        public void Wait()
        {
            syncResult = BlockingInvoke();
        }
    }

    public class WorkerAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; set; }

        private Action? action;
        private Exception? exception;

        public void OnCompleted(Action action)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException();
            }
            this.action = action;
        }

        public void GetResult()
        {
            if (exception is not null)
            {
                throw exception;
            }

            if (IsCompleted)
            {
                return;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void SetResult()
        {
            IsCompleted = true;
            action?.Invoke();
        }

        public void SetException(Exception exception)
        {
            IsCompleted = true;
            this.exception = exception;
            action?.Invoke();
        }
    }

    public class WorkerAwaiter<T> : INotifyCompletion
    {
        public bool IsCompleted { get; set; }

        private Action? action;
        private Exception? exception;
        private T result;

        public void OnCompleted(Action action)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException();
            }
            this.action = action;
        }

        public T GetResult()
        {
            if (exception is not null)
            {
                throw exception;
            }

            if (IsCompleted)
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void SetResult(T result)
        {
            IsCompleted = true;
            this.result = result;
            action?.Invoke();
        }

        public void SetException(Exception exception)
        {
            IsCompleted = true;
            this.exception = exception;
            action?.Invoke();
        }
    }
}
