using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        public DataMode CurrentData;
        public HashStorage<Address> Storage;
        public PageTable<string> PageTable;
        
        public List<List<PageRepresentation>> Pages 
            = new List<List<PageRepresentation>>();

        private readonly BackgroundWorker _worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            _worker.DoWork += WorkerOnDoWork;
            _worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += WorkerOnProgressChanged;

            DataGrid.Visibility = Visibility.Hidden;
            StatisticsPanel.Visibility = Visibility.Hidden;
            
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var hashResult = (HashResult) e.Result;
            LoadingIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            DataGrid.Visibility = Visibility.Visible;
            StatisticsPanel.Visibility = Visibility.Visible;
            PageDataGrid.ItemsSource = hashResult.PageRepresentations;
            BucketDataGrid.ItemsSource = hashResult.BucketRepresentations;
            Storage = hashResult.Storage;
            PageTable = hashResult.PageTable;
            OverflowLabel.Content = $"{hashResult.overflowPct:0.######}%";
            CollisionLabel.Content = $"{hashResult.collisionsPct:0.######}%";
            AccessLabel.Content = $"{hashResult.avgAccess:#.##}";
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var hashargs = (HashArgs)e.Argument;

            var lines = File.ReadLines(hashargs.Path).ToArray();

            int pageCount;
            int pageSize;
            int progress = 0;

            if (hashargs.Choice == HashArgs.InputChoice.PageCount)
            {
                pageCount = hashargs.Input;
                pageSize = (int) Math.Ceiling((decimal) ((float)lines.Length / pageCount));
            }
            else
            {
                pageSize = hashargs.Input;
                pageCount = (int) Math.Ceiling((decimal) ((float)lines.Length / pageSize));
            }
            
            var pageTable = new PageTable<string>(pageCount, pageSize);
            var hashStorage = new HashStorage<Address>(hashargs.BucketCount, hashargs.BucketSize);

            for (int i = 0; i < pageTable.Pages.Length; i++)
                Pages.Add(new List<PageRepresentation>());

            var pageRepresentations = new List<PageRepresentation>();
            var bucketRepresentations = new List<BucketRepresentation>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var p = i*100 / lines.Length;
                if (p > progress)
                {
                    progress = p;
                    _worker.ReportProgress(progress);
                }
                var address = pageTable.Insert(lines[i]);



                var rep = new PageRepresentation
                {
                    Page = address.Page + 1,
                    Line = address.Line + 1,
                    Index = i,
                    Text = lines[i]
                };
                
                Pages[address.Page].Add(rep);

                pageRepresentations.Add(rep);
                hashStorage.Insert(lines[i], address);
            }

            var buckets = hashStorage.Buckets;

            string buildContent(HashStorage<Address>.Entry[] entries)
            {
                var res = "[";

                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null)
                    {
                        res += $"{entries[i].key}-P{entries[i].value.Page+1}L{entries[i].value.Line+1}";
                    }
                    
                    if (i != entries.Length - 1)
                        res += ",";
                }

                return res + "]";
            }

            for (int i = 0; i < buckets.Length; i++)
            {
                int counter = 0;
                var bucket = buckets[i];
                while (bucket != null)
                {
                    bucketRepresentations.Add(new BucketRepresentation
                    {
                        BucketID = $"{i}-{counter}",
                        Content = buildContent(bucket.Entries),
                        Overflow = bucket.Next != null ? $"{i}-{counter+1}": ""
                    });
                    bucket = bucket.Next;
                    counter++;
                }
            }

            var collisionPct = (float)hashStorage.collisionCount * 100 / lines.Length;
            var overflowPct = (float)hashStorage.Buckets.Aggregate(0, (i, b) =>
           {
               if (b.Next != null)
                   return i + 1;
               return i;
           }) * 100 / hashStorage.Buckets.Length;

            var usedBuckets = hashStorage.Buckets.Where(b => !b.Empty).ToArray();
            var avgAccess = (float) usedBuckets
                .Sum(b => b.Count()) / usedBuckets.Count();

            e.Result = new HashResult
            {
                PageRepresentations = pageRepresentations,
                BucketRepresentations = bucketRepresentations,
                Storage = hashStorage,
                PageTable = pageTable,
                collisionsPct = collisionPct,
                overflowPct = overflowPct,
                avgAccess = avgAccess
            };
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Numerical_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out var _);
        }

        private void Numerical_Pasting(object sender, DataObjectPastingEventArgs e)
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
            if (!int.TryParse(PageCounter.Text, out var pageInput) ||
                !int.TryParse(BucketCount.Text, out var bucketCount) ||
                !int.TryParse(BucketSize.Text, out var bucketSize))
            {
                return;
            }
            
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                LoadingIcon.Visibility = Visibility.Visible;
                DataGrid.Visibility = Visibility.Hidden;
                StatisticsPanel.Visibility = Visibility.Hidden;
                ProgressBar.Visibility = Visibility.Visible;
                _worker.RunWorkerAsync(new HashArgs
                {
                    BucketCount = bucketCount,
                    BucketSize = bucketSize,
                    Choice = HashModeComboBox.SelectedIndex == 0 ? HashArgs.InputChoice.PageCount : HashArgs.InputChoice.PageSize,
                    Input = pageInput,
                    Path = ofd.FileName
                });
            }
        }

        public class PageRepresentation
        {
            public int Index { get; set; }
            public int Page { get; set; }
            public int Line { get; set; }
            public string Text { get; set; }
        }

        public class BucketRepresentation
        {
            public string BucketID { get; set; }
            public string Content { get; set; }
            public string Overflow { get; set; }
        }

        public enum DataMode { Pages, Bucket }
        
        public class HashArgs
        {
            public enum InputChoice
            {
                PageSize,
                PageCount
            }

            public string Path;
            public InputChoice Choice;
            public int Input;
            public int BucketCount;
            public int BucketSize;
        }

        public class HashResult
        {
            public List<PageRepresentation> PageRepresentations;
            public List<BucketRepresentation> BucketRepresentations;
            public HashStorage<Address> Storage;
            public PageTable<string> PageTable;
            public float collisionsPct;
            public float overflowPct;
            public float avgAccess;
        }

        private void ToPages_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentData == DataMode.Pages)
                return;

            CurrentData = DataMode.Pages;
            Transitioner.SelectedIndex = 0;
        }

        private void ToBuckets_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentData == DataMode.Bucket)
                return;

            CurrentData = DataMode.Bucket;
            Transitioner.SelectedIndex = 1;
        }

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            Address address;

            try
            {
                address = Storage.Retrieve(IndexSearch.Text);
            }
            catch (KeyNotFoundException knfe)
            {
                return;
            }

            if (CurrentData != DataMode.Pages)
            {
                CurrentData = DataMode.Pages;
                Transitioner.SelectedIndex = 0;
            }

            // var item = ((List<PageRepresentation>)PageDataGrid.ItemsSource).FirstOrDefault(i => (i.Page == address.Page + 1 && i.Line == address.Line + 1));
            // PageDataGrid.ScrollIntoView(item);

            var page = Pages[address.Page]; //De indice 1 pra indice 0
            foreach (var word in page)
            {
                if (String.Equals(word.Text, IndexSearch.Text, StringComparison.CurrentCultureIgnoreCase))
                    PageDataGrid.ScrollIntoView(word);
            }
        }
    }
}