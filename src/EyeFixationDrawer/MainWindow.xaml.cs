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

        // GazePoints, Fixations & Saccades
        // TODO: These shouldn't be stored and calculated in the view: MainWindow, (need to refactor all of this)
        private List<GazePoint> gazePoints = new List<GazePoint>();
        private List<Fixation> calculatedFixations = new List<Fixation>();
        private List<Saccade> calculatedSaccades = new List<Saccade>();

        // Loading/Saving Data
        private XmlSerializer serialiser;

        // Fixation algorithm arguments
        private int currentWindowSize = 13;
        private double peakThreshold = 50;
        private double radius = 10;

        // UI Specifics
        // ################

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

        bool shouldDrawAngles = false;

        private double saccadeAngleRadius = 25;

        private double startTime;

        // Initialisation
        // ##############

        public MainWindow()
        {
            InitializeComponent();
            serialiser = new XmlSerializer(gazePoints.GetType());

            InitSliders();

            // Make note of when we started recording.
            startTime = GetUnixMillisecondsForNow();

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
            GazePoint gazePoint = new GazePoint((float)args.X, (float)args.Y, elapsedMilliseconds);
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
            DrawCircle(args.X, args.Y, gazePointBrush, gazePointCircleSize);
        }

        private void DrawAllGazePoints()
        {
            foreach (GazePoint gazePoint in gazePoints)
            {
                DrawCircle(gazePoint.x, gazePoint.y, gazePointBrush, gazePointCircleSize);
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

            List<Fixation> fixations = converter.CalculateFixations(currentWindowSize, (float)peakThreshold, (float)radius);
            List<Saccade> saccades = converter.GenerateSaccades(fixations);

            calculatedSaccades = saccades;
            calculatedFixations = fixations;

            foreach (Fixation fixation in fixations)
            {
                double lengthOfFixation = fixation.endTime - fixation.startTime;
                double seconds = lengthOfFixation / 1000;

                DrawCircle(fixation.x, fixation.y, System.Windows.Media.Brushes.Red, fixationCircleSize);
                DrawLabel(seconds.ToString(), fixation.x + fixationCircleSize, fixation.y, System.Windows.Media.Brushes.Red);
            }
        }

        private void DrawCircle(double screenX, double screenY, SolidColorBrush brush, double size)
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

                // This is absolutey horrible, needs to change.
                if (brush == System.Windows.Media.Brushes.Red)
                {
                    fixationCircles.Add(ellipse);
                }
                else if(brush == System.Windows.Media.Brushes.Black)
                {
                    gazeCircles.Add(ellipse);
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
        private void DrawSaccades()
        {
            // Draw the saccade lines.
            for (int i = 1; i < fixationCircles.Count; i++)
            {
                Ellipse from = fixationCircles[i - 1];
                Ellipse to = fixationCircles[i];

                double startX = Canvas.GetLeft(from) + (fixationCircleSize / 2);
                double startY = Canvas.GetTop(from) + (fixationCircleSize / 2);

                double endX = Canvas.GetLeft(to) + (fixationCircleSize / 2);
                double endY = Canvas.GetTop(to) + (fixationCircleSize / 2);

                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                line.X1 = startX;
                line.Y1 = startY;

                line.X2 = endX;
                line.Y2 = endY;

                line.StrokeThickness = saccadeLineWidth;
                line.Stroke = System.Windows.Media.Brushes.Red;

                saccadeLines.Add(line);
                canvas.Children.Add(line);
            }

            
            // Draw the angle arcs.
            if(shouldDrawAngles)
            {
                foreach (Saccade s in calculatedSaccades)
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

            foreach(Path path in saccadeAnglePaths)
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

        // Events
        // ######

        // Start collecting gaze data button
        private void Start_Click(object sender, RoutedEventArgs e)
        {
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
            DrawSaccades();
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
            Console.WriteLine("Saving Data...");

            System.IO.FileStream fileStream = new System.IO.FileStream("./gazePoints.xml", System.IO.FileMode.OpenOrCreate);

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

            Console.WriteLine("Save complete!");
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

        private void featureSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            Window featureExtractionWindow = new FeatureExtractionWindow(calculatedFixations);
            featureExtractionWindow.Show();
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
    }
}
