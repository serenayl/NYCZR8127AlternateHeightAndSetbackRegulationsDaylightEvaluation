using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class PlanOrSectionGrid
    {
        public Grid1d Grid;
        public double Id;

        protected PlanOrSectionGrid(double id, Grid1d grid)
        {
            this.Id = id;
            this.Grid = grid;
        }
    }

    public class PlanGrid : PlanOrSectionGrid
    {
        public double Multiplier = 0.0;
        public List<PlanGrid> Children = new List<PlanGrid>();

        public PlanGrid(double id, Grid1d grid) : base(id, grid)
        {

        }

        public PlanGrid(double id, Grid1d grid, double multiplier) : base(id, grid)
        {
            this.Multiplier = multiplier;
        }
    }

    public class SectionGrid : PlanOrSectionGrid
    {
        public SectionGrid(double id, Grid1d grid) : base(id, grid)
        {

        }
    }


    public class Square
    {
        // ID is plan id, sectionid.
        // Plan id is the id number as referenced in the code: 1st cell is furthest from 90 degree line.
        // A full chart will have two of each plan ID, 1-10, and then 10-1 on the other side of the 90 degree line.
        // Section id is the lower section angle bounds of the square
        public (double, double) Id;

        public PlanGrid PlanGrid;
        public SectionGrid SectionGrid;

        public double PotentialScore = 0.0;
        public double PotentialProfilePenalty = 0.0;

        public Polygon Polygon;

        public List<Square> SubSquares = new List<Square>();

        public Square(PlanGrid planGrid, SectionGrid sectionGrid, VantagePoint vp)
        {
            this.Id = (planGrid.Id, sectionGrid.Id);
            this.PlanGrid = planGrid;
            this.SectionGrid = sectionGrid;

            this.Polygon = Polygon.Rectangle(new Vector3(this.PlanGrid.Grid.Domain.Min, this.SectionGrid.Grid.Domain.Min), new Vector3(this.PlanGrid.Grid.Domain.Max, this.SectionGrid.Grid.Domain.Max));

            var isSubsquare = this.PlanGrid.Id % 1.0 != 0.0;

            var encroachmentKey = ((int)Math.Floor(this.PlanGrid.Id), (int)Math.Floor(this.SectionGrid.Id));

            if (Settings.ProfileEncroachments.ContainsKey(encroachmentKey))
            {
                this.PotentialProfilePenalty = -1 * (!isSubsquare ? Settings.ProfileEncroachments[encroachmentKey] : Settings.ProfileEncroachments[encroachmentKey] / 10.0);
            }

            if (
                !vp.VantageStreet.StreetWallContinuity &&
                this.PlanGrid.Grid.Domain.Max <= Settings.SectionCutoffLine &&
                this.PlanGrid.Multiplier == (isSubsquare ? 0.2 : 1.0) // code is unclear on this, but to be safe we do not count unblocked squares for credit that are cut off by daylight boundaries
            )
            {
                this.PotentialScore = isSubsquare ? 0.03 : 0.3;
            }

            if (this.SectionGrid.Id >= Settings.SectionCutoffLine)
            {
                this.PotentialScore = -1 * (isSubsquare ? 0.1 : 1.0);
            }
        }
    }
}