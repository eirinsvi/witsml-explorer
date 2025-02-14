using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Witsml;
using Witsml.Extensions;
using Witsml.ServiceReference;
using WitsmlExplorer.Api.Jobs;
using WitsmlExplorer.Api.Jobs.Common;
using WitsmlExplorer.Api.Models;
using WitsmlExplorer.Api.Query;
using WitsmlExplorer.Api.Services;

namespace WitsmlExplorer.Api.Workers
{
    public class ModifyTrajectoryStationWorker : BaseWorker<ModifyTrajectoryStationJob>, IWorker
    {
        private readonly IWitsmlClient witsmlClient;
        public JobType JobType => JobType.ModifyTrajectoryStation;

        public ModifyTrajectoryStationWorker(IWitsmlClientProvider witsmlClientProvider)
        {
            witsmlClient = witsmlClientProvider.GetClient();
        }

        public override async Task<(WorkerResult, RefreshAction)> Execute(ModifyTrajectoryStationJob job)
        {
            Verify(job.TrajectoryStation, job.TrajectoryReference);

            var wellUid = job.TrajectoryReference.WellUid;
            var wellboreUid = job.TrajectoryReference.WellboreUid;
            var trajectoryUid = job.TrajectoryReference.TrajectoryUid;

            var query = TrajectoryQueries.UpdateTrajectoryStation(job.TrajectoryStation, job.TrajectoryReference);
            var result = await witsmlClient.UpdateInStoreAsync(query);
            if (result.IsSuccessful)
            {
                Log.Information("{JobType} - Job successful", GetType().Name);
                var refreshAction = new RefreshTrajectory(witsmlClient.GetServerHostname(), wellUid, wellboreUid, trajectoryUid, RefreshType.Update);
                return (new WorkerResult(witsmlClient.GetServerHostname(), true, $"TrajectoryStation updated ({job.TrajectoryStation.Uid})"), refreshAction);
            }

            Log.Error("Job failed. An error occurred when modifying TrajectoryStation object: {TrajectoryStation}", job.TrajectoryStation.PrintProperties());
            var TrajectoryStationQuery = TrajectoryQueries.GetWitsmlTrajectoryById(wellUid, wellboreUid, trajectoryUid);
            var TrajectoryStations = await witsmlClient.GetFromStoreAsync(TrajectoryStationQuery, new OptionsIn(ReturnElements.IdOnly));
            var trajectory = TrajectoryStations.Trajectories.FirstOrDefault();
            EntityDescription description = null;
            if (trajectory != null)
            {
                description = new EntityDescription
                {
                    WellName = trajectory.NameWell,
                    WellboreName = trajectory.NameWellbore,
                    ObjectName = job.TrajectoryStation.Uid
                };
            }

            return (new WorkerResult(witsmlClient.GetServerHostname(), false, "Failed to update TrajectoryStation", result.Reason, description), null);
        }

        private static void Verify(TrajectoryStation trajectoryStation, TrajectoryReference trajectoryReference)
        {
            if (string.IsNullOrEmpty(trajectoryReference.WellUid)) throw new InvalidOperationException($"{nameof(trajectoryReference.WellUid)} cannot be empty");
            if (string.IsNullOrEmpty(trajectoryReference.WellboreUid)) throw new InvalidOperationException($"{nameof(trajectoryReference.WellboreUid)} cannot be empty");
            if (string.IsNullOrEmpty(trajectoryReference.TrajectoryUid)) throw new InvalidOperationException($"{nameof(trajectoryReference.TrajectoryUid)} cannot be empty");

            if (string.IsNullOrEmpty(trajectoryStation.Uid)) throw new InvalidOperationException($"{nameof(TrajectoryStation.Uid)} cannot be empty");
        }
    }
}
