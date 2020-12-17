using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class Settings
    {
        public enum MaterialPalette
        {
            Silhouette = 0,
            BuildingEdges = 1,
            GridlinesMajor = 10,
            GridlinesMinor = 11,
            DaylightBoundaries = 12
        }
        public static Dictionary<MaterialPalette, Material> Materials => new Dictionary<MaterialPalette, Material> {
            {
                MaterialPalette.Silhouette,
                new Material(
                    "silhouette",
                    new Color(Colors.Darkgray.Red, Colors.Darkgray.Green, Colors.Darkgray.Blue, 0.5),
                    doubleSided:true
                )
            },
            {
                MaterialPalette.BuildingEdges,
                new Material("building edge", Colors.Black)
            },
            {
                MaterialPalette.GridlinesMajor,
                new Material("major gridline", Colors.Darkgray)
            },
            {
                MaterialPalette.GridlinesMinor,
                new Material( "minor gridline", Colors.Gray)
            },
            {
                MaterialPalette.DaylightBoundaries,
                new Material( "daylight boundary", Colors.Magenta)
            }
        };

        public static Dictionary<VantageStreetsWidth, double> CenterlineDistances => new Dictionary<VantageStreetsWidth, double> {
            {VantageStreetsWidth._60ft, Units.FeetToMeters(60.0)},
            {VantageStreetsWidth._75ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._80ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._100ft, Units.FeetToMeters(100.0)},
            {VantageStreetsWidth._140ft__Park_Avenue_, Units.FeetToMeters(140.0)}
        };
    }
}