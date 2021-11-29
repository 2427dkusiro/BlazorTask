using System.Text.Json;

using Microsoft.JSInterop;

namespace WebWorkerParent
{
    internal static class WorkerBuilder
    {
        public async static Task<int> CreateNew(IJSObjectReference jSObjectReference, WorkerInitOption workerInitOption)
        {
            var json = JsonSerializer.Serialize(workerInitOption);

            var id = await jSObjectReference.InvokeAsync<int>("CreateWorker", json);
            return id;
        }
    }
}
