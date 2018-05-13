using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MaskingTool
{
    /// <summary>
    /// SettingMasksDialog.xaml の相互作用ロジック
    /// マスキング箇所の設定画面
    /// </summary>
    public partial class EditMasksDialog : Window
    {
        /// <summary>
        /// 現在編集中のマスク
        /// </summary>
        private Polygon currentMask = new Polygon();

        /// <summary>
        /// マスクの一覧
        /// </summary>
        private List<Polygon> masks = new List<Polygon>();

        /// <summary>
        /// 開いている画像ファイルのパス
        /// </summary>
        private string imageFilePath;

        /// <summary>
        /// 開いているCSV ファイルのパス
        /// </summary>
        private string maskCsvFilePath;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EditMasksDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウィンドウのロードイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.currentMask = this.CreateNewMask();
        }

        /// <summary>
        /// マスク描画用キャンバスの左クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (this.currentMask == null) this.currentMask = this.CreateNewMask();

                var mousePosition = e.GetPosition(this.maskCanvas);
                this.currentMask.Points.Add(mousePosition);
            }
            else
            {
                // マスクが3点以上で構成されるなら保存
                if ((this.currentMask?.Points.Count ?? 0) > 2)
                {
                    this.masks.Add(this.currentMask);

                    this.currentMask = this.CreateNewMask();
                }
            }
        }

        /// <summary>
        /// マスク描画用キャンバスの右クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.currentMask?.Points?.Clear();
        }

        /// <summary>
        /// キャンセルボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 保存ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var mat = new OpenCvSharp.Mat(this.imageFilePath);

            var rows = this.masks
                .Select(mask =>
                {
                    return mask.Points
                        .Select(p => new Point(p.X * mat.Width / this.image.ActualWidth, p.Y * mat.Height / this.image.ActualHeight));
                })
                .Select(points => string.Join(",", points.Select(p => $"{p.X} {p.Y}")));

            File.WriteAllLines("mask.csv", rows);

            this.Close();
        }

        /// <summary>
        /// 画像ファイルの選択ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiseImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = false,
                DefaultFileName = this.imageFilePath
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.imageFilePath = dialog.FileName;
                this.SetImage(this.imageFilePath);
            }
        }

        /// <summary>
        /// マスクCSV ファイルの選択ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiseCsvMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = false,
                DefaultFileName = this.maskCsvFilePath
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.maskCsvFilePath = dialog.FileName;
                this.SetMaskCsv(this.maskCsvFilePath);
            }
        }

        /// <summary>
        /// 指定した画像ファイルを表示します。
        /// </summary>
        /// <param name="filePath">画像ファイルのパス</param>
        private void SetImage(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var mat = new OpenCvSharp.Mat(filePath);
            var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                this.image.Source = bitmapimage;
            }
        }

        /// <summary>
        /// 指定したCSV ファイルに記録されているマスクを表示します。
        /// </summary>
        /// <param name="filePath"></param>
        private void SetMaskCsv(string filePath)
        {
            if (!File.Exists(filePath)) return;

            this.maskCanvas.Children.Clear();
            this.masks.Clear();

            var csvRows = File.ReadAllLines(filePath);
            var masksPoints = csvRows
                .Select(row =>
                {
                    return row.Split(',').Select(cell =>
                    {
                        var xy = cell.Trim().Split(' ').Select(c => double.Parse(c)).ToArray();
                        return new Point(xy[0], xy[1]);
                    });
                });

            if (File.Exists(this.imageFilePath))
            {
                var imageFile = new OpenCvSharp.Mat(this.imageFilePath);

                masksPoints = masksPoints
                    .Select(points => points.Select(p => new Point(p.X * this.image.ActualWidth / imageFile.Width, p.Y * this.image.ActualHeight / imageFile.Height)));
            }

            foreach (var points in masksPoints)
            {
                var mask = this.CreateNewMask(points.ToArray());
                this.masks.Add(mask);
            }
        }

        /// <summary>
        /// 新たなマスクを作成します。
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private Polygon CreateNewMask(params Point[] points)
        {
            var mask = new Polygon()
            {
                Fill = Brushes.Black,
                Points = new PointCollection(points)
            };

            this.maskCanvas.Children.Add(mask);

            return mask;
        }
    }
}
