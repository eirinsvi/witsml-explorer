using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Witsml;
using WitsmlExplorer.Api.Jobs;
using WitsmlExplorer.Api.Models;
using WitsmlExplorer.Api.Query;
using WitsmlExplorer.Api.Services;

namespace WitsmlExplorer.Api.Workers
{
    public class BatchModifyWellWorker : BaseWorker<BatchModifyWellJob>, IWorker
    {
        private readonly IWitsmlClient witsmlClient;
        public JobType JobType => JobType.BatchModifyWell;

        public BatchModifyWellWorker(IWitsmlClientProvider witsmlClientProvider)
        {
            witsmlClient = witsmlClientProvider.GetClient();
        }

        public override async Task<(WorkerResult, RefreshAction)> Execute(BatchModifyWellJob job)
        {
            Verify(job.Wells);

            var wellsToUpdate = job.Wells.Select(WellQueries.UpdateWitsmlWell);
            var updateWellTasks = wellsToUpdate.Select(wellToUpdate => witsmlClient.UpdateInStoreAsync(wellToUpdate));

            Task resultTask = Task.WhenAll(updateWellTasks);
            await resultTask;

            if (resultTask.Status == TaskStatus.Faulted)
            {
                Log.Error("Job failed. An error occurred when batch updating wells");
                return (new WorkerResult(witsmlClient.GetServerHostname(), false, "Failed to batch update well properties"), null);
            }

            Log.Information("{JobType} - Job successful", GetType().Name);
            var workerResult = new WorkerResult(witsmlClient.GetServerHostname(), true, "Batch updated well properties");
            var wells = job.Wells.Select(well => well.Uid).ToArray();
            var refreshAction = new RefreshWells(witsmlClient.GetServerHostname(), wells, RefreshType.BatchUpdate);
            return (workerResult, refreshAction);
        }

        private static void Verify(IEnumerable<Well> wells)
        {
            if (!wells.Any()) throw new InvalidOperationException("payload cannot be empty");
        }
    }
}
