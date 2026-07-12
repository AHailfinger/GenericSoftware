using BlazorBootstrap;
using EnergyAutomate.Definitions;

namespace EnergyAutomate.Extentions
{
    public static class ApiServiceExtensions
    {
        #region Growatt

        public static void AddOrUpdate(this List<APiTraceValue> list, APiTraceValue value)
        {
            var existing = list.FirstOrDefault(x => x.Index == value.Index);
            if (existing != null)
            {
                existing.Key = value.Key;
                existing.Value = value.Value;
            }
            else
            {
                list.Add(value);
            }
        }

        #endregion Growatt

        #region Tibber

        /// <summary>Builds a query for home consumption.</summary>
        /// <param name="homeQueryBuilder"></param>
        /// <param name="resolution"></param>
        /// <param name="lastEntries">how many last entries to fetch</param>
        /// <returns></returns>
        public static HomeQueryBuilder WithConsumption(this HomeQueryBuilder homeQueryBuilder, EnergyResolution resolution, int lastEntries) =>
            homeQueryBuilder.WithConsumption(
                new HomeConsumptionConnectionQueryBuilder().WithNodes(new ConsumptionEntryQueryBuilder().WithAllFields()),
                resolution,
                last: lastEntries);

        /// <summary>Builds a query for home consumption.</summary>
        /// <param name="builder"></param>
        /// <param name="homeId"></param>
        /// <param name="resolution"></param>
        /// <param name="lastEntries">how many last entries to fetch</param>
        /// <returns></returns>
        public static TibberQueryBuilder WithHomeConsumption(this TibberQueryBuilder builder, Guid homeId, EnergyResolution resolution, int lastEntries) =>
            builder.WithAllScalarFields()
                .WithViewer(
                    new ViewerQueryBuilder()
                        .WithHome(
                            new HomeQueryBuilder().WithConsumption(resolution, lastEntries),
                            homeId
                        )
                );

        #endregion Tibber

        public static LineChartDataset SetDefaultStyle(this LineChartDataset lineChartDataset, string backgroundColor = "rgb(0, 0, 0)", string borderColor = "rgb(0, 0, 0)", double radius = 3, double borderWidth = 1)
        {
            lineChartDataset.PointRadius = [radius];
            lineChartDataset.PointHoverRadius = [radius];
            lineChartDataset.PointHitRadius = [radius];

            lineChartDataset.BorderColor = borderColor;
            lineChartDataset.HoverBorderColor = borderColor;
            lineChartDataset.PointBorderColor = [borderColor];
            lineChartDataset.PointHoverBorderColor = [borderColor];

            lineChartDataset.BackgroundColor = backgroundColor;
            lineChartDataset.HoverBackgroundColor = backgroundColor;
            lineChartDataset.PointBackgroundColor = [backgroundColor];
            lineChartDataset.PointHoverBackgroundColor = [backgroundColor];

            lineChartDataset.BorderWidth = borderWidth;
            lineChartDataset.HoverBorderWidth = borderWidth;
            lineChartDataset.PointBorderWidth = [borderWidth];
            lineChartDataset.PointHoverBorderWidth = [borderWidth];

            return lineChartDataset;
        }
    }
}
