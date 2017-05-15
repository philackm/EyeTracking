using System;
using System.Collections.Generic;
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

using EyeTrackingCore;
using System.Xml.Serialization;
using System.IO;

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for DataGenerationWindow.xaml
    /// </summary>
    public partial class DataGenerationWindow : Window
    {
        private XmlSerializer serialiser;
        private List<GazePoint> gazePoints = new List<GazePoint>(); // Stores the gazepoints for each file we load.

        // To generate the csv files, we need to know:

        // The windows size, peak threshold, radius
        // The feature extractors we are going to use.
        float windowSize = 0;
        float peakThreshold = 0;
        float radius = 0;

        List<FeatureExtractionWindow.FeatureExtractor> extractors = new List<FeatureExtractionWindow.FeatureExtractor>();

        public DataGenerationWindow(float windowSize, float peakThreshold, float radius, List<FeatureExtractionWindow.FeatureExtractor> extractors)
        {
            this.windowSize = windowSize;
            this.peakThreshold = peakThreshold;
            this.radius = radius;

            this.extractors = extractors;

            InitializeComponent();
            InitDataItems();

            this.serialiser = new XmlSerializer(gazePoints.GetType());
        }

        private void InitDataItems()
        {
            Data one = new Data("example_file_1.xml", "READ");
            Data two = new Data("example_file_2.xml", "WATCH");

            List<Data> datum = new List<Data>();

            datum.Add(one);
            datum.Add(two);

            dataFileList.ItemsSource = datum;
        }

        private void loadDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;

            // Set filter for file extension and default file extension 
            openFileDialog.DefaultExt = ".xml";
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string[] filenames = openFileDialog.FileNames;

                List<Data> datum = new List<Data>();

                foreach (string filename in filenames)
                {
                    try
                    {
                        Data one = new Data(filename, "CLASS");
                        datum.Add(one);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }

                // Set the items source for the window to be the data files we have just loaded in.
                dataFileList.ItemsSource = datum;
            }
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = ".csv";

            Nullable<bool> result = saveFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {

                // Want to generate instances for all of the selected slice times.

                foreach (int sliceTime in GetSliceLengths())
                {
                    // Get the name the user gave the file.
                    string saveFileName = saveFileDialog.FileName;
                    bool includeNames = true;

                    // Add the slice time to the end of the filename. (We generate as many files as the needed with <saveFileName>_sliceTime.csv as the filename)
                    string updatedFileName = saveFileName.Insert(saveFileName.Length - 4, String.Format("_{0}s", sliceTime));
                    Console.WriteLine(sliceTime);
                    Console.WriteLine(updatedFileName);

                    // Setup the file stream writer.
                    System.IO.FileStream fileStream = new System.IO.FileStream(updatedFileName, System.IO.FileMode.Append);
                    StreamWriter writer = new StreamWriter(fileStream);
                    

                    // Write the column names to the top 
                    if (includeNames)
                    {
                        for (int i = 0; i < extractors.Count - 1; i++)
                        {
                            writer.Write(extractors[i].featureName + ", ");
                        }
                        writer.Write(extractors[extractors.Count - 1].featureName);

                        writer.Write("\n");
                        writer.Flush();
                    }

                    // We have the fileName to save the instances to, generate all the instances and then save them to the file at this location.

                    // Foreach data file loaded we need to get the gazepoints and then convert them to fixations, then we can slice them.
                    foreach (Data dataFile in dataFileList.Items)
                    {
                        string filename = dataFile.fileName;

                        System.IO.FileStream readFileStream = new System.IO.FileStream(filename, System.IO.FileMode.Open);
                        this.gazePoints = this.serialiser.Deserialize(readFileStream) as List<GazePoint>;
                        readFileStream.Close();

                        RawToFixationConverter converter = new RawToFixationConverter(gazePoints);
                        List<Fixation> fixations = converter.CalculateFixations((int)windowSize, (float)peakThreshold, (float)radius);

                        // Now we can slice the fixations for this data file, write it to the csv and append the class for this data file to the end.
                        List<Slice> slices = SliceFixations(fixations, sliceTime * 1000); // convert sliceTime to milliseconds

                        foreach (Slice slice in slices)
                        {
                            foreach (var extractor in extractors)
                            {
                                switch (extractor.DataType())
                                {
                                    case FeatureExtractionWindow.RequiredData.Fixation:
                                        var fixationExtractor = extractor as FeatureExtractionWindow.FixationFeatureExtractor;
                                        writer.Write(fixationExtractor.action(slice.fixations) + ", ");
                                        break;
                                    case FeatureExtractionWindow.RequiredData.Saccade:
                                        var saccadeExtractor = extractor as FeatureExtractionWindow.SaccadeFeatureExtractor;
                                        writer.Write(saccadeExtractor.action(slice.saccades) + ", ");
                                        break;
                                }

                            }

                            writer.Write(dataFile.className);
                            writer.Write("\n");
                            writer.Flush();
                        }
                    }

                    writer.Close();
                }
            }
        }


        private int[] GetSliceLengths()
        {
            Dictionary<int, bool> sliceLengths = new Dictionary<int, bool>();

            sliceLengths.Add(15, checkBox15s.IsChecked.Value);
            sliceLengths.Add(30, checkBox30s.IsChecked.Value);
            sliceLengths.Add(60, checkBox60s.IsChecked.Value);
            sliceLengths.Add(90, checkBox90s.IsChecked.Value);

            // Only return an array of the keys of the ones that are actually checked.
            return sliceLengths.Where(kvp => kvp.Value).ToDictionary(i => i.Key, i => i.Value).Keys.ToArray();
        }

        // Need to return an array of List<Fixation>, and then calculate all features for each
        // element (slice) of the fixations.

        // We can either slice by: number of fixations or time period

        /*
        private List<Slice> SliceFixations(List<Fixation> allFixations, int numberOfFixations) {
            // go through the array, keeping tracking of teh current count and adding each fixation to the current slide
            // when reach numberOfFixations, add current slice to array, then start a new slice

            List<Slice> slices = new List<Slice>();
            List<Fixation> currentSliceFixations = new List<Fixation>();
            RawToFixationConverter converter = new RawToFixationConverter();

            int count = 0;
            
            foreach(Fixation fixation in allFixations) {   
                
                count++;
                currentSliceFixations.Add(fixation);
                
                if(count >= numberOfFixations) {
                    Slice slice = new Slice();
                    
                    slice.fixations = currentSliceFixations;
                    slice.saccades = converter.GenerateSaccades(currentSliceFixations);

                    slices.Add(slice);
                    currentSliceFixations = new List<Fixation>();
                }
            }

            return slices;
        }
        */

        // timePeriod in milliseconds, e.g., 1000 for 1 second, 300,000 for 5 minutes.
        // slices it with an overlapping window, each successive window overlaps half of the previous window
        private List<Slice> SliceFixations(List<Fixation> allFixations, double timePeriod)
        {
            // go through array adding the elapsed between each fixation, each time adding fixation to slice
            // when elapsed time sum > timePeriod, then add slide to array and start new slice.

            List<Slice> slices = new List<Slice>();
            List<Fixation> currentSliceFixations = new List<Fixation>();
            RawToFixationConverter converter = new RawToFixationConverter();

            double elapsedTime = 0;

            int windowCount = 0;
            double windowMiddle = allFixations[0].startTime + (timePeriod / 2);
            int savedIndex = -1;

            // basically, we move the window forward timePeriod/2 fixations at a time.
            // then we go from that fixation, collecting all fixations that are within timePeriod from that fixation
            for (int i = 0; i < allFixations.Count; i++)
            {
                Fixation fixation = allFixations[i];

                elapsedTime += (fixation.endTime - fixation.startTime);
                currentSliceFixations.Add(fixation);

                // check if we are half way through this window, save the index if we are
                if (savedIndex < 0 && windowMiddle <= fixation.startTime)
                {
                    savedIndex = i;
                }
                // BUG: MAYBE: If there is missing data and a big jump in time, we will never set savedIndex to the middle index.
                //          This means windowMiddle will try to be set to -1 once we have reached the elapsedTime.

                // if we found all the fixations within the timePeriod starting from the starting index for this window
                if (elapsedTime >= timePeriod)
                {
                    Slice slice = new Slice();

                    slice.fixations = currentSliceFixations;
                    slice.saccades = converter.GenerateSaccades(currentSliceFixations);

                    slices.Add(slice);
                    currentSliceFixations = new List<Fixation>();
                    Console.WriteLine("New slice.");

                    // reset the time counter
                    elapsedTime = 0;

                    // update the position we have to go back to
                    i = savedIndex;
                    // unset this so it is saved in the next round
                    savedIndex = -1;
                    // increment slice count
                    windowCount++;
                    // calculate the new time that will be the middle of the next sliding window
                    windowMiddle = allFixations[i].startTime + (timePeriod / 2);
                }
            }

            // have to add the final (possibly incomplete) slice.
            Slice incompleteSlice = new Slice();
            incompleteSlice.fixations = currentSliceFixations;
            incompleteSlice.saccades = converter.GenerateSaccades(currentSliceFixations);
            slices.Add(incompleteSlice);

            return slices;
        }

        struct Slice
        {
            public List<Fixation> fixations;
            public List<Saccade> saccades;
        }
    }

    public class Data
    {
        public string fileName { get; set; }
        public string className { get; set; }

        public Data(string fileName, string className)
        {
            this.fileName = fileName;
            this.className = className;
        }
    }
}
