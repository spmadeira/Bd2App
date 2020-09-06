using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace Bd2App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public List<List<BucketRepresentation>> bucketRepresentations;
        public int currentIndex;
        
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            _worker.DoWork += WorkerOnDoWork;
            _worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += WorkerOnProgressChanged;
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DataGrid.ItemsSource = bucketRepresentations[currentIndex];
            DataGrid.Visibility = Visibility.Visible;
            LoadingIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            PageControls.Visibility = Visibility.Visible;
            PageLabel.Content = $"{currentIndex+1}/{bucketRepresentations.Count}";
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            dynamic arg = e.Argument;

            var lines = File.ReadAllLines((string) arg.Path);
            var storage = new HashStorage<string, string>((int) arg.Pages, false);
            // foreach (var line in lines)
            // {
            //     if (string.IsNullOrWhiteSpace(line)) continue;
            //     storage.Store(line, line);
            // }

            int progress = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                storage.Store(line, line);
                var p = (int)((float) i / lines.Length * 50);
                if (p > progress)
                {
                    progress = p;
                    _worker.ReportProgress(progress);
                }
            }

            bucketRepresentations = new List<List<BucketRepresentation>>();

            for (int i = 0; i < storage.Array.Length; i++)
            {
                var reps = new List<BucketRepresentation>();

                var bucket = storage.Array[i];
                var counter = 0;
                var entry = bucket.First;

                while (entry != null)
                {
                    reps.Add(new BucketRepresentation
                    {
                        Bucket = i,
                        HashValue = (int) Hasher.Hash(entry.Key),
                        BucketIndex = counter++,
                        Word = entry.Value
                    });
                    entry = entry.Next;
                }

                bucketRepresentations.Add(reps);
                
                var p = 50 + (int) ((float) i / lines.Length * 50);
                if (p > progress)
                {
                    progress = p;
                    _worker.ReportProgress(progress);
                }
            }

            currentIndex = 0;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void PageCounter_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out var _);
        }

        private void PageCounter_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!int.TryParse(text, out var _)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PageCounter.Text, out var pageCount))
            {
                DialogHost.Show("Invalid Page Count");
                return;
            }

            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                LoadingIcon.Visibility = Visibility.Visible;
                PageControls.Visibility = Visibility.Hidden;
                DataGrid.Visibility = Visibility.Hidden;
                ProgressBar.Visibility = Visibility.Visible;
                _worker.RunWorkerAsync(new
                {
                    Path = ofd.FileName,
                    Pages = pageCount
                });
            }
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                if (currentIndex == 0)
                    PrevPageButton.IsEnabled = false;
                if (!NextPageButton.IsEnabled)
                    NextPageButton.IsEnabled = true;
                DataGrid.ItemsSource = bucketRepresentations[currentIndex];
                PageLabel.Content = $"{currentIndex+1}/{bucketRepresentations.Count}";
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex < bucketRepresentations.Count)
            {
                currentIndex++;
                if (currentIndex == bucketRepresentations.Count-1)
                    NextPageButton.IsEnabled = false;
                if (!PrevPageButton.IsEnabled)
                    PrevPageButton.IsEnabled = true;
                DataGrid.ItemsSource = bucketRepresentations[currentIndex];
                PageLabel.Content = $"{currentIndex+1}/{bucketRepresentations.Count}";
            }
        }

        public class BucketRepresentation
        {
            public int HashValue { get; set; }
            public int Bucket { get; set; }
            public int BucketIndex { get; set; }
            public string Word { get; set; } 
        }
    }
}