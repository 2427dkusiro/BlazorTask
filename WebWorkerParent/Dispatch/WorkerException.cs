namespace BlazorTask.Dispatch;

/// <summary>
/// Represent a exception which occured in worker. This class is serializable. 
/// </summary>
public class WorkerException : Exception
{
    /// <summary>
    /// Create a new instance of <see cref="WorkerException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="stackTrace"></param>
    /// <param name="source"></param>
    /// <param name="baseExceptionName"></param>
    public WorkerException(string message, string? stackTrace, string? source, string baseExceptionName)
    {
        Message = message;
        StackTrace = stackTrace;
        Source = source;
        BaseExceptionName = baseExceptionName;
    }

    /// <inheritdoc />
    public override string Message { get; }

    /// <inheritdoc />
    public override string? StackTrace { get; }

    /// <inheritdoc />
    public override string? Source { get; set; }

    /// <summary>
    /// Get the name of inner exception.
    /// </summary>
    public string BaseExceptionName { get; }
}
