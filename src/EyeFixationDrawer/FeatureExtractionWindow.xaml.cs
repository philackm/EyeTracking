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

            // Saccade related features.
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Short Saccades", include = true, action = NumberOfShortSaccades });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Long Saccades", include = true, action = NumberOfLongSaccades });

            // Saccade direction counts. (36 features, one for each 10 degree bucket)
            // TODO: figure out how we can write out 36 features whilst passing one function
            // items.Add(new SaccadeFeatureExtractor() { featureName = "Saccade Direction Counts", include = true, action = SaccadeDirectionCounts });

            // For each successive pair of saccades, how many had an opposite direction, how many had a neighbouring direction.
            items.Add(new SaccadeFeatureExtractor() { featureName = "Follow Direction Count", include = true, action = FollowDirectionCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Neighbouring Direction Count", include = true, action = NeighbouringDirectionCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Opposite Direction Count", include = true, action = OppositeDirectionCount });

            // For the 4 sectors, how many of each sector do we have?
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Right Count", include = true, action = Sector4RightCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Up Count", include = true, action = Sector4UpCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Left Count", include = true, action = Sector4LeftCount });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Sector4 Down Count", include = true, action = Sector4DownCount });

            // Wordbook / Pattern related features.

            // "Saccade direction" based features.
            // inversity
            // category inversity
            // neighbouring direction

            // Wordbook related features.

            // Blink related features.



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

        // Software Eng Fixation Counts
        private double NumberOfEditorFixations(List<Fixation> fixations)
        {
            return 0;
        }

        private double NumberOfSolutionExplorerFixations(List<Fixation> fixations)
        {
            return 0;
        }

        private double NumberOfOutputFixations(List<Fixation> fixations)
        {
            return 0;
        }

        // Saccade related features
        private double NumberOfShortSaccades(List<Saccade> saccades)
        {
            int count = 0;

            foreach (var saccade in saccades)
            {
                if (saccade.Type == SaccadeType.Short)
                {
                    count++;
                }
            }

            return count;
        }

        private double NumberOfLongSaccades(List<Saccade> saccades)
        {
            int count = 0;

            foreach (var saccade in saccades)
            {
                if (saccade.Type == SaccadeType.Long)
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

    }
}
