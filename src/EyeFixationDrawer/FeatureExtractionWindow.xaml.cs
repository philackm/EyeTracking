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
        private ObservableCollection<FixationFeatureExtractor> list = new ObservableCollection<FixationFeatureExtractor>();
        private List<Fixation> currentlyLoadedFixations;
        private List<Saccade> currentlyLoadedSaccades;

        public FeatureExtractionWindow(List<Fixation> fixations, List<Saccade> saccades)
        {
            InitializeComponent();
            InitFeatureList();

            this.currentlyLoadedFixations = fixations;
            this.currentlyLoadedSaccades = saccades;
            this.Closing += FeatureExtractionWindow_Closing;
        }

        private void FeatureExtractionWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<FixationExtractor> extractors = GetFeatures();
            
            foreach (var extractor in extractors)
            {
                switch(extractor.DataType())
                {
                    case RequiredData.Fixation:
                        var fixationExtractor = extractor as FixationFeatureExtractor;
                        Console.Write(fixationExtractor.action(currentlyLoadedFixations) + ",");
                        break;
                    case RequiredData.Saccade:
                        var saccadeExtractor = extractor as SaccadeFeatureExtractor;
                        Console.Write(saccadeExtractor.action(currentlyLoadedSaccades) + ",");
                        break;
                }
                
            }
            Console.Write("\n");
        }


        public enum RequiredData
        {
            Fixation,
            Saccade
        }

        public abstract class FixationExtractor
        {
            public bool include { get; set; }
            public string featureName { get; set; }
            public abstract RequiredData DataType();
        }

        public class FixationFeatureExtractor : FixationExtractor
        {
            public Func<List<Fixation>, double> action;

            override public RequiredData DataType()
            {
                return RequiredData.Fixation;
            }
        }

        public class SaccadeFeatureExtractor : FixationExtractor
        {
            public Func<List<Saccade>, double> action;

            override public RequiredData DataType()
            {
                return RequiredData.Saccade;
            }
        }

        private void InitFeatureList()
        {
            List<FixationExtractor> items = new List<FixationExtractor>();

            // Fixation related features.
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (mean)", include = true, action = FixationDurationMean });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (variance)", include = true, action = FixationDurationVariance });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Duration (standard deviation)", include = true, action = FixationDurationStandardDeviation });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Rate (per second)", include = true, action = FixationRatePerSecond });
            items.Add(new FixationFeatureExtractor() { featureName = "Fixation Slope", include = true, action = FixationSlope });

            // Saccade related features.
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Short Saccades", include = true, action = NumberOfShortSaccades });
            items.Add(new SaccadeFeatureExtractor() { featureName = "Number of Long Saccades", include = false, action = NumberOfLongSaccades });
            items.Add(new SaccadeFeatureExtractor() { featureName = "feature 8", include = true, action = (List<Saccade> fixations) => 8 });
            items.Add(new SaccadeFeatureExtractor() { featureName = "feature 9", include = true, action = (List<Saccade> fixations) => 9 });
            items.Add(new SaccadeFeatureExtractor() { featureName = "feature 10", include = true, action = (List<Saccade> fixations) => 10 });
            items.Add(new SaccadeFeatureExtractor() { featureName = "feature 11", include = true, action = (List<Saccade> fixations) => 11 });

            // "Saccade direction" based features.
            // inversity
            // category inversity
            // neighbouring direction

            // Wordbook related features.

            // Blink related features.



            featureList.ItemsSource = items;
        }

        private List<FixationExtractor> GetFeatures()
        {
            var itemsSource = featureList.ItemsSource;
            List<FixationExtractor> featuresToExtract = new List<FixationExtractor>();

            foreach (FixationExtractor item in itemsSource)
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
    }
}
