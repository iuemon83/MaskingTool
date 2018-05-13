using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MaskingTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ビューモデル
        /// </summary>
        private MainWindowViewModel viewmodel;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
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
            this.viewmodel = new MainWindowViewModel()
            {
                ConfirmDistFolder = this.ConfirmDistFolder
            };
            this.DataContext = this.viewmodel;
        }

        /// <summary>
        /// 選択されている入力フォルダーと出力フォルダーが同一の場合に、処理を継続するかどうかを確認します。
        /// 処理を継続する場合はTrue、そうでなければFalse を取得します。
        /// </summary>
        /// <returns></returns>
        private bool ConfirmDistFolder()
        {
            var dialogMessage = "選択されている入力フォルダーと出力フォルダーが同一です。処理を継続してよろしいですか？";
            var dialogResult = MessageBox.Show(dialogMessage, "確認", MessageBoxButton.YesNo);

            return dialogResult == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 実行ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var isExecutable = this.viewmodel.IsExecutable();
            if (!isExecutable)
            {
                MessageBox.Show("パラメータが正しくありません。", "エラー");
                return;
            }

            this.ExecuteButton.IsEnabled = false;

            var progress = new Progress<ProgressParameter>(p =>
            {
                this.ProgressBar.Minimum = 0;
                this.ProgressBar.Maximum = p.Max;
                this.ProgressBar.Value = p.Current;
                this.ProgressMax.Text = p.Max.ToString();
                this.ProgressCurrent.Text = p.Current.ToString();
            });

            await Task.Run(() =>
            {
                this.viewmodel.ExecuteMasking(progress);
            });

            this.ExecuteButton.IsEnabled = true;
        }

        /// <summary>
        /// 入力フォルダーの選択ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiseSrcFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                DefaultFileName = this.viewmodel.SrcFolderPath.Value
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.viewmodel.SrcFolderPath.Value = dialog.FileName;
            }
        }

        /// <summary>
        /// 出力フォルダーの選択ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiseDistFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                DefaultFileName = this.viewmodel.DistFolderPath.Value
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.viewmodel.DistFolderPath.Value = dialog.FileName;
            }
        }

        /// <summary>
        /// マスクCSVファイルの選択ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiseMaskCsvFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = false,
                DefaultFileName = this.viewmodel.MaskCsvFilePath.Value
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.viewmodel.MaskCsvFilePath.Value = dialog.FileName;
            }
        }

        /// <summary>
        /// プレビューボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewmodel.ShowPreview();
        }

        /// <summary>
        /// マスク編集画面表示ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditMaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new EditMasksDialog()
            {
                Owner = this
            }
            .ShowDialog();
        }
    }
}
