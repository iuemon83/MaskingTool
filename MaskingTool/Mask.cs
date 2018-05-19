using Reactive.Bindings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MaskingTool
{
    /// <summary>
    /// マスク
    /// </summary>
    class Mask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// CSV データからマスクを作成します。
        /// </summary>
        /// <param name="csvData"></param>
        /// <param name="image"></param>
        /// <param name="canvasSize"></param>
        /// <returns></returns>
        public static IEnumerable<Mask> FromCsv(IEnumerable<string> csvData, Size imageSize, Size canvasSize)
        {
            return csvData
                .Where(row => !string.IsNullOrWhiteSpace(row))
                .Select(row =>
                {
                    var points = row.Trim().Split(',').Select(cell =>
                    {
                        var xy = cell.Trim().Split(' ').Select(c => double.Parse(c)).ToArray();
                        return new Point(xy[0], xy[1]);
                    });

                    return new Mask(points, imageSize, canvasSize);
                });
        }

        /// <summary>
        /// ピクセル座標からキャンバス座標へ変換します。
        /// </summary>
        /// <param name="pixelPoint">ピクセル座標</param>
        /// <param name="imageSize">画像のサイズ</param>
        /// <param name="canvasSize">キャンバスのサイズ</param>
        /// <returns></returns>
        public static Point PixelToCanvasPoint(Point pixelPoint, Size imageSize, Size canvasSize)
        {
            return new Point(
                pixelPoint.X * canvasSize.Width / imageSize.Width,
                pixelPoint.Y * canvasSize.Height / imageSize.Height
                );
        }

        /// <summary>
        /// キャンバス座標からピクセル座標へ変換します。
        /// </summary>
        /// <param name="canvasPoint">キャンバス座標</param>
        /// <param name="imageSize">画像のサイズ</param>
        /// <param name="canvasSize">キャンバスのサイズ</param>
        /// <returns></returns>
        public static Point CanvasToPixelPoint(Point canvasPoint, Size imageSize, Size canvasSize)
        {
            return new Point(
                canvasPoint.X * imageSize.Width / canvasSize.Width,
                canvasPoint.Y * imageSize.Height / canvasSize.Height
                );
        }

        /// <summary>
        /// ピクセル上での頂点の一覧
        /// </summary>
        private List<Point> PixelPoints { get; } = new List<Point>();

        /// <summary>
        /// キャンバス上での頂点の一覧
        /// </summary>
        public ObservableCollection<Point> CanvasPoints { get; } = new ObservableCollection<Point>();

        /// <summary>
        /// 編集中である場合はTrue、そうでなければFalse
        /// </summary>
        public ReactiveProperty<bool> IsEditing { get; } = new ReactiveProperty<bool>();

        /// <summary>
        /// キャンバスのサイズ
        /// </summary>
        private Size canvasSize;

        /// <summary>
        /// キャンバスのサイズ
        /// </summary>
        public Size CanvasSize
        {
            get { return this.canvasSize; }
            set
            {
                if (this.canvasSize != value)
                {
                    this.canvasSize = value;

                    this.CanvasPoints.Clear();
                    foreach (var p in this.PixelPoints)
                    {
                        this.CanvasPoints.Add(PixelToCanvasPoint(p, this.ImageSize, this.CanvasSize));
                    }
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CanvasPoints)));
                }
            }
        }

        /// <summary>
        /// 画像のサイズ
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// ジオメトリとして成立する形状の場合はTrue、そうでなければFalse を取得します。
        /// </summary>
        public bool IsValidGeometry => this.PixelPoints.Count > 2;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Mask(Size imageSize, Size canvasSize)
            : this(new Point[0], imageSize, canvasSize)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// 頂点を指定してインスタンスを初期化
        /// </summary>
        /// <param name="pixelPoints"></param>
        public Mask(IEnumerable<Point> pixelPoints, Size imageSize, Size canvasSize)
        {
            this.ImageSize = imageSize;
            this.CanvasSize = canvasSize;

            foreach (var point in pixelPoints)
            {
                this.PixelPoints.Add(point);
                this.CanvasPoints.Add(PixelToCanvasPoint(point, this.ImageSize, this.CanvasSize));
            }
        }

        /// <summary>
        /// 指定した頂点を追加します。
        /// </summary>
        /// <param name="pixelPoint"></param>
        public void AddVertex(Point pixelPoint)
        {
            this.PixelPoints.Add(pixelPoint);
            this.CanvasPoints.Add(PixelToCanvasPoint(pixelPoint, this.ImageSize, this.CanvasSize));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CanvasPoints)));
        }

        /// <summary>
        /// 頂点をすべて削除します。
        /// </summary>
        public void ClearVertex()
        {
            this.PixelPoints.Clear();
            this.CanvasPoints.Clear();
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CanvasPoints)));
        }

        /// <summary>
        /// CSV データへ変換します。
        /// </summary>
        /// <param name="image"></param>
        /// <param name="canvasSize"></param>
        /// <returns></returns>
        public string ToCsv(OpenCvSharp.Mat image, Size canvasSize)
        {
            return string.Join(",", this.PixelPoints
                .Select(point => $"{point.X} {point.Y}"));
        }
    }
}
