using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections;
using System.Data;

using System.IO;

using EyeTrackingCore;

using MathNet.Numerics.LinearRegression;
using MathNet.Numerics.Statistics;

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for FeatureExtractionWindow.xaml
    /// </summary>
    public partial class FeatureExtractionWindow : Window
    {
        private ObservableCollection<FixationFeatureExtractor> list = new ObservableCollection<FixationFeatureExtractor>();
        private List<Fixation> currentlyLoadedFixations;
        private List<Saccade> currentlyLoadedSaccades;

        private MainWindow parent;

        public FeatureExtractionWindow(List<Fixation> fixations, List<Saccade> saccades, MainWindow parent)
        {
            InitializeComponent();
            InitFeatureList();

            this.parent = parent;

            this.currentlyLoadedFixations = fixations;
            this.currentlyLoadedSaccades = saccades;
            this.Closing += FeatureExtractionWindow_Closing;
        }

        private void FeatureExtractionWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<FeatureExtractor> extractors = GetFeatures();
            parent.SetExtractors(extractors);

               
        }

        public enum RequiredData
        {
            Fixation,
            Saccade
        }

        public abstract class FeatureExtractor
        {
            public bool include { get; set; }
            public string featureName { get; set; }
            public abstract RequiredData DataType();
        }

        public class FixationFeatureExtractor : FeatureExtractor
        {
            public Func<List<Fixation>, double> action;

            override public RequiredData DataType()
            {
                return RequiredData.Fixation;
            }
        }

        public class SaccadeFeatureExtractor : FeatureExtractor
        {
            public Func<List<Saccade>, double> action;

            override public RequiredData DataType()
            {
                return RequiredData.Saccade;
            }
        }

        private void InitFeatureList()
        {
            List<FeatureExtractor> items = new List<FeatureExtractor>();

            // Fixation related features.
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (mean)", include = true, action = FixationDurationMean });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (variance)", include = true, action = FixationDurationVariance });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (standard deviation)", include = true, action = FixationDurationStandardDeviation });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Rate (per second)", include = true, action = FixationRatePerSecond });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Slope", include = true, action = FixationSlope });

            items.Add(new FixationFeatureExtractor() { featureName = "Number of VSEditor Fixations", include = true, action = NumberOfEditorFixations });
            items.Add(new FixationFeatureExtractor() { featureName = "Number of VSExplorer Fixations", include = true, action = NumberOfSolutionExplorerFixations });
            items.Add(new FixationFeatureExtractor() { featureName = "Number of VSOutput Fixations", include = true, action = NumberOfOutputFixations });

            items.Add(new FixationFeatureExtractor() { featureName = "NumberOfBriefFixations", include = true, action = NumberOfBriefFixations });
            items.Add(new FixationFeatureExtractor() { featureName = "NumberOfHoldFixations", include = true, action = NumberOfHoldFixations });


            // Can't use the ratio as the denominator can be zero and therefore the ratio would be undefined.
            // items.Add(new FixationFeatureExtractor() { featureName = "BriefToHoldRatio", include = true, action = BriefToHoldRatio });

            items.Add(new FixationFeatureExtractor() { featureName = "DistractionUp", include = true, action = DistractionUp });
            items.Add(new FixationFeatureExtractor() { featureName = "DistractionRight", include = true, action = DistractionRight });
            items.Add(new FixationFeatureExtractor() { featureName = "DistractionDown", include = true, action = DistractionDown });
            items.Add(new FixationFeatureExtractor() { featureName = "DistractionLeft", include = true, action = DistractionLeft });

            items.Add(new FixationFeatureExtractor() { featureName = "Area Containing Fixations (75%)", include = true, action = AreaContainingFixations75 });
            items.Add(new FixationFeatureExtractor() { featureName = "Area Containing Fixations (50%)", include = true, action = AreaContainingFixations50 });
            items.Add(new FixationFeatureExtractor() { featureName = "Area Containing Fixations (25%)", include = true, action = AreaContainingFixations25 });

            // Saccade related features.
            items.Add(new SaccadeFeatureExtractor() { featureName = "Saccade Size (mean)", include = true, action = SaccadeSizeMean });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Saccade Size (variance)", include = true, action = SaccadeSizeVariance });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Saccade Size (standard deviation)", include = true, action = SaccadeSizeStandardDeviation });

            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Short Saccades", include = false, action = NumberOfShortSaccades });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Medium Saccades", include = false, action = NumberOfMediumSaccades });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Long Saccades", include = false, action = NumberOfLongSaccades });

            // Saccade direction counts. (36 features, one for each 10 degree bucket)
            // TODO: figure out how we can write out 36 features whilst passing one function
            // items.Add(new SaccadeFeatureExtractor() { featureName = "Saccade Direction Counts", include = true, action = SaccadeDirectionCounts });

            // For each successive pair of saccades, how many had an opposite direction, how many had a neighbouring direction.
            items.Add(new SaccadeFeatureExtractor() { featureName = "Follow Direction Count", include = true, action = FollowDirectionCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Neighbouring Direction Count", include = true, action = NeighbouringDirectionCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Opposite Direction Count", include = true, action = OppositeDirectionCount });

            // For the 4 sectors, how many of each sector do we have?
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Right Count", include = false, action = Sector4RightCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Up Count", include = false, action = Sector4UpCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Left Count", include = false, action = Sector4LeftCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Down Count", include = false, action = Sector4DownCount });

            // For the 8 sectors, how many of each sector do we have?
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 Right Count", include = true, action = Sector8RightCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 Up Count", include = true, action = Sector8UpCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 Left Count", include = true, action = Sector8LeftCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 Down Count", include = true, action = Sector8DownCount });

            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 UpRight Count", include = true, action = Sector8UpRightCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 UpLeft", include = true, action = Sector8UpLeftCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 DownLeft", include = true, action = Sector8DownLeftCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector8 DownRight", include = true, action = Sector8DownRightCount });

            // Wordbook / Pattern related features.

            // "Saccade direction" based features.
            // inversity
            // category inversity
            // neighbouring direction

            // Wordbook related features.

            // Blink related features.

            // Atoms
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Strings", include = true, action = NumberOfStrings });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Lines", include = true, action = NumberOfLines });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Comparisons", include = true, action = NumberOfComparisons });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Scans", include = true, action = NumberOfScans });

            featureList.ItemsSource = items;
        }

        private List<FeatureExtractor> GetFeatures()
        {
            var itemsSource = featureList.ItemsSource;
            List<FeatureExtractor> featuresToExtract = new List<FeatureExtractor>();

            foreach (FeatureExtractor item in itemsSource)
            {
                Console.Write(item.featureName + ": ");
                Console.WriteLine(item.include);

                if(item.include)
                {
                    featuresToExtract.Add(item);
                }
            }

            return featuresToExtract;
        }
        
        // Features extracted from fixations
        // #################################
        private double FixationDurationMean(List<Fixation> fixations)
        {
            double sum = 0;
            int fixationCount = 1;

            foreach (var fixation in fixations)
            {
                fixationCount++;
                sum += fixation.endTime - fixation.startTime;
            }

            return sum / fixationCount;
        }

        private double FixationDurationVariance(List<Fixation> fixations)
        {
            List<Double> durations = new List<Double>();

            foreach (var fixation in fixations)
            {
                var duration = fixation.endTime - fixation.startTime;
                durations.Add(duration);
            }

            return Statistics.Variance(durations);
        }

        private double FixationDurationStandardDeviation(List<Fixation> fixations)
        {
            return Math.Sqrt(FixationDurationVariance(fixations));
        }

        private double FixationRatePerSecond(List<Fixation> fixations)
        {
            int numberOfFixations = fixations.Count;

            int startTime = fixations[0].startTime;
            int endTime = fixations[numberOfFixations - 1].endTime;

            double totalTimeInSeconds = (endTime - startTime) / 1000;

            return numberOfFixations / totalTimeInSeconds;
        }

        private double FixationSlope(List<Fixation> fixations)
        {
            List<Tuple<double, double>> samples = new List<Tuple<double, double>>();

            foreach (var fixation in fixations)
            {
                Tuple<double, double> sample = new Tuple<double, double>(fixation.x, fixation.y);
                samples.Add(sample);
            }

            if(samples.Count >= 2)
            {
                Tuple<double, double> result = SimpleRegression.Fit(samples);
                return result.Item2;
            }
            else
            {
                return 0;
            }
        }

        private double NumberOfBriefFixations(List<Fixation> fixations)
        {
            Wordbook fixationBook = new Wordbook(fixations);

            double numberOfBriefs = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Brief ? acc + 1 : acc);

            return numberOfBriefs;
        }

        private double NumberOfHoldFixations(List<Fixation> fixations)
        {
            Wordbook fixationBook = new Wordbook(fixations);

            double numberOfHolds = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Hold ? acc + 1 : acc);

            return (numberOfHolds);
        }

        private double BriefToHoldRatio(List<Fixation> fixations)
        {
            Wordbook fixationBook = new Wordbook(fixations);

            double numberOfBriefs = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Brief ? acc + 1 : acc);
            double numberOfHolds = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Hold ? acc + 1 : acc);

            return (numberOfBriefs / numberOfHolds);
        }

        double screenWidth = 1920;
        double screenHeight = 1080;
        double buffer = 50;

        private double DistractionUp(List<Fixation> fixations)
        {
            return fixations.Aggregate(0, (acc, fixation) => fixation.y < -buffer ? acc + 1 : acc);
        }

        private double DistractionRight(List<Fixation> fixations)
        {
            return fixations.Aggregate(0, (acc, fixation) => (fixation.x > screenWidth + buffer && 0 < fixation.y && fixation.y < screenHeight) ? acc + 1 : acc);
        }

        private double DistractionLeft(List<Fixation> fixations)
        {
            return fixations.Aggregate(0, (acc, fixation) => (fixation.x < -buffer && 0 < fixation.y && fixation.y < screenHeight) ? acc + 1 : acc);
        }

        private double DistractionDown(List<Fixation> fixations)
        {
            return fixations.Aggregate(0, (acc, fixation) => fixation.y > screenHeight + buffer ? acc + 1 : acc);
        }

        private double AreaContainingFixations(List<Fixation> fixations, double percentToDrop)
        {
            // find centre of all fixations
            System.Windows.Point centre = CalculateMean(fixations);

            // get lengths from centre to each fixation and sort them
            var distances = DistancesFromPointToOtherPoints(centre, fixations);
            distances.Sort((x,y) => x.Item2.CompareTo(y.Item2));

            // drop the fixations that were in the top 10% of
            int numToDrop = (int)(distances.Count * percentToDrop);
            var dropped = distances.GetRange(0, distances.Count - numToDrop);

            // for each remaining fixation,
            // if the fixations falls within the rectangle do nothing, else
            // extend the rectangle to encompass the fixations
            // return the area of the rectangle
            var areaRect = new AreaRect(fixations[dropped[0].Item1].x, fixations[dropped[0].Item1].y);
            for (int i = 1; i < dropped.Count; i++)
            {
                areaRect.IncreaseRectSizeToContainPoint(fixations[dropped[i].Item1].x, fixations[dropped[i].Item1].y);
            }

            return areaRect.CalculateArea();
        }

        private double AreaContainingFixations75(List<Fixation> fixations)
        {
            return AreaContainingFixations(fixations, 0.25);
        }

        private double AreaContainingFixations50(List<Fixation> fixations)
        {
            return AreaContainingFixations(fixations, 0.5);
        }

        private double AreaContainingFixations25(List<Fixation> fixations)
        {
            return AreaContainingFixations(fixations, 0.75);
        }

        // area helper functions
        private System.Windows.Point CalculateMean(List<Fixation> fixations)
        {
            System.Windows.Point mean = new System.Windows.Point();
            double count = fixations.Count;

            double xSum = 0;
            double ySum = 0;

            foreach(Fixation fixation in fixations)
            {
                xSum += fixation.x;
                ySum += fixation.y;
            }

            mean.X = xSum / count;
            mean.Y = ySum / count;

            return mean;
        }

        private List<Tuple<int, double>> DistancesFromPointToOtherPoints(System.Windows.Point centre, List<Fixation> fixations)
        {
            List<Tuple<int, double>> indexDistanceList = new List<Tuple<int, double>>();

            for(int i = 0; i < fixations.Count; i++)
            {
                Fixation fixation = fixations[i];

                int index = i;
                double distance = MathNet.Numerics.Distance.Euclidean(new double[] { centre.X, centre.Y }, new double[] { fixation.x, fixation.y });

                Tuple<int, double> distanceForFixationIndex = new Tuple<int, double>(index, distance);
                indexDistanceList.Add(distanceForFixationIndex);
            }

            return indexDistanceList;
        }

        private class AreaRect
        {
            System.Windows.Point centre = new System.Windows.Point();
            double top = 0;
            double right = 0;
            double bottom = 0;
            double left = 0;

            public AreaRect(double centreX, double centreY)
            {
                this.centre = new System.Windows.Point(centreX, centreY);
            }

            public double CalculateArea()
            {
                return (left + right) * (top + bottom);
            }
            
            public void IncreaseRectSizeToContainPoint(double x, double y)
            {
                Tuple<Double, Double> xRange = GetXRange();
                Tuple<Double, Double> yRange = GetYRange();
                
                if(!InRange(x, xRange))
                {
                    // the point is outside the x range.
                    // have to increase the width of the rect to encompass it

                    // increase the rect to the left
                    if (x < centre.X)
                    {
                        left = centre.X - x;
                    }

                    //increase the rect to the right
                    if (x > centre.X)
                    {
                        right = x - centre.X;
                    }
                }

                if (!InRange(y, yRange))
                {
                    // the point is outside the y range.
                    // have to increase the height of the rect to encompass it

                    // increase the rect to the top
                    if (y < centre.Y)
                    {
                        top = centre.Y - y;
                    }

                    // increase the rect towards the bottom
                    if (y > centre.Y)
                    {
                        bottom = y - centre.Y;
                    }
                }
            }

            private bool InRange(double value, Tuple<Double, Double> range)
            {
                return value >= range.Item1 && value <= range.Item2;
            }

            private Tuple<Double, Double> GetXRange()
            {
                double from = centre.X - left;
                double to = centre.X + right;
                return new Tuple<double, double>(from, to);
            }

            private Tuple<Double, Double> GetYRange()
            {
                double from = centre.Y - top;
                double to = centre.Y + bottom;
                return new Tuple<double, double>(from, to);
            }
        }

        // Software Eng Fixation Counts
        // ############################
        private double NumberOfEditorFixations(List<Fixation> fixations)
        {
            return CountFixationsInLocation(fixations, VSLocation.Editor);
        }

        private double NumberOfSolutionExplorerFixations(List<Fixation> fixations)
        {
            return CountFixationsInLocation(fixations, VSLocation.SolutionExplorer);
        }

        private double NumberOfOutputFixations(List<Fixation> fixations)
        {
            return CountFixationsInLocation(fixations, VSLocation.Output);
        }

        private int CountFixationsInLocation(List<Fixation> fixations, VSLocation vsLocation)
        {
            int count = 0;

            foreach (Fixation fixation in fixations)
            {
                if (fixation.location == vsLocation)
                {
                    count++;
                }
            }

            return count;
        }

        // Saccade related features
        // ########################

        private double SaccadeSizeMean(List<Saccade> saccades)
        {
            double sum = 0;
            int saccadeCount = 1;

            foreach (var saccade in saccades)
            {
                saccadeCount++;
                sum += saccade.Distance;
            }

            return sum / saccadeCount;
        }

        private double SaccadeSizeVariance(List<Saccade> saccades)
        {
            List<Double> distances = new List<Double>();

            foreach (var saccade in saccades)
            {
                var distance = saccade.Distance;
                distances.Add(distance);
            }

            return Statistics.Variance(distances);
        }

        private double SaccadeSizeStandardDeviation(List<Saccade> saccades)
        {
            return Math.Sqrt(SaccadeSizeVariance(saccades));
        }




        private double NumberOfShortSaccades(List<Saccade> saccades)
        {
            return CountSaccadesOfType(saccades, SaccadeType.Short);
        }

        private double NumberOfMediumSaccades(List<Saccade> saccades)
        {
            return CountSaccadesOfType(saccades, SaccadeType.Medium);
        }

        private double NumberOfLongSaccades(List<Saccade> saccades)
        {
            return CountSaccadesOfType(saccades, SaccadeType.Long);
        }

        private int CountSaccadesOfType(List<Saccade> saccades, SaccadeType type)
        {
            int count = 0;

            foreach (var saccade in saccades)
            {
                if (saccade.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private double SaccadeDirectionCounts(List<Saccade> saccades)
        {
            // key is 0 through 35
            // value is the count for that bucket
            Dictionary<int, int> counts = CSVGenerator.CalculateDirectionCounts(currentlyLoadedSaccades.ToArray());
            
            return 0;
        }

        // TODO: possible feature:  "longest follow streak, mean/median follow streak"
        //                          "longest opposite streak, mean/meadian opposite streak" 

        private double CountRelation(Saccade.Relation relation, List<Saccade> saccades)
        {
            int count = 0;
            int numberOfSaccades = saccades.Count;

            // Compare each pair of saccades.
            for (int i = 1; i < numberOfSaccades; i++)
            {
                Saccade previous = saccades[i - 1];
                Saccade next = saccades[i];

                if (Saccade.Compare(next, previous) == relation)
                {
                    count++;
                }
            }

            return count;
        }

        private double FollowDirectionCount(List<Saccade> saccades)
        {
            return CountRelation(Saccade.Relation.Follow, saccades);
        }

        private double NeighbouringDirectionCount(List<Saccade> saccades)
        {
            return CountRelation(Saccade.Relation.Neighbour, saccades);
        }

        private double OppositeDirectionCount(List<Saccade> saccades)
        {
            return CountRelation(Saccade.Relation.Opposite, saccades);
        }

        // Sector4
        private double Sector4RightCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector4 == Sector.Right).ToList().Count;
        }

        private double Sector4UpCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector4 == Sector.Up).ToList().Count;
        }

        private double Sector4LeftCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector4 == Sector.Left).ToList().Count;
        }

        private double Sector4DownCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector4 == Sector.Down).ToList().Count;
        }

        // Sector8

        private double Sector8RightCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.Right).ToList().Count;
        }

        private double Sector8UpCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.Up).ToList().Count;
        }

        private double Sector8LeftCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.Left).ToList().Count;
        }

        private double Sector8DownCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.Down).ToList().Count;
        }

        private double Sector8UpRightCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.UpRight).ToList().Count;
        }

        private double Sector8UpLeftCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.UpLeft).ToList().Count;
        }

        private double Sector8DownLeftCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.DownLeft).ToList().Count;
        }

        private double Sector8DownRightCount(List<Saccade> saccades)
        {
            return saccades.Where(saccade => saccade.Sector8 == SectorEight.DownRight).ToList().Count;
        }

        // wordbook features
        // #################

        private double NumberOfStrings(List<Saccade> saccades)
        {
            Wordbook saccadeBook = new Wordbook(saccades);
            AtomBook atomBook = new AtomBook(saccadeBook);
            return atomBook.NumberOfStrings;
        }

        private double NumberOfLines(List<Saccade> saccades)
        {
            Wordbook saccadeBook = new Wordbook(saccades);
            AtomBook atomBook = new AtomBook(saccadeBook);
            return atomBook.NumberOfLines;
        }

        private double NumberOfComparisons(List<Saccade> saccades)
        {
            Wordbook saccadeBook = new Wordbook(saccades);
            AtomBook atomBook = new AtomBook(saccadeBook);
            return atomBook.NumberOfComparisons;
        }

        private double NumberOfScans(List<Saccade> saccades)
        {
            Wordbook saccadeBook = new Wordbook(saccades);
            AtomBook atomBook = new AtomBook(saccadeBook);
            return atomBook.NumberOfScans;
        }
    }
}
