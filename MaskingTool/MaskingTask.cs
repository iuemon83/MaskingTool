using OpenCvSharp;
using System.Collections.Generic;
using System.Linq;

namespace MaskingTool
{
    /// <summary>
    /// マスキング処理
    /// </summary>
    class MaskingTask
    {
        /// <summary>
        /// 入力フォルダーのパス
        /// </summary>
        private readonly string srcFilePath;

        /// <summary>
        /// 出力フォルダーのパス
        /// </summary>
        private readonly string distFilePath;

        /// <summary>
        /// マスキング範囲を表す
        /// </summary>
        private readonly string[][] maskListCsv;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="distFilePath"></param>
        /// <param name="maskListCsv"></param>
        public MaskingTask(string srcFilePath, string distFilePath, IEnumerable<IEnumerable<string>> maskListCsv)
        {
            this.srcFilePath = srcFilePath;
            this.distFilePath = distFilePath;
            this.maskListCsv = maskListCsv.Select(row => row.ToArray()).ToArray();
        }

        /// <summary>
        /// マスキング処理を実行します。
        /// </summary>
        public void Execute()
        {
            using (var src = new Mat(this.srcFilePath))
            {
                // マスキング
                var maskList = this.maskListCsv
                    .Select(row => row.Select(column =>
                    {
                        var xy = column.Split(' ').Select(s => double.Parse(s)).ToArray();
                        return new Point(xy[0], xy[1]);
                    }));

                Cv2.FillPoly(src, maskList, new Scalar(255, 0, 0));
                Cv2.ImWrite(this.distFilePath, src);
            }
        }

        /// <summary>
        /// マスキングのプレビューを表示します。
        /// </summary>
        public void ShowPreview()
        {
            using (var src = new Mat(this.srcFilePath))
            {
                // マスキング
                var maskList = this.maskListCsv
                    .Select(row => row.Select(column =>
                    {
                        var xy = column.Split(' ').Select(s => double.Parse(s)).ToArray();
                        return new Point(xy[0], xy[1]);
                    }));

                Cv2.FillPoly(src, maskList, new Scalar(255, 0, 0));
                Cv2.ImShow("プレビュー", src);

                // キー入力があるまで待機
                Cv2.WaitKey();

                // 明示的に全画像の破棄を指示
                Cv2.DestroyAllWindows();
            }
        }
    }
}
