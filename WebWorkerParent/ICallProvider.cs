using BlazorTask.Tasks;

using System.Reflection;

namespace BlazorTask;

public interface ICallProvider
{
    WorkerTask Call(MethodInfo methodInfo, params object?[] args);
    WorkerTask<T> Call<T>(MethodInfo methodInfo, params object?[] args);
}