using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class Lookups
    {
        public static Dictionary<VantageStreetsWidth, double> CenterlineDistances => new Dictionary<VantageStreetsWidth, double> {
            {VantageStreetsWidth._60ft, Units.FeetToMeters(60.0)},
            {VantageStreetsWidth._75ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._80ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._100ft, Units.FeetToMeters(100.0)},
            {VantageStreetsWidth._140ft__Park_Avenue_, Units.FeetToMeters(140.0)}
        };
    }
}