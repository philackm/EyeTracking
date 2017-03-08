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
        private List<GazePoint> gazePoints = new List<GazePoint>();
        // TODO: Fixations
        // TODO: Saccades

        // UI Representations of the fixations and saccades.
        private List<Ellipse> gazeCircles = new List<Ellipse>();
        private List<Ellipse> fixationCircles = new List<Ellipse>();
        private List<Line> saccades = new List<Line>();

        // Loading/Saving Data
        private XmlSerializer serialiser;

        // Fixation algorithm arguments
        private int currentWindowSize = 13;
        private double peakThreshold = 50;
        private double radius = 10;

        // Determines the maximum settable by the UI
        private int maxWindowSize = 100;
        private double maxPeakThreshold = 1000;
        private double maxRadius = 1000;

        // UI Specifics
        private double fixationCircleSize = 20;
        private double gazePointCircleSize = 5;
        private double saccadeLineWidth = 2;

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
            DrawCircle(args.X, args.Y, System.Windows.Media.Brushes.Black, gazePointCircleSize);
        }

        private void DrawAllGazePoints()
        {
            foreach (GazePoint gazePoint in gazePoints)
            {
                DrawCircle(gazePoint.x, gazePoint.y, System.Windows.Media.Brushes.Black, 5);
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

            foreach (Fixation fixation in fixations)
            {
                DrawCircle(fixation.x, fixation.y, System.Windows.Media.Brushes.Red, fixationCircleSize);
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
            DrawFixation_Click(this, null);
        }

        // Saccades
        private void DrawSaccades()
        {
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

                saccades.Add(line);
                canvas.Children.Add(line);
            }
        }

        private void RemoveSaccades()
        {
            foreach (Line line in saccades)
            {
                canvas.Children.Remove(line);
            }

            saccades.Clear();
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
    }
}
