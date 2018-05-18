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
        public static IEnumerable<Mask> FromCsv(IEnumerable<string> csvData, OpenCvSharp.Mat image, Size canvasSize)
        {
            return csvData
                .Where(row => !string.IsNullOrWhiteSpace(row))
                .Select(row =>
                {
                    var points = row.Trim().Split(',').Select(cell =>
                    {
                        var xy = cell.Trim().Split(' ').Select(c => double.Parse(c)).ToArray();
                        var x = xy[0] * canvasSize.Width / image.Width;
                        var y = xy[1] * canvasSize.Height / image.Height;

                        return new Point(x, y);
                    });

                    return new Mask(points);
                });
        }

        /// <summary>
        /// 頂点の一覧
        /// </summary>
        public ObservableCollection<Point> Points { get; } = new ObservableCollection<Point>();

        /// <summary>
        /// 編集中である場合はTrue、そうでなければFalse
        /// </summary>
        public ReactiveProperty<bool> IsEditing { get; } = new ReactiveProperty<bool>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Mask()
            : this(new Point[0])
        {
        }

        /// <summary>
        /// コンストラクタ
        /// 頂点を指定してインスタンスを初期化
        /// </summary>
        /// <param name="points"></param>
        public Mask(IEnumerable<Point> points)
        {
            foreach (var point in points)
            {
                this.Points.Add(point);
            }
        }

        /// <summary>
        /// 指定した頂点を追加します。
        /// </summary>
        /// <param name="point"></param>
        public void AddVertex(Point point)
        {
            this.Points.Add(point);
            this.PropertyChanged(this, new PropertyChangedEventArgs(nameof(this.Points)));
        }

        /// <summary>
        /// 頂点をすべて削除します。
        /// </summary>
        public void ClearVertex()
        {
            this.Points.Clear();
            this.PropertyChanged(this, new PropertyChangedEventArgs(nameof(this.Points)));
        }

        /// <summary>
        /// CSV データへ変換します。
        /// </summary>
        /// <param name="image"></param>
        /// <param name="canvasSize"></param>
        /// <returns></returns>
        public string ToCsv(OpenCvSharp.Mat image, Size canvasSize)
        {
            return string.Join(",", this.Points
                .Select(point =>
                {
                    var x = point.X * image.Width / canvasSize.Width;
                    var y = point.Y * image.Height / canvasSize.Height;

                    return $"{x} {y}";
                }));
        }
    }
}
