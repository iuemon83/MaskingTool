using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MaskingTool
{
    /// <summary>
    /// マスクの頂点コレクションのコンバーター
    /// </summary>
    class MaskPointsConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter
                              , System.Globalization.CultureInfo culture)
        {
            if (value is ObservableCollection<Point> points
                && targetType == typeof(PointCollection))
            {

                var pointCollection = new PointCollection();
                foreach (var point in points)
                {
                    pointCollection.Add(point);
                }

                return pointCollection;
            }
            return null;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter
                                  , System.Globalization.CultureInfo culture)
        {
            return null; // 不要
        }
    }
}
