//using Tibber.Sdk;

//namespace EnergyAutomate
//{
//    public static class QueryBuilderExtensions
//    {
//        /// <summary>
//        /// Builds a query for home consumption.
//        /// </summary>
//        /// <param name="builder"></param>
//        /// <param name="homeId"></param>
//        /// <param name="resolution"></param>
//        /// <param name="lastEntries">how many last entries to fetch</param>
//        /// <returns></returns>
//        public static TibberQueryBuilder WithHomeConsumption(this TibberQueryBuilder builder, Guid homeId, EnergyResolution resolution, int lastEntries) =>
//            builder.WithAllScalarFields()
//                .WithViewer(
//                    new ViewerQueryBuilder()
//                        .WithHome(
//                            new HomeQueryBuilder().WithConsumption(resolution, lastEntries),
//                            homeId
//                        )
//                );

//        /// <summary>
//        /// Builds a query for home consumption.
//        /// </summary>
//        /// <param name="homeQueryBuilder"></param>
//        /// <param name="resolution"></param>
//        /// <param name="lastEntries">how many last entries to fetch</param>
//        /// <returns></returns>
//        public static HomeQueryBuilder WithConsumption(this HomeQueryBuilder homeQueryBuilder, EnergyResolution resolution, int lastEntries) =>
//            homeQueryBuilder.WithConsumption(
//                new HomeConsumptionConnectionQueryBuilder().WithNodes(new ConsumptionEntryQueryBuilder().WithAllFields()),
//                resolution,
//                last: lastEntries);
//    }
//}
