namespace BlazorTask.Dispatch;

public class WorkerException : Exception
{
    public WorkerException(string message, string? stackTrace, string? source, string baseExceptionName)
    {
        Message = message;
        StackTrace = stackTrace;
        Source = source;
        BaseExceptionName = baseExceptionName;
    }

    public override string Message { get; }

    public override string? StackTrace { get; }

    public override string? Source { get; set; }

    public string BaseExceptionName { get; }
}
