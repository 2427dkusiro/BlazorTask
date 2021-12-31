namespace BlazorTask.Messaging;

internal enum CallResultTypes
{
    SuccessedVoid = 0,
    Allocated = 1,
    SuccessedJson = 2,
    FailedVoid = 3,
    Exception = 4,
}
