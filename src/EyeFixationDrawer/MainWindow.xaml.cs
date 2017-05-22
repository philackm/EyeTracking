using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;

using System.Timers;

using EyeXFramework; // Tobii
using Tobii.EyeX.Framework; // Tobii

using EyeTrackingCore; // From EyeTrackingCore project reference
using System.IO.Pipes;
using System.IO;
using Path = System.Windows.Shapes.Path;

using System.Text.RegularExpressions;
using System.Linq;

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Access to the Tobii eyeX
        private EyeXHost _eyeXHost;
        private GazePointDataStream stream;
        private bool trackingVSLocation = false;

        // GazePoints, Fixations & Saccades
        // TODO: These shouldn't be stored and calculated in the view: MainWindow, (need to refactor all of this)
        private List<GazePoint> gazePoints = new List<GazePoint>();
        private List<Fixation> calculatedFixations = new List<Fixation>();
        private List<Saccade> calculatedSaccades = new List<Saccade>();

        // Loading/Saving Data
        private XmlSerializer serialiser;

        // Fixation algorithm arguments
        private int currentWindowSize = 13;
        private double peakThreshold = 25;
        private double radius = 1;

        // Features
        List<FeatureExtractionWindow.FeatureExtractor> extractors = null;

        // UI Specifics
        // ################

        private enum CircleType
        {
            FixationCircle,
            GazeCircle
        }

        // Determines the maximum settable by the UI
        private int maxWindowSize = 100;
        private double maxPeakThreshold = 200;
        private double maxRadius = 200;

        // UI Representations of the fixations and saccades.
        private List<Ellipse> gazeCircles = new List<Ellipse>();
        private List<Ellipse> fixationCircles = new List<Ellipse>();
        private List<Line> saccadeLines = new List<Line>();
        private List<TextBlock> fixationTimeLabels = new List<TextBlock>();
        private List<Path> saccadeAnglePaths = new List<Path>();

        private double fixationCircleSize = 20;
        private double gazePointCircleSize = 6;
        private double saccadeLineWidth = 2;

        private SolidColorBrush gazePointBrush = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        private SolidColorBrush saccadeAngleBrush = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0));

        private Dictionary<VSLocation, System.Windows.Media.SolidColorBrush> VSLocationBrushes = 
            new Dictionary<VSLocation, System.Windows.Media.SolidColorBrush> {
                [VSLocation.Nothing] = System.Windows.Media.Brushes.Red,
                [VSLocation.Editor] = System.Windows.Media.Brushes.Orange,
                [VSLocation.SolutionExplorer] = System.Windows.Media.Brushes.Green,
                [VSLocation.Output] = System.Windows.Media.Brushes.Blue
            };

        private Dictionary<SaccadeType, System.Windows.Media.SolidColorBrush> SaccadeTypeBrushes =
            new Dictionary<SaccadeType, System.Windows.Media.SolidColorBrush>
            {
                [SaccadeType.Short] = System.Windows.Media.Brushes.Red,
                [SaccadeType.Medium] = System.Windows.Media.Brushes.Green,
                [SaccadeType.Long] = System.Windows.Media.Brushes.Blue
            };

        bool shouldDrawAngles = false;
        private double saccadeAngleRadius = 25;

        private double startTime;

        // Collecting user data.
        private string participantFolderLocation = null;

        private RecordingState currentRecordingState = RecordingState.Stopped;
        private object currentRecorder = null;
        private string participantID = null;

        // Initialisation
        // ##############

        public MainWindow()
        {
            InitializeComponent();
            serialiser = new XmlSerializer(gazePoints.GetType());

            InitSliders();

            gazePointBrush.Freeze();
            saccadeAngleBrush.Freeze();
        }

        private void InitEyeTracker()
        {
            // Initialize the EyeX Host 
            _eyeXHost = new EyeXHost();
            _eyeXHost.Start();

            // Create a data stream object and listen to events. 
            stream = _eyeXHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
            stream.Next += DrawCircleAtGazePoint;
            stream.Next += StoreGazePoint;
        }

        private void InitSliders()
        {
            windowSizeSlider.Value = (float)currentWindowSize / (float)maxWindowSize;
            peakThresholdSlider.Value = peakThreshold / maxPeakThreshold;
            radiusSlider.Value = radius / maxRadius;
        }

        private void StoreGazePoint(object sender, GazePointEventArgs args)
        {
            int elapsedMilliseconds = (int)(GetUnixMillisecondsForNow() - startTime);

            // If we are tracking Visual Studio locations

            VSLocation location = trackingVSLocation ? (VSLocation)GetVSWindowForScreenPoint(new System.Windows.Point(args.X, args.Y)) : VSLocation.Nothing; 

            GazePoint gazePoint = new GazePoint((float)args.X, (float)args.Y, elapsedMilliseconds, location);
            gazePoints.Add(gazePoint);
        }

        private double GetUnixMillisecondsForNow()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        // Drawing / Updating UI
        // #####################

        // Gaze Points
        private void DrawCircleAtGazePoint(object sender, GazePointEventArgs args)
        {
            DrawCircle(args.X, args.Y, gazePointBrush, gazePointCircleSize, CircleType.GazeCircle);
        }

        private void DrawAllGazePoints()
        {
            foreach (GazePoint gazePoint in gazePoints)
            {
                DrawCircle(gazePoint.x, gazePoint.y, gazePointBrush, gazePointCircleSize, CircleType.GazeCircle);
            }
        }

        private void ClearGazePoints()
        {
            foreach (Ellipse ellipse in gazeCircles)
            {
                canvas.Children.Remove(ellipse);
            }

            gazeCircles.Clear();
        }

        // Fixations
        private void DrawFixations()
        {
            RawToFixationConverter converter = new RawToFixationConverter(gazePoints);

            List<Fixation> fixations = converter.CalculateFixations(currentWindowSize, (float)peakThreshold, (float)radius, 0); // don't clip any gazepoints when simply drawing
            List<Saccade> saccades = converter.GenerateSaccades(fixations);

            calculatedSaccades = saccades;
            calculatedFixations = fixations;

            foreach (Fixation fixation in fixations)
            {
                double lengthOfFixation = fixation.endTime - fixation.startTime;
                double seconds = lengthOfFixation / 1000;

                System.Windows.Media.SolidColorBrush brush = VSLocationBrushes[fixation.location];
                DrawCircle(fixation.x, fixation.y, brush, fixationCircleSize, CircleType.FixationCircle);
                DrawLabel(seconds.ToString(), fixation.x + fixationCircleSize, fixation.y, brush);
            }
        }

        private void DrawCircle(double screenX, double screenY, SolidColorBrush brush, double size, CircleType circleType)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var canvasXY = ScreenToCanvas(new System.Windows.Point(screenX, screenY));

                Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                ellipse.Width = size;
                ellipse.Height = size;
                ellipse.Fill = brush;
                Canvas.SetLeft(ellipse, canvasXY.X);
                Canvas.SetTop(ellipse, canvasXY.Y);

                switch(circleType)
                {
                    case CircleType.GazeCircle:
                        gazeCircles.Add(ellipse);
                        break;
                    case CircleType.FixationCircle:
                        fixationCircles.Add(ellipse);
                        break;
                }

                canvas.Children.Add(ellipse);
            }));
        }

        private void RemoveFixationCircles()
        {
            foreach (Ellipse ellipse in fixationCircles)
            {
                canvas.Children.Remove(ellipse);
            }

            fixationCircles.Clear();
        }

        private void UpdateFixationCircles()
        {
            RemoveFixationCircles();
            RemoveSaccades();
            RemoveLabels();
            DrawFixation_Click(this, null);
        }

        private void DrawLabel(String text, double x, double y, System.Windows.Media.Brush brush)
        {
            System.Windows.Point canvasLocation = ScreenToCanvas(new System.Windows.Point(x, y));

            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.FontWeight = FontWeight.FromOpenTypeWeight(999);
            textBlock.Foreground = brush;

            Canvas.SetLeft(textBlock, canvasLocation.X);
            Canvas.SetTop(textBlock, canvasLocation.Y);

            fixationTimeLabels.Add(textBlock);
            canvas.Children.Add(textBlock);
        }

        private void RemoveLabels()
        {
            RemoveElements<TextBlock>(fixationTimeLabels);
        }

        private void RemoveElements<T>(List<T> elements)
        {
            foreach (T element in elements)
            {
                canvas.Children.Remove(element as UIElement);
            }

            elements.Clear();
        }

        // Saccades
        private void DrawSaccades(List<Saccade> saccades)
        {
            //Saccade previous = null; // testing

            // Draw the saccade lines.
            foreach (Saccade s in saccades)
            {
                EyeTrackingCore.Point from = s.From;
                EyeTrackingCore.Point to = s.To;

                System.Windows.Point start = ScreenToCanvas(new System.Windows.Point(s.From.x, s.From.y));
                System.Windows.Point end = ScreenToCanvas(new System.Windows.Point(s.To.x, s.To.y));

                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                line.X1 = start.X + (fixationCircleSize / 2);
                line.Y1 = start.Y + (fixationCircleSize / 2);

                line.X2 = end.X + (fixationCircleSize / 2);
                line.Y2 = end.Y + (fixationCircleSize / 2);

                line.StrokeThickness = saccadeLineWidth;
                line.Stroke = SaccadeTypeBrushes[s.Type];

                saccadeLines.Add(line);
                canvas.Children.Add(line);

                /*
                // testing
                if(previous != null)
                {
                    Console.WriteLine(Saccade.Compare(s, previous));
                }

                previous = s;
                */
            }

            // Draw the angle arcs.
            if (shouldDrawAngles)
            {
                foreach (Saccade s in saccades)
                {
                    DrawAngleArc(s);
                }
            }
        }

        private void RemoveSaccades()
        {
            foreach (Line line in saccadeLines)
            {
                canvas.Children.Remove(line);
            }

            foreach (Path path in saccadeAnglePaths)
            {
                canvas.Children.Remove(path);
            }

            saccadeLines.Clear();
        }


        private Path CreateArcPath(System.Windows.Point centre, double radians, double radius)
        {
            // Create a path to draw a geometry with.
            Path path = new Path();
            path.Stroke = saccadeAngleBrush;
            path.Fill = saccadeAngleBrush;
            path.StrokeThickness = 1;

            // Create a StreamGeometry to use to specify myPath.
            StreamGeometry geometry = new StreamGeometry();
            geometry.FillRule = FillRule.EvenOdd;

            // Open a StreamGeometryContext that can be used to describe this StreamGeometry object's contents. 
            using (StreamGeometryContext ctx = geometry.Open())
            {

                // start point is the centre
                // end point is the radius times (radius * cos(radians), radius* sin(radians)) 

                double endX = centre.X + (radius * Math.Cos(radians));
                double endY = centre.Y - (radius * Math.Sin(radians));
                System.Windows.Point endPoint = new System.Windows.Point(endX, endY);

                SweepDirection direction = radians < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                bool isLargeArc = radians > Math.PI ? true : false;


                // Set the begin point of the shape.
                ctx.BeginFigure(centre, true /* is filled */, true /* is closed */);

                ctx.LineTo(new System.Windows.Point(centre.X + radius, centre.Y), true, false);


                // Create an arc. Draw the arc from the begin point to 200,100 with the specified parameters.
                ctx.ArcTo(endPoint, new Size(radius, radius), 0 /* rotation angle */, isLargeArc /* is large arc */,
                          direction, true /* is stroked */, false /* is smooth join */);

                Console.WriteLine(radians);

                ctx.LineTo(centre, true, false);

            }

            // Freeze the geometry (make it unmodifiable)
            // for additional performance benefits.
            geometry.Freeze();

            // specify the shape (arc) of the path using the StreamGeometry.
            path.Data = geometry;

            return path;
        }

        private void DrawAngleArc(Saccade saccade)
        {
            // Want to draw the angle arc at the beginning of the saccade (?)
            System.Windows.Point arcCentre = new System.Windows.Point(saccade.From.x, saccade.From.y);
            System.Windows.Point arcCentreCanvas = ScreenToCanvas(arcCentre);

            arcCentreCanvas.X += fixationCircleSize / 2;
            arcCentreCanvas.Y += fixationCircleSize / 2;

            Double angle = saccade.Direction;

            // Create a path to draw a geometry with.
            Path arcPath = CreateArcPath(arcCentreCanvas, angle, saccadeAngleRadius);

            canvas.Children.Add(arcPath);
            saccadeAnglePaths.Add(arcPath);
        }

        // Helper method that converts a point in screen space to canvas space.
        private System.Windows.Point ScreenToCanvas(System.Windows.Point screenPosition)
        {
            double windowX = 0;
            double windowY = 0;

            this.Dispatcher.Invoke((Action)(() =>
            {
                windowX = this.Left;
                windowY = this.Top;
            }));

            double X = screenPosition.X - windowX;
            double Y = screenPosition.Y - windowY;

            return new System.Windows.Point(X, Y);
        }

        private System.Windows.Point CanvasToScreen(System.Windows.Point canvasPosition)
        {
            double windowX = 0;
            double windowY = 0;

            this.Dispatcher.Invoke((Action)(() =>
            {
                windowX = this.Left;
                windowY = this.Top;
            }));

            double X = canvasPosition.X + windowX;
            double Y = canvasPosition.Y + windowY;

            return new System.Windows.Point(X, Y);
        }



        // Events
        // ######

        // Start collecting gaze data button
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // Make note of when we started recording.
            startTime = GetUnixMillisecondsForNow();
            InitEyeTracker();
        }

        // Stop collecting gaze data button
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            stream.Next -= DrawCircleAtGazePoint;
            stream.Next -= StoreGazePoint;
        }

        // Draw fixation button
        private void DrawFixation_Click(object sender, RoutedEventArgs e)
        {
            DrawFixations();
            DrawSaccades(calculatedSaccades);
        }

        // Clear fixation button
        private void ClearFixation_Click(object sender, RoutedEventArgs e)
        {
            RemoveFixationCircles();
            RemoveSaccades();
            RemoveLabels();
        }

        // Window size slider changed
        private void WindowSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentWindowSize = (int)(e.NewValue * maxWindowSize);
            windowSizeLabel.Content = "Window size: " + currentWindowSize;

            UpdateFixationCircles();
        }

        // Peak threshold slider changed
        private void PeakThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            peakThreshold = e.NewValue * maxPeakThreshold;
            peakThresholdLabel.Content = "Peak Threshold: " + peakThreshold;

            UpdateFixationCircles();
        }

        // Radius slider changed
        private void Radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            radius = e.NewValue * maxRadius;
            radiusLabel.Content = "Radius: " + radius;

            UpdateFixationCircles();
        }

        // Saving / Loading
        // ################

        // TODO: Add loading functionality.

        // SaveData button
        // Currently just saves the raw gaze points out to an xml file in the same directory as the executable.
        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Getting location for save file...");
            string saveFileLocation = GetFileLocationForSave();
            SerialiseGazePoints(saveFileLocation);
        }

        private void SerialiseGazePoints(string fileLocation)
        {
            if (fileLocation != null)
            {
                System.IO.FileStream fileStream = new System.IO.FileStream(fileLocation, System.IO.FileMode.OpenOrCreate);

                try
                {
                    this.serialiser.Serialize(fileStream, gazePoints);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                finally
                {
                    fileStream.Close();
                }

                Console.WriteLine("Serialisation complete!");
            }
        }  

        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            openFileDialog.DefaultExt = ".xml";
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = openFileDialog.FileName;
                System.IO.FileStream fileStream = new System.IO.FileStream(filename, System.IO.FileMode.Open);

                try
                {
                    this.gazePoints = this.serialiser.Deserialize(fileStream) as List<GazePoint>;

                    // Clear anything that is on the canvas.
                    ClearGazePoints();
                    RemoveFixationCircles();
                    RemoveSaccades();

                    // Draw the data we just loaded in.
                    DrawAllGazePoints();
                    //DrawFixations();
                    //DrawSaccades();

                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                finally
                {
                    fileStream.Close();
                }

            }
        }

        private string GetFileLocationForSave()
        {
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = saveFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        // Features
        private void featureSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            Window featureExtractionWindow = new FeatureExtractionWindow(calculatedFixations, calculatedSaccades, this);
            featureExtractionWindow.Show();
        }

        public void SetExtractors(List<FeatureExtractionWindow.FeatureExtractor> extractors)
        {
            this.extractors = extractors;
        }

        private void saccadeAngleCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            shouldDrawAngles = saccadeAngleCheckbox.IsChecked ?? false;
        }

        private void directionFrequenciesButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<int, int> counts = CSVGenerator.CalculateDirectionCounts(calculatedSaccades.ToArray());
            CSVGenerator.CreateDirectionCSV(counts);
        }

        private int GetVSWindowForScreenPoint(System.Windows.Point screenPoint)
        {
            try
            {
                var client = new NamedPipeClientStream("VSServerPipe");
                Console.WriteLine("Sending message to server.");
                client.Connect(200);
                StreamReader reader = new StreamReader(client);
                StreamWriter writer = new StreamWriter(client);

                string requestMessage = String.Format("get {0} {1}\n", (int)screenPoint.X, (int)screenPoint.Y);
                writer.WriteLine(requestMessage);
                writer.Flush();

                Console.WriteLine(requestMessage);

                Console.WriteLine("Waiting on server...");
                string result = reader.ReadLine();

                if(result == "error")
                {
                    client.Dispose();
                    return 0;
                }
                else
                {
                    client.Dispose();
                    return Int32.Parse(result);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }

        private void sendMessageToServerButton_Click(object sender, RoutedEventArgs e)
        {
            //GetLocation();
        }

        private void canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // This is only for testing.
            /*
            int x = (int)e.GetPosition(canvas).X;
            int y = (int)e.GetPosition(canvas).Y;

            System.Windows.Point screenPoint = CanvasToScreen(new System.Windows.Point(x, y));

            int window = GetVSWindowForScreenPoint(screenPoint);
            System.Windows.MessageBox.Show(window.ToString());
            */
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove UI
            ClearGazePoints();
            RemoveFixationCircles();
            RemoveSaccades();
            RemoveLabels();

            // Clear the things we have actually calculated.
            gazePoints = new List<GazePoint>();
            calculatedFixations = new List<Fixation>();
            calculatedSaccades = new List<Saccade>();
        }




        // Data collection helpers.
        private void generateDataButton_Click(object sender, RoutedEventArgs e)
        {
            if(extractors == null)
            {
                System.Windows.MessageBox.Show("Extractors are null. Make sure you have selected the features you want to generate first.");
                return;
            }

            Window dataGenerationWindow = new DataGenerationWindow(currentWindowSize, (float)peakThreshold, (float)radius, extractors);
            dataGenerationWindow.Show();
        }

        private enum RecordingState
        {
            Recording,
            Stopped
        }



        private void RecordingButtonClick(object sender, RoutedEventArgs e)
        {
            switch(currentRecordingState)
            {
                // not currently recording
                case RecordingState.Stopped:
                    // Have to make sure we have a place to save.
                    if(FolderHasBeenChosen())
                    {
                        Start_Click(sender, e);

                        // Update the status label.
                        participantID = participantTextBox.Text;
                        statusLabel.Content = String.Format("{0} RECORDING {1}", GetTextOfSender(sender), participantID);

                        // Record who requested this and change the recording state.
                        currentRecorder = sender;
                        currentRecordingState = RecordingState.Recording;
                    }
                    else
                    {
                        MessageBox.Show("Please choose a directory first.");
                    }
                    
                    break;
                // currently recording
                case RecordingState.Recording:
                    // only do something if it was the same requester that started that wanted to stop
                    if(currentRecorder == sender)
                    {
                        // Stop recording.
                        Stop_Click(sender, e);
                        statusLabel.Content = "STOPPED";

                        // Save the gazepoints to a file, making sure it saves the file in the right place with the correct activity saved to it
                        string activityRaw = GetTextOfSender(currentRecorder).ToLower();
                        string activity = Regex.Replace(activityRaw, " ", "", RegexOptions.Compiled);
                        string saveFilename = String.Format("{0}_{1}.xml", participantID, activity);
                        string saveLocation = String.Format("{0}/{1}", participantFolderLocation, saveFilename);

                        SerialiseGazePoints(saveLocation);

                        // Clear everything out.
                        clearButton_Click(sender, e);
                        currentRecorder = null;
                        currentRecordingState = RecordingState.Stopped;
                    }
                    
                    break;
            }
        }

        private string GetTextOfSender(object sender)
        {
            Button button = (Button)sender;
            return button.Content.ToString();
        }

        private bool FolderHasBeenChosen()
        {
            return participantFolderLocation != null;
        }

        private void selectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            participantFolderLocation = GetFolderLocation();
        }

        private string GetFolderLocation()
        {
            // Get a place to save for this participant.
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }

            return null;
        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void watchButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void gameButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void determineOutputButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void debugButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void writeFunctionButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingButtonClick(sender, e);
        }

        private void includeVSLocationCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            trackingVSLocation = includeVSLocationCheckbox.IsChecked ?? false;
        }

        private void showBookButton_Click(object sender, RoutedEventArgs e)
        {

            Wordbook wordbook = new Wordbook(calculatedFixations, calculatedSaccades);

            // Testing wordbooks.
            Wordbook saccadeBook = new Wordbook(calculatedSaccades);
            MessageBox.Show(saccadeBook.SaccadeBook);

            Wordbook fixationBook = new Wordbook(calculatedFixations);
            MessageBox.Show(fixationBook.FixationBook);

            MessageBox.Show(fixationBook.VSLocationBook);

            double numberOfBriefs = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Brief ? acc + 1 : acc);
            double numberOfHolds = fixationBook.fixationTokens.Aggregate(0, (acc, next) => next == EyeTrackingCore.Token.Hold ? acc + 1 : acc);

            MessageBox.Show((numberOfBriefs / numberOfHolds).ToString());

            foreach (KeyValuePair<string, int> keyValuePair in saccadeBook.SortedSaccadeWordCount(4))
            {
                Console.Write(keyValuePair.Key + ": ");
                Console.WriteLine(keyValuePair.Value);
            }

            foreach (KeyValuePair<string, int> keyValuePair in fixationBook.SortedFixationWordCount(4))
            {
                Console.Write(keyValuePair.Key + ": ");
                Console.WriteLine(keyValuePair.Value);
            }

            foreach (KeyValuePair<string, int> keyValuePair in fixationBook.SortedLocationWordCount(4))
            {
                Console.Write(keyValuePair.Key + ": ");
                Console.WriteLine(keyValuePair.Value);
            }
        }

        private void showBookCountsButton_Click(object sender, RoutedEventArgs e)
        {
            Wordbook saccadeBook = new Wordbook(calculatedSaccades);
            MessageBox.Show(saccadeBook.SaccadeBook);

            string book = saccadeBook.SaccadeBook;
            int windowSize = 4; // 4 directions in a row, eg, SrSrSrLl


            Dictionary<string, int> counts = new Dictionary<string, int>();
            for(int i = 0; i < book.Length - windowSize * 2; i += 2) {

                string current = book.Substring(i, windowSize * 2);

                if (counts.ContainsKey(current))
                {
                    // the segment has already been seen, just increment.
                    int currentCount = counts[current];
                    counts[current] = ++currentCount;
                }
                else
                {
                    // the segment hasnt been seen, set it to 1.
                    counts[current] = 1;
                }
            }

            var countsList = counts.ToList();
            countsList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            foreach (KeyValuePair<string, int> kvp in countsList)
            {
                Console.WriteLine(String.Format("{0}:{1}", kvp.Key, kvp.Value));
            }
        }



        // Finding and showing ATOMS

        private void localAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            Wordbook saccadeBook = new Wordbook(calculatedSaccades);
            AtomBook atomBook = new AtomBook(saccadeBook);

            MessageBox.Show(atomBook.NumberOfScans.ToString());
        }
        
        private void drawNextFocalPointButton_Click(object sender, RoutedEventArgs e)
        {
            DrawNextAtom(AtomType.ScanHorizontal);
        }

        private void drawNextHorizontalCompareButton_Click(object sender, RoutedEventArgs e)
        {
            DrawNextAtom(AtomType.ScanHorizontalAlt);
        }

        private void drawNextVertCompare_Click(object sender, RoutedEventArgs e)
        {
            DrawNextAtom(AtomType.CompareVertical);
        }

        static int current = 0;
        private void DrawNextAtom(AtomType type)
        {
            Wordbook saccadeBook = new Wordbook(calculatedSaccades);
            AtomBook atomBook = new AtomBook(saccadeBook);

            List<Atom> mediumLines = atomBook.atoms[type];

            if (current < mediumLines.Count)
            {
                RemoveLabels();
                RemoveFixationCircles();
                RemoveSaccades();
                DrawSaccades(mediumLines[current++].saccades);
            }
            else
            {
                current = 0;
            }
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            current = 0;
        }
    }
}
