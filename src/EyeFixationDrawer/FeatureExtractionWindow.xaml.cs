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

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for FeatureExtractionWindow.xaml
    /// </summary>
    public partial class FeatureExtractionWindow : Window
    {
        private ObservableCollection<FeatureExtractor> list = new ObservableCollection<FeatureExtractor>();

        public FeatureExtractionWindow()
        {
            InitializeComponent();
            InitFeatureList();

            this.Closing += FeatureExtractionWindow_Closing;
        }

        private void FeatureExtractionWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<FeatureExtractor> extractors = GetFeatures();
            List<Fixation> tempFixations = new List<Fixation>();
            foreach (var extractor in extractors)
            {
                Console.Write(extractor.action(tempFixations) + ",");
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
            items.Add(new FeatureExtractor() { featureName = "feature 1", include = true, action = (List<Fixation> fixations) => 1 });
            items.Add(new FeatureExtractor() { featureName = "feature 2", include = true, action = (List<Fixation> fixations) => 2 });
            items.Add(new FeatureExtractor() { featureName = "feature 3", include = false, action = (List<Fixation> fixations) => 3 });

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
    }
}
