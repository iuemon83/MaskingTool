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
        private readonly Func<(bool, string)> confirmNewSaveMaskCsvFile;

        /// <summary>
        /// 画像描画箇所のサイズを取得します。
        /// </summary>
        private readonly Func<Size> getImageCanvasSize;

        /// <summary>
        /// 現在編集中のマスク
        /// </summary>
        private Mask EditingMask;

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
        public ReactiveProperty<string> ImageFilePath = new ReactiveProperty<string>();

        /// <summary>
        /// キャンバスサイズのキャッシュ
        /// </summary>
        private Lazy<Size> canvasSizeCache;

        /// <summary>
        /// 画像サイズのキャッシュ
        /// </summary>
        private readonly ReactiveProperty<Size> imageSizeCache;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EditMasksDialogViewModel(Func<Size> getImageCanvasSize, Func<(bool, string)> confirmNewSaveMaskCsvFile)
        {
            this.getImageCanvasSize = getImageCanvasSize;
            this.confirmNewSaveMaskCsvFile = confirmNewSaveMaskCsvFile;

            this.UpdateCanvasSize();

            this.imageSizeCache = this.ImageFilePath
                .Select(filePath =>
                {
                    Size result;
                    if (File.Exists(filePath))
                    {
                        using (var mat = new OpenCvSharp.Mat(filePath))
                        {
                            result = new Size(mat.Width, mat.Height);
                        }
                    }
                    else
                    {
                        result = new Size();
                    }

                    foreach (var mask in this.Masks)
                    {
                        mask.ImageSize = result;
                    }

                    return result;
                })
                .ToReactiveProperty();

            this.WindowTitle = this.MaskCsvFilePath
                .Select(filePath =>
                {
                    var fileName = string.IsNullOrEmpty(filePath)
                        ? "無題"
                        : $"{System.IO.Path.GetFileName(filePath)}";

                    return $"{fileName} - マスク編集";
                })
                .ToReactiveProperty("");

            this.CreateNewEditMask();
        }

        /// <summary>
        /// マスクに新たな頂点を追加します。
        /// </summary>
        /// <param name="canvasPoint"></param>
        public void AddNewVertex(Point canvasPoint)
        {
            if (this.EditingMask == null) this.CreateNewEditMask();

            var pixelPoint = Mask.CanvasToPixelPoint(canvasPoint, this.imageSizeCache.Value, this.canvasSizeCache.Value);
            this.EditingMask.AddVertex(pixelPoint);
        }

        /// <summary>
        /// 編集中のマスクを保存します。
        /// </summary>
        public void SaveEditingMask()
        {
            // マスクが3点以上で構成されるなら保存
            if (this.EditingMask?.IsValidGeometry ?? false)
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
            var mask = new Mask(points, this.imageSizeCache.Value, this.canvasSizeCache.Value);
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
            var (allowed, filePath) = this.confirmNewSaveMaskCsvFile();
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
            var canvasSize = this.canvasSizeCache.Value;

            var rows = this.Masks.Select(mask => mask.ToCsv(canvasSize));

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

            this.ImageFilePath.Value = filePath;

            using (var mat = new OpenCvSharp.Mat(filePath))
            using (var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))
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

            if (!File.Exists(this.ImageFilePath.Value)) return;

            this.MaskCsvFilePath.Value = filePath;
            this.Masks.Clear();

            var imageSize = this.imageSizeCache.Value;
            var canvasSize = this.canvasSizeCache.Value;
            var csvRows = File.ReadAllLines(filePath);

            var masks = Mask.FromCsv(csvRows, imageSize, canvasSize);
            foreach (var mask in masks)
            {
                this.Masks.Add(mask);
            }
        }

        /// <summary>
        /// キャンバスサイズを更新します。
        /// </summary>
        public void UpdateCanvasSize()
        {
            this.canvasSizeCache = new Lazy<Size>(() => this.getImageCanvasSize?.Invoke() ?? default);

            foreach (var mask in this.Masks)
            {
                mask.CanvasSize = this.canvasSizeCache.Value;
            }
        }
    }
}
