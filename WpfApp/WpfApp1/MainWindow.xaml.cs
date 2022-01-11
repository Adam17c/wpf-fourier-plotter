using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DispatcherTimer Timer;
        Stopwatch stopWatch = new Stopwatch();
        bool drawing = false;

        List<Circle> list = new List<Circle>();
        Polyline blueLine = new Polyline();

        public class CircleList
        {
            [XmlArray("CircleList")]
            public List<Circle> list = new List<Circle>();
        }

        public MainWindow()
        {
            InitializeComponent();

            Timer = new DispatcherTimer(DispatcherPriority.Render);
            Timer.Tick += TimerOnTick;
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 1);

            blueLine.Stroke = Brushes.Blue;
            blueLine.StrokeThickness = 1;
            blueLine.HorizontalAlignment = HorizontalAlignment.Left;
            blueLine.VerticalAlignment = VerticalAlignment.Center;

            this.DataContext = list;
        }

        private void TimerOnTick(object sender, object o)
        {
            if(stopWatch.IsRunning)
            {
                double seconds = stopWatch.Elapsed.TotalSeconds;
                pBar.Value = seconds;
                if (seconds >= pBar.Maximum)
                {
                    stopWatch.Stop();
                    drawing = false;
                }
                Draw();
            }
        }

        //https://stackoverflow.com/questions/14096138/find-the-point-on-a-circle-with-given-center-point-radius-and-degree
        private void Draw()
        {
            canvas.Children.Clear();
            double seconds = stopWatch.Elapsed.TotalSeconds;
            Matrix matrix;
            double angle;//, offset = 0;
            Point canvasCenter = new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            Circle prev = null;
            foreach (Circle circle in list)
            {
                circle.center = prev == null ? canvasCenter : prev.edge;
                
                angle = (360 * (seconds / 10) * circle.frequency) % 360;
                matrix = new RotateTransform(angle, circle.center.X, circle.center.Y).Value;
                Point point;
                if (prev != null)
                    point = new Point((prev.edge.X + circle.radius), prev.edge.Y);
                else point = new Point((canvasCenter.X + circle.radius), canvasCenter.Y);

                Point rotated = Point.Multiply(point, matrix);
                circle.edge = rotated;

                prev = circle;
            }

            if (prev != null)
            {
                Point lastPoint = prev.edge;
                blueLine.Points.Add(lastPoint);
            }

            double X = prev == null ? canvasCenter.X : prev.edge.X;
            double Y = prev == null ? canvasCenter.Y : prev.edge.Y;

            Ellipse redDot = new Ellipse();
            redDot.Fill = Brushes.Red;
            redDot.Stroke = null;
            redDot.Width = 4;
            redDot.Height = 4;
            Canvas.SetLeft(redDot, X - redDot.Width / 2);
            Canvas.SetTop(redDot, Y - redDot.Height / 2);
            canvas.Children.Add(redDot);

            DrawCircles();
            DrawLines();
            DrawPen();
        }

        private void DrawCircles()
        {
            if (!drawCircles.IsChecked) return;
            Point canvasCenter = new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            double X, Y;
            Circle prev = null;
            foreach (Circle circle in list)
            {
                double rad = circle.radius;
                Ellipse ellipse = new Ellipse();
                ellipse.Stroke = Brushes.Black;
                ellipse.Width = 2*rad;
                ellipse.Height = 2*rad;
                ellipse.StrokeThickness = 1;

                X = prev == null ? canvasCenter.X : prev.edge.X;
                Y = prev == null ? canvasCenter.Y : prev.edge.Y; 

                Canvas.SetLeft(ellipse, X - rad);
                Canvas.SetTop(ellipse, Y - rad);

                canvas.Children.Add(ellipse);

                prev = circle;
            }
        }

        private void DrawLines()
        {
            if (!drawLines.IsChecked) return;
            foreach(Circle circle in list)
            {
                Line line = new Line();
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Black;
                line.SnapsToDevicePixels = true;
                line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                
                line.X1 = circle.center.X;
                line.Y1 = circle.center.Y;
                line.X2 = circle.edge.X;
                line.Y2 = circle.edge.Y;

                canvas.Children.Add(line);
            }
        }

        private void DrawPen()
        {
            canvas.Children.Add(blueLine);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            stopWatch.Start();
            Timer.Start();
            drawing = true;
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            stopWatch.Stop();
            Timer.Stop();
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            stopWatch.Reset();
            Timer.Stop();
            pBar.Value = 0;
            canvas.Children.Clear();
            blueLine.Points.Clear();
            Draw();
            drawing = false;
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to leave?", "Exit", MessageBoxButton.YesNo))
                Close();
        }

        private void drawCircles_Click(object sender, RoutedEventArgs e)
        {
            Draw();
        }

        private void drawLines_Click(object sender, RoutedEventArgs e)
        {
            Draw();
        }

        private void dataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            Draw();
        }


        private void dataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if(drawing)
                blueLine.Points.Clear();
            Draw();
        }

        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            list.Clear();
            canvas.Children.Clear();
            blueLine.Points.Clear();
            stopWatch.Reset();
            pBar.Value = 0;

            //https://stackoverflow.com/questions/11324688/how-to-refresh-datagrid-in-wpf
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = (IEnumerable<Circle>)DataContext;

            Draw();
        }
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var filePath = string.Empty;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "XML (*.xml)";
            openFileDialog.Filter = "XML (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            var serializer = new XmlSerializer(typeof(CircleList));
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                FileStream stream = new FileStream(filePath, FileMode.Open);
                CircleList other;
                try
                {
                    other = (CircleList)(serializer.Deserialize(stream));
                }
                catch (Exception)
                {
                    MessageBox.Show("Select correct .xml file!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                stream.Close();
                list.Clear();
                list.AddRange(other.list);
                blueLine.Points.Clear();
            }
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = (IEnumerable<Circle>)DataContext;
            Draw();
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var filePath = string.Empty;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "XML (*.xml)";
            saveFileDialog.Filter = "XML (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            var serializer = new XmlSerializer(typeof(CircleList));
            if (saveFileDialog.ShowDialog() == true)
            {
                filePath = saveFileDialog.FileName;
                FileStream stream;
                if ((stream = (FileStream)saveFileDialog.OpenFile()) != null)
                {
                    CircleList circleList = new CircleList();
                    circleList.list = list;
                    serializer.Serialize(stream, circleList);
                    stream.Close();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Draw();
        }

    }
}
