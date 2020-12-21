using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127DaylightEvaluation
{
    public class Settings
    {
        public enum MaterialPalette
        {
            Silhouette = 0,
            BuildingEdges = 1,
            GridlinesMajor = 10,
            GridlinesMinor = 11,
            GridlinesIrrelevant = 12,
            DaylightBoundaries = 13,
            ProfileCurves = 14,
            BlockedDaylight = 20,
            ProfileEncroachment = 21,
            UnblockedCredit = 22,
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
                MaterialPalette.GridlinesIrrelevant,
                new Material( "irrelevant gridline", Colors.Gray)
            },
            {
                MaterialPalette.DaylightBoundaries,
                new Material( "daylight boundary", Colors.Magenta)
            },
            {
                MaterialPalette.ProfileCurves,
                new Material( "profile curve", Colors.Red)
            },
            {
                MaterialPalette.BlockedDaylight,
                new Material( "blocked daylight", new Color(1,0,0,0.3))
            },
            {
                MaterialPalette.ProfileEncroachment,
                new Material( "profile encroachment", Colors.Red)
            },
            {
                MaterialPalette.UnblockedCredit,
                new Material( "unblocked credit", new Color(0,1,0,0.3))
            }
        };

        private static Color transparentColor = new Color(0, 0, 0, 0);

        public static Dictionary<MaterialPalette, SVG.Style> SvgStyles => new Dictionary<MaterialPalette, SVG.Style> {
            {MaterialPalette.Silhouette, new SVG.Style()},
            {MaterialPalette.BuildingEdges, new SVG.Style()},
            {MaterialPalette.GridlinesMajor, new SVG.Style(strokeWidth:0.1, enableFill: false, enableStroke: true, stroke: Colors.Black, fill: transparentColor)},
            {MaterialPalette.GridlinesMinor, new SVG.Style(strokeWidth:0.05, enableFill: false, enableStroke: true, stroke: Colors.Gray, fill: transparentColor)},
            {MaterialPalette.GridlinesIrrelevant, new SVG.Style()},
            {MaterialPalette.DaylightBoundaries, new SVG.Style()},
            {MaterialPalette.ProfileCurves, new SVG.Style()},
            {MaterialPalette.BlockedDaylight, new SVG.Style()},
            {MaterialPalette.ProfileEncroachment, new SVG.Style()},
            {MaterialPalette.UnblockedCredit, new SVG.Style()}
        };

        public static Dictionary<VantageStreetsWidth, double> CenterlineDistances => new Dictionary<VantageStreetsWidth, double> {
            {VantageStreetsWidth._60ft, Units.FeetToMeters(60.0)},
            {VantageStreetsWidth._75ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._80ft, Units.FeetToMeters(80.0)},
            {VantageStreetsWidth._100ft, Units.FeetToMeters(100.0)},
            {VantageStreetsWidth._140ft__Park_Avenue_, Units.FeetToMeters(140.0)}
        };


        /// <summary>
        /// First dict level is plan angle, second is section angle
        /// </summary>
        /// <value></value>
        public static Dictionary<(int, int), double> ProfileEncroachments = new Dictionary<(int, int), double>{
            { (1, 88), 8.5 }, { (2, 88), 8.0 }, { (3, 88), 7.5 }, { (4, 88), 7.0 }, { (5, 88), 6.5 }, { (6, 88), 6.0 }, { (7, 88), 5.5 }, { (8, 88), 5.0 },
            { (1, 86), 7.5 }, { (2, 86), 7.0 }, { (3, 86), 6.5 }, { (4, 86), 6.0 }, { (5, 86), 5.5 }, { (6, 86), 5.0 }, { (7, 86), 4.5 }, { (8, 86), 4.0 },
            { (1, 84), 6.5 }, { (2, 84), 6.0 }, { (3, 84), 5.5 }, { (4, 84), 5.0 }, { (5, 84), 4.5 }, { (6, 84), 4.0 }, { (7, 84), 3.5 },
            { (1, 82), 5.5 }, { (2, 82), 5.0 }, { (3, 82), 4.5 }, { (4, 82), 4.0 }, { (5, 82), 3.5 }, { (6, 82), 3.0 }, { (7, 82), 2.5 },
            { (1, 80), 4.5 }, { (2, 80), 4.0 }, { (3, 80), 3.5 }, { (4, 80), 3.0 }, { (5, 80), 2.5 }, { (6, 80), 2.0 }, { (7, 80), 1.5 },
            { (1, 78), 3.5 }, { (2, 78), 3.0 }, { (3, 78), 2.5 }, { (4, 78), 2.0 }, { (5, 78), 1.5 }, { (6, 78), 1.0 },
            { (1, 76), 2.5 }, { (2, 76), 2.0 }, { (3, 76), 1.5 }, { (4, 76), 1.0 }, { (5, 76), 0.5 },
            { (1, 74), 1.5 }, { (2, 74), 1.0 }, { (3, 74), 0.5 }, { (4, 74), 0.5 },
            { (1, 72), 0.5 }, { (2, 72), 0.5 }, { (3, 72), 0.5 }
        };

        public static int SectionCutoffLine = 70;

        public static double ChartHeight = 140.0;
    }
}