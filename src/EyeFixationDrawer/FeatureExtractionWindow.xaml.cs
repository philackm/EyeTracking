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
        private ObservableCollection<FeatureExtractor> list = new ObservableCollection<FeatureExtractor>();
        private List<Fixation> currentlyLoadedFixations;

        public FeatureExtractionWindow(List<Fixation> fixations)
        {
            InitializeComponent();
            InitFeatureList();

            this.currentlyLoadedFixations = fixations;
            this.Closing += FeatureExtractionWindow_Closing;
        }

        private void FeatureExtractionWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<FeatureExtractor> extractors = GetFeatures();
            
            foreach (var extractor in extractors)
            {
                Console.Write(extractor.action(currentlyLoadedFixations) + ",");
            }
            Console.Write("\n");
        }

        public class FeatureExtractor
        {
            public bool include { get; set; }
            public string featureName { get; set; }
            public Func<List<Fixation>, double> action;
        }

        private void InitFeatureList()
        {
            List<FeatureExtractor> items = new List<FeatureExtractor>();

            items.Add(new FeatureExtractor() { featureName = "Fixation Duration (mean)", include = true, action = FixationDurationMean });
            items.Add(new FeatureExtractor() { featureName = "Fixation Duration (variance)", include = true, action = FixationDurationVariance });
            items.Add(new FeatureExtractor() { featureName = "Fixation Duration (standard deviation)", include = true, action = FixationDurationStandardDeviation });
            items.Add(new FeatureExtractor() { featureName = "Fixation Rate (per second)", include = true, action = FixationRatePerSecond });
            items.Add(new FeatureExtractor() { featureName = "Fixation Slope", include = true, action = FixationSlope });
            items.Add(new FeatureExtractor() { featureName = "feature 6", include = true, action = (List<Fixation> fixations) => 6 });
            items.Add(new FeatureExtractor() { featureName = "feature 7", include = false, action = (List<Fixation> fixations) => 7 });
            items.Add(new FeatureExtractor() { featureName = "feature 8", include = true, action = (List<Fixation> fixations) => 8 });
            items.Add(new FeatureExtractor() { featureName = "feature 9", include = false, action = (List<Fixation> fixations) => 9 });
            items.Add(new FeatureExtractor() { featureName = "feature 10", include = true, action = (List<Fixation> fixations) => 10 });
            items.Add(new FeatureExtractor() { featureName = "feature 11", include = false, action = (List<Fixation> fixations) => 11 });

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

            Tuple<double, double> result = SimpleRegression.Fit(samples);

            return result.Item2;
        }
    }
}
