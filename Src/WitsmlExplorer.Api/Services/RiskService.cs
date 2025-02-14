using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WitsmlExplorer.Api.Query;
using Witsml.ServiceReference;
using WitsmlExplorer.Api.Models;
using WitsmlExplorer.Api.Models.Measure;
using System.Globalization;

namespace WitsmlExplorer.Api.Services
{
    public interface IRiskService
    {
        Task<IEnumerable<Risk>> GetRisks(string wellUid, string wellboreUid);

    }

    // ReSharper disable once UnusedMember.Global
    public class RiskService : WitsmlService, IRiskService
    {
        public RiskService(IWitsmlClientProvider witsmlClientProvider) : base(witsmlClientProvider) { }

        public async Task<IEnumerable<Risk>> GetRisks(string wellUid, string wellboreUid)
        {
            var query = RiskQueries.GetWitsmlRiskByWellbore(wellUid, wellboreUid);
            var result = await WitsmlClient.GetFromStoreAsync(query, new OptionsIn(ReturnElements.All));


            return result.Risks.Select(risk =>

                new Risk
                {
                    Name = risk.Name,
                    WellboreName = risk.WellboreName,
                    WellboreUid = risk.WellboreUid,
                    WellName = risk.WellName,
                    WellUid = risk.WellUid,
                    Uid = risk.Uid,
                    Type = risk.Type,
                    Category = risk.Category,
                    SubCategory = risk.SubCategory,
                    ExtendCategory = risk.ExtendCategory,
                    AffectedPersonnel = (risk.AffectedPersonnel != null) ? string.Join(", ", risk.AffectedPersonnel) : "",
                    DTimStart = StringHelpers.ToDateTime(risk.DTimStart),
                    DTimEnd = StringHelpers.ToDateTime(risk.DTimEnd),
                    MdBitStart = (risk.MdBitStart == null) ? null : new LengthMeasure { Uom = risk.MdBitStart.Uom, Value = decimal.Parse(risk.MdBitStart.Value) },
                    MdBitEnd = (risk.MdBitEnd == null) ? null : new LengthMeasure { Uom = risk.MdBitEnd.Uom, Value = decimal.Parse(risk.MdBitEnd.Value) },
                    SeverityLevel = risk.SeverityLevel,
                    ProbabilityLevel = risk.ProbabilityLevel,
                    Summary = risk.Summary,

                    Details = risk.Details,
                    CommonData = new CommonData()
                    {
                        ItemState = risk.CommonData.ItemState,
                        SourceName = risk.CommonData.SourceName,
                        DTimLastChange = StringHelpers.ToDateTime(risk.CommonData.DTimLastChange),
                        DTimCreation = StringHelpers.ToDateTime(risk.CommonData.DTimCreation),
                    }
                }).OrderBy(risk => risk.Name);
        }
    }
}
