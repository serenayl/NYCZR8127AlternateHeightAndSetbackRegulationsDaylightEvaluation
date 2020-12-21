using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
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

        public Square(PlanGrid planGrid, SectionGrid sectionGrid)
        {
            this.Id = (planGrid.Id, sectionGrid.Id);
            this.PlanGrid = planGrid;
            this.SectionGrid = sectionGrid;

            this.Polygon = Polygon.Rectangle(new Vector3(this.PlanGrid.Grid.Domain.Min, this.SectionGrid.Grid.Domain.Min), new Vector3(this.PlanGrid.Grid.Domain.Max, this.SectionGrid.Grid.Domain.Max));

            var isParentSquare = this.PlanGrid.Id % 1.0 == 0.0;

            var encroachmentKey = ((int)Math.Floor(this.PlanGrid.Id), (int)Math.Floor(this.SectionGrid.Id));

            if (isParentSquare && Settings.ProfileEncroachments.ContainsKey(encroachmentKey))
            {
                this.PotentialProfilePenalty = -1 * Settings.ProfileEncroachments[encroachmentKey];
            }

            if (
                this.PlanGrid.Grid.Domain.Max <= Settings.SectionCutoffLine &&
                this.PlanGrid.Multiplier == (isParentSquare ? 1.0 : 0.2) // code is unclear on this, but to be safe we do not count unblocked squares for credit that are cut off by daylight boundaries
            )
            {
                // This won't apply if street continuity is desired and we are not in east midtown subdistrict, but that will be handled by not looping over these in that case
                this.PotentialScore = isParentSquare ? 0.3 : 0.03;
            }

            if (this.SectionGrid.Id >= Settings.SectionCutoffLine)
            {
                this.PotentialScore = -1 * (isParentSquare ? 1.0 : 0.1);
            }
        }

        public Square(Square parentSquare, PlanGrid planGrid, SectionGrid sectionGrid) : this(planGrid, sectionGrid)
        {
            if (parentSquare.PotentialProfilePenalty != 0)
            {
                this.PotentialProfilePenalty = parentSquare.PotentialProfilePenalty / 10;
            }
        }
    }
}