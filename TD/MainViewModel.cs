using System;

using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace TD
{
    public class MainViewModel
    {
        public MainViewModel()
        {
            this.Model = new PlotModel { Title = "Example" };

            this.Model.LegendTitle = "Heat";
            this.Model.LegendOrientation = LegendOrientation.Horizontal;
            this.Model.LegendPlacement = LegendPlacement.Outside;
            this.Model.LegendPosition = LegendPosition.TopRight;
            this.Model.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            this.Model.LegendBorder = OxyColors.Black;

                var dateAxis = new LinearAxis() { MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, IntervalLength = 80 };
            this.Model.Axes.Add(dateAxis);
                var valueAxis = new LinearAxis() { MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, Title = "Value" };
            this.Model.Axes.Add(valueAxis);
        }

        public PlotModel Model { get; private set; }
    }
}
