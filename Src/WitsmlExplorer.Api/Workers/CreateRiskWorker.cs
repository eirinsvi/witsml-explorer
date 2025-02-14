using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Witsml;
using Witsml.Data;
using Witsml.Extensions;
using WitsmlExplorer.Api.Query;
using Witsml.ServiceReference;
using WitsmlExplorer.Api.Jobs;
using WitsmlExplorer.Api.Models;
using WitsmlExplorer.Api.Services;
using Witsml.Data.Measures;
using System.Globalization;

namespace WitsmlExplorer.Api.Workers
{

    public class CreateRiskWorker : BaseWorker<CreateRiskJob>, IWorker
    {
        private readonly IWitsmlClient witsmlClient;
        public JobType JobType => JobType.CreateRisk;

        public CreateRiskWorker(IWitsmlClientProvider witsmlClientProvider)
        {
            witsmlClient = witsmlClientProvider.GetClient();
        }

        public override async Task<(WorkerResult, RefreshAction)> Execute(CreateRiskJob job)
        {
            var risk = job.Risk;
            Verify(risk);

            var riskToCreate = SetupRiskToCreate(risk);

            var result = await witsmlClient.AddToStoreAsync(riskToCreate);
            if (result.IsSuccessful)
            {
                await WaitUntilRiskHasBeenCreated(risk);
                Log.Information("{JobType} - Job successful", GetType().Name);
                var workerResult = new WorkerResult(witsmlClient.GetServerHostname(), true, $"Risk created ({risk.Name} [{risk.Uid}])");
                var refreshAction = new RefreshWellbore(witsmlClient.GetServerHostname(), risk.WellUid, risk.Uid, RefreshType.Add);
                return (workerResult, refreshAction);
            }

            var description = new EntityDescription { WellboreName = risk.WellboreName };
            Log.Error($"Job failed. An error occurred when creating Risk: {job.Risk.PrintProperties()}");
            return (new WorkerResult(witsmlClient.GetServerHostname(), false, "Failed to create Risk", result.Reason, description), null);

        }
        private async Task WaitUntilRiskHasBeenCreated(Risk risk)
        {
            var isCreated = false;
            var query = RiskQueries.QueryById(risk.WellUid, risk.WellboreUid, risk.Uid);
            var maxRetries = 30;
            while (!isCreated)
            {
                if (--maxRetries == 0)
                {
                    throw new InvalidOperationException($"Not able to read newly created Risk with name {risk.Name} (id={risk.Uid})");
                }
                Thread.Sleep(1000);
                var riskResult = await witsmlClient.GetFromStoreAsync(query, new OptionsIn(ReturnElements.IdOnly));
                isCreated = riskResult.Risks.Any();
            }
        }

        private static WitsmlRisks SetupRiskToCreate(Risk risk)
        {
            return new WitsmlRisks
            {
                Risks = new WitsmlRisk
                {
                    Uid = risk.Uid,
                    WellboreUid = risk.WellboreUid,
                    WellUid = risk.WellUid,
                    Name = risk.Name,
                    WellboreName = risk.WellboreName,
                    WellName = risk.WellName,
                    Type = risk.Type,
                    Category = risk.Category,
                    SubCategory = risk.SubCategory,
                    ExtendCategory = risk.ExtendCategory,
                    AffectedPersonnel = risk.AffectedPersonnel?.Split(", "),
                    DTimStart = risk.DTimStart?.ToString("yyyy-MM-ddTHH:mm:ssK.fffZ"),
                    DTimEnd = risk.DTimEnd?.ToString("yyyy-MM-ddTHH:mm:ssK.fffZ"),
                    MdHoleStart = risk.MdHoleStart != null ? new WitsmlMeasuredDepthCoord { Uom = risk.MdHoleStart.Uom, Value = risk.MdHoleStart.Value.ToString(CultureInfo.InvariantCulture) } : null,
                    MdHoleEnd = risk.MdHoleEnd != null ? new WitsmlMeasuredDepthCoord { Uom = risk.MdHoleEnd.Uom, Value = risk.MdHoleEnd.Value.ToString(CultureInfo.InvariantCulture) } : null,
                    TvdHoleStart = risk.TvdHoleStart,
                    TvdHoleEnd = risk.TvdHoleEnd,
                    MdBitStart = risk.MdBitStart != null ? new WitsmlMeasuredDepthCoord { Uom = risk.MdBitStart.Uom, Value = risk.MdBitStart.Value.ToString(CultureInfo.InvariantCulture) } : null,
                    MdBitEnd = risk.MdBitEnd != null ? new WitsmlMeasuredDepthCoord { Uom = risk.MdBitEnd.Uom, Value = risk.MdBitEnd.Value.ToString(CultureInfo.InvariantCulture) } : null,
                    DiaHole = risk.DiaHole,
                    SeverityLevel = risk.SeverityLevel,
                    ProbabilityLevel = risk.ProbabilityLevel,
                    Summary = risk.Summary,
                    Details = risk.Details,
                    Identification = risk.Identification,
                    Contingency = risk.Contigency,
                    Mitigation = risk.Mitigation,
                    CommonData = new WitsmlCommonData
                    {
                        ItemState = risk.CommonData.ItemState,
                        SourceName = risk.CommonData.SourceName,
                        DTimCreation = risk.CommonData.DTimCreation?.ToString("yyyy-MM-ddTHH:mm:ssK.fffZ"),
                        DTimLastChange = risk.CommonData.DTimLastChange?.ToString("yyyy-MM-ddTHH:mm:ssK.fffZ"),
                    },
                }.AsSingletonList()
            };
        }

        private static void Verify(Risk risk)
        {
            if (string.IsNullOrEmpty(risk.Uid)) throw new InvalidOperationException($"{nameof(risk.Uid)} cannot be empty");
            if (string.IsNullOrEmpty(risk.Name)) throw new InvalidOperationException($"{nameof(risk.Name)} cannot be empty");
        }
    }
}
