using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace MaskingTool
{
    /// <summary>
    /// SettingMasksDialog.xaml の相互作用ロジック
    /// マスキング箇所の設定画面
    /// </summary>
    public partial class EditMasksDialog : Window
    {
        /// <summary>
        /// ビューモデル
        /// </summary>
        private readonly EditMasksDialogViewModel viewmodel = new EditMasksDialogViewModel()
        {
            ConfirmNewSaveMaskCsvFile = () =>
            {
                var dialog = new CommonSaveFileDialog()
                {
                    Title = "名前を付けて保存",
                    DefaultFileName = "mask.csv"
                };

                return (dialog.ShowDialog() == CommonFileDialogResult.Ok, dialog.FileName);
            },
        };

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
            this.viewmodel.GetImageCanvasSize = () =>
            {
                return new Size(this.image.ActualWidth, this.image.ActualHeight);
            };

            this.DataContext = this.viewmodel;
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
                var mousePosition = e.GetPosition(this.maskCanvas);

                this.viewmodel.AddNewVertex(mousePosition);
            }
            else
            {
                this.viewmodel.SaveEditingMask();
            }
        }

        /// <summary>
        /// マスク描画用キャンバスの右クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.viewmodel.ClearEditingMask();
        }

        /// <summary>
        /// マスクCSV 上書き保存ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverwriteCsvButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewmodel.OverwriteMaskCsv();
        }

        /// <summary>
        /// マスクCSV 名前を付けて保存ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveNewCsvButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewmodel.SaveNewMaskCsv();
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
                DefaultFileName = this.viewmodel.ImageFilePath
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.viewmodel.SetImage(dialog.FileName);
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
                DefaultFileName = this.viewmodel.MaskCsvFilePath.Value
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.viewmodel.SetMaskCsv(dialog.FileName);
            }
        }
    }
}
