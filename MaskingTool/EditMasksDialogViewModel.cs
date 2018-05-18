using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MaskingTool
{
    /// <summary>
    /// マスク編集画面のビューモデル
    /// </summary>
    class EditMasksDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// マスクCSV ファイルを新規に保存するかを尋ねます。
        /// 保存するかどうかと、ファイル名を返します。
        /// </summary>
        public Func<(bool, string)> ConfirmNewSaveMaskCsvFile { get; set; }

        /// <summary>
        /// 画像描画箇所のサイズを取得します。
        /// </summary>
        public Func<Size> GetImageCanvasSize { get; set; }

        /// <summary>
        /// 現在編集中のマスク
        /// </summary>
        private Mask EditingMask { get; set; } = new Mask();

        /// <summary>
        /// マスクの一覧
        /// </summary>
        public ObservableCollection<Mask> Masks { get; } = new ObservableCollection<Mask>();

        /// <summary>
        /// 表示中の画像
        /// </summary>
        public ReactiveProperty<ImageSource> ImageSource { get; } = new ReactiveProperty<ImageSource>();

        /// <summary>
        /// ウィンドウタイトル
        /// </summary>
        public ReactiveProperty<string> WindowTitle { get; private set; }

        /// <summary>
        /// 開いている画像ファイルのパス
        /// </summary>
        public ReactiveProperty<string> MaskCsvFilePath { get; private set; } = new ReactiveProperty<string>();

        /// <summary>
        /// 開いているCSV ファイルのパス
        /// </summary>
        public string ImageFilePath { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EditMasksDialogViewModel()
        {
            this.CreateNewEditMask();

            this.WindowTitle = this.MaskCsvFilePath
                .Select(filePath =>
                {
                    var fileName = string.IsNullOrEmpty(filePath)
                        ? "無題"
                        : $"{System.IO.Path.GetFileName(filePath)}";

                    return $"{fileName} - マスク編集";
                })
                .ToReactiveProperty("");
        }

        /// <summary>
        /// マスクに新たな頂点を追加します。
        /// </summary>
        /// <param name="point"></param>
        public void AddNewVertex(Point point)
        {
            if (this.EditingMask == null) this.CreateNewEditMask();

            this.EditingMask.AddVertex(point);
        }

        /// <summary>
        /// 編集中のマスクを保存します。
        /// </summary>
        public void SaveEditingMask()
        {
            // マスクが3点以上で構成されるなら保存
            if ((this.EditingMask?.Points.Count ?? 0) > 2)
            {
                this.EditingMask.IsEditing.Value = false;
                this.CreateNewEditMask();
            }
        }

        /// <summary>
        /// 新たな編集用のマスクを作成します。
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private void CreateNewEditMask(params Point[] points)
        {
            var mask = new Mask(points);
            mask.IsEditing.Value = true;

            this.Masks.Add(mask);
            this.EditingMask = mask;
        }

        /// <summary>
        /// 編集中のマスクの頂点をすべて削除します。
        /// </summary>
        public void ClearEditingMask()
        {
            this.EditingMask.ClearVertex();
        }

        /// <summary>
        /// マスクCSV を上書き保存します。
        /// </summary>
        public void OverwriteMaskCsv()
        {
            if (!File.Exists(this.MaskCsvFilePath.Value))
            {
                this.SaveNewMaskCsv();
                return;
            }

            this.SaveMaskCsv(this.MaskCsvFilePath.Value);
        }

        /// <summary>
        /// マスクCSV を名前を付けて保存します。
        /// </summary>
        public void SaveNewMaskCsv()
        {
            var (allowed, filePath) = this.ConfirmNewSaveMaskCsvFile();
            if (!allowed) return;

            this.SaveMaskCsv(filePath);
            this.MaskCsvFilePath.Value = filePath;
        }

        /// <summary>
        /// 指定したファイルパスにマスク用のCSV を保存します。
        /// </summary>
        /// <param name="filePath"></param>
        private void SaveMaskCsv(string filePath)
        {
            var mat = new OpenCvSharp.Mat(this.ImageFilePath);
            var imageSize = this.GetImageCanvasSize();

            var rows = this.Masks.Select(mask => mask.ToCsv(mat, imageSize));

            File.WriteAllLines(filePath, rows);
            this.MaskCsvFilePath.Value = filePath;
        }

        /// <summary>
        /// 指定した画像ファイルを表示します。
        /// </summary>
        /// <param name="filePath">画像ファイルのパス</param>
        public void SetImage(string filePath)
        {
            if (!File.Exists(filePath)) return;

            this.ImageFilePath = filePath;

            var mat = new OpenCvSharp.Mat(filePath);
            var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                this.ImageSource.Value = bitmapimage;
            }
        }

        /// <summary>
        /// 指定したCSV ファイルに記録されているマスクを表示します。
        /// </summary>
        /// <param name="filePath"></param>
        public void SetMaskCsv(string filePath)
        {
            if (!File.Exists(filePath)) return;

            if (!File.Exists(ImageFilePath)) return;

            this.Masks.Clear();

            var image = new OpenCvSharp.Mat(this.ImageFilePath);
            var imageSize = this.GetImageCanvasSize();
            var csvRows = File.ReadAllLines(filePath);

            var masks = Mask.FromCsv(csvRows, image, imageSize);
            foreach (var mask in masks)
            {
                this.Masks.Add(mask);
            }
        }
    }
}
