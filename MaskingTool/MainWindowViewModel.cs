using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace MaskingTool
{
    /// <summary>
    /// メインウィンドウのビューモデル
    /// </summary>
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 元となる画像フォルダーのパス
        /// </summary>
        public ReactiveProperty<string> SrcFolderPath { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// マスク処理後の画像を出力するフォルダーのパス
        /// </summary>
        public ReactiveProperty<string> DistFolderPath { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// ファイルを検索するためのパターン文字列
        /// </summary>
        public ReactiveProperty<string> FileNamePattern { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// マスク情報が記録されたCSV ファイルのパス
        /// </summary>
        public ReactiveProperty<string> MaskCsvFilePath { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// 出力先のフォルダーをユーザーに確認します。
        /// 承諾された場合にはTrue、そうでなければFalse を取得します。
        /// </summary>
        public Func<bool> ConfirmDistFolder { get; set; }

        /// <summary>
        /// マスキング処理が実行可能であるかを検査します。
        /// 実行可能である場合はTrue、そうでなければFalse を取得します。
        /// </summary>
        /// <returns></returns>
        public bool IsExecutable()
        {
            if (string.IsNullOrEmpty(this.SrcFolderPath.Value)
                || !Directory.Exists(this.SrcFolderPath.Value))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.DistFolderPath.Value)
                || !Directory.Exists(this.DistFolderPath.Value))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.FileNamePattern.Value))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.MaskCsvFilePath.Value)
                || !File.Exists(this.MaskCsvFilePath.Value))
            {
                return false;
            }

            // 入力フォルダーと出力フォルダーが同一の場合は確認する
            if (this.SrcFolderPath.Value == this.DistFolderPath.Value
                && !(this.ConfirmDistFolder?.Invoke() ?? true))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// マスキング処理を実行します。
        /// </summary>
        /// <param name="progress"></param>
        public void ExecuteMasking(IProgress<ProgressParameter> progress)
        {
            var csv = File.ReadAllText(this.MaskCsvFilePath.Value);
            var csvData = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(row => row.Split(','));

            var srcFilePathList = Directory
                .EnumerateFiles(this.SrcFolderPath.Value, this.FileNamePattern.Value)
                .ToArray();

            progress.Report(new ProgressParameter(srcFilePathList.Length, 0));

            var count = 0;
            foreach (var srcFilePath in srcFilePathList)
            {
                var distFilePath = Path.Combine(this.DistFolderPath.Value, Path.GetFileName(srcFilePath));
                new MaskingTask(srcFilePath, distFilePath, csvData).Execute();

                count++;

                progress.Report(new ProgressParameter(srcFilePathList.Length, count));
            }
        }

        /// <summary>
        /// マスキング処理のプレビューを表示します。
        /// </summary>
        public void ShowPreview()
        {
            if (!this.IsExecutable()) return;

            var srcFilePath = Directory
                .EnumerateFiles(this.SrcFolderPath.Value, this.FileNamePattern.Value)
                .FirstOrDefault();

            var csv = File.ReadAllText(this.MaskCsvFilePath.Value);
            var csvData = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(row => row.Split(','));

            new MaskingTask(srcFilePath, "", csvData).ShowPreview();
        }
    }
}
