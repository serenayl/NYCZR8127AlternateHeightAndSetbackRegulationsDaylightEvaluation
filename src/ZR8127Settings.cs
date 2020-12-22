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
            DaylightBoundaries = 12,
            ProfileCurves = 13,
            BlockedDaylight = 20,
            ProfileEncroachment = 21,
            UnblockedCredit = 22,
        }

        public static Color TransparentColor = new Color(0, 0, 0, 0);

        public static Dictionary<MaterialPalette, SVG.Style> SvgStyles => new Dictionary<MaterialPalette, SVG.Style> {
            {MaterialPalette.Silhouette, new SVG.Style(strokeWidth:1, enableFill: true, enableStroke: true, stroke: Colors.Black, fill: new Color(0, 0, 0, 0.2))},
            {MaterialPalette.BuildingEdges, new SVG.Style(strokeWidth:0.2, enableFill: false, enableStroke: true, stroke: Colors.Black, fill: TransparentColor)},
            {MaterialPalette.GridlinesMajor, new SVG.Style(strokeWidth:0.1, enableFill: false, enableStroke: true, stroke: Colors.Black, fill: TransparentColor)},
            {MaterialPalette.GridlinesMinor, new SVG.Style(strokeWidth:0.05, enableFill: false, enableStroke: true, stroke: Colors.Gray, fill: TransparentColor)},
            {MaterialPalette.DaylightBoundaries, new SVG.Style(strokeWidth:0.5, enableFill: false, enableStroke: true, stroke: Colors.Magenta, fill: TransparentColor)},
            {MaterialPalette.ProfileCurves, new SVG.Style(strokeWidth:0.5, enableFill: false, enableStroke: true, stroke: Colors.Red, fill: TransparentColor)},
            {MaterialPalette.BlockedDaylight, new SVG.Style(strokeWidth:0, enableFill: true, enableStroke: false, stroke: TransparentColor, fill: new Color(1, 0.75, 0, 0.75))},
            {MaterialPalette.ProfileEncroachment, new SVG.Style(strokeWidth:0, enableFill: true, enableStroke: false, stroke: TransparentColor, fill: new Color(1, 0, 0, 0.75))},
            {MaterialPalette.UnblockedCredit, new SVG.Style(strokeWidth:0, enableFill: true, enableStroke: false, stroke: TransparentColor, fill: new Color(0, 1, 0, 0.75))}
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