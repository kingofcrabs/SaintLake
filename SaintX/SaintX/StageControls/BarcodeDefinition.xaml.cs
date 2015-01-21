﻿using SaintX.Data;
using SaintX.Navigation;
using SaintX.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;

namespace SaintX.StageControls
{
    /// <summary>
    /// BarcodeDefinition.xaml 的交互逻辑
    /// </summary>
    public partial class BarcodeDefinition : BaseUserControl
    {
        SampleInfos _sampleInfos = null;
        ObservableCollection<ColorfulAssay> _assays;
        PanelViewModel panelVM;

        public SampleInfos SampleInfos
        {
            get { return this._sampleInfos; }
        }
        public BarcodeDefinition(Stage stage, BaseHost host)
            : base(stage, host)
        {
            InitializeComponent();
            _assays = SettingsManager.Instance.Assays;
            _sampleInfos = GlobalVars.Instance.SampleInfos;
            this.Loaded += BarcodeDefinition_Loaded;
            dataGridView.CellPainting += dataGridView_CellPainting;
            this.DataContext = this;
        }

        void BarcodeDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            InitTreeview(_assays.Select(x => x.Name).ToList());
            Helper.InitDataGridView(dataGridView,CurStage);
        }

        protected override void onStageChanged(object sender, EventArgs e)
        {
            base.onStageChanged(sender, e);
            Helper.InitDataGridView(dataGridView, CurStage);
            if(this.Visibility != System.Windows.Visibility.Visible)
                return;
            Helper.UpdateDataGridView(dataGridView,CurStage);
        }

        private void InitTreeview(List<string> assays)
        {
            panelVM = PanelViewModel.CreateViewModel(assays);
            tree.ItemsSource = new ObservableCollection<PanelViewModel>() { panelVM };
            this.tree.Focus();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NotifyFinished();
        }

        #region datagridview
        void dataGridView_CellPainting(object sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
        {
            
            if (e.ColumnIndex < 0)
                return;
            if (e.RowIndex == 0)
                return;
            //use default painting for first selected cell.
            if( dataGridView.SelectedCells.Count > 0)
            {
                var selectedCell = dataGridView.SelectedCells[0];
                if (selectedCell.RowIndex == e.RowIndex && selectedCell.ColumnIndex == e.ColumnIndex)
                    return;
            }
            System.Drawing.Rectangle newRect = new System.Drawing.Rectangle(e.CellBounds.X + 1,
                e.CellBounds.Y + 1, e.CellBounds.Width - 4,
                e.CellBounds.Height - 4);

            using (System.Drawing.Brush gridBrush = new System.Drawing.SolidBrush(this.dataGridView.GridColor),
                    backColorBrush = new System.Drawing.SolidBrush(e.CellStyle.BackColor))
            {
                using (System.Drawing.Pen gridLinePen = new System.Drawing.Pen(gridBrush))
                {
                    // Erase the cell.
                    e.Graphics.FillRectangle(backColorBrush, e.CellBounds);

                    // Draw the grid lines (only the right and bottom lines;
                    // DataGridView takes care of the others).
                    e.Graphics.DrawLine(gridLinePen, e.CellBounds.Left,
                        e.CellBounds.Bottom - 1, e.CellBounds.Right - 1,
                        e.CellBounds.Bottom - 1);
                    e.Graphics.DrawLine(gridLinePen, e.CellBounds.Right - 1,
                        e.CellBounds.Top, e.CellBounds.Right - 1,
                        e.CellBounds.Bottom);

                    // Draw the inset highlight box.
                        

                        //Draw the text content of the cell, ignoring alignment.
                    if (e.Value != null)
                    {
                        e.Graphics.DrawString((String)e.Value, e.CellStyle.Font,
                            System.Drawing.Brushes.Black, e.CellBounds.X + 2,
                            e.CellBounds.Y + 2, System.Drawing.StringFormat.GenericDefault);
                    }
                    e.Handled = true;
                }
            }
        }
        #endregion

        private void btnBarcodeOk_Click(object sender, RoutedEventArgs e)
        {
            // Might be a bit lengthy operation
            this.Cursor = System.Windows.Input.Cursors.Wait;

            // Clear all the barcode assignment of samples
            foreach (SampleInfo si in GlobalVars.Instance.SampleInfos.SampleInfoList)
                si.Barcode = string.Empty;
            Helper.UpdateDataGridView(dataGridView, Stage.BarcodeDef);

            int startBarCodeNum = 0;
            int endBarCodeNum = 0;

            // The bar code is assumed to be an integer
            if(rdbStartCount.IsChecked.Value)
            {
                startBarCodeNum = int.Parse(txtStartBarcodeApproach1.Text);
                int barCodeCount = int.Parse(txtCount.Text);
                endBarCodeNum = startBarCodeNum + barCodeCount - 1;
            }
            else
            {
                startBarCodeNum = int.Parse(txtStartBarcodeApproach2.Text);
                endBarCodeNum = int.Parse(txtEndBarcode.Text);
            }

            // Assays need to assign barcode
            PanelViewModel root_PVM = ((ObservableCollection<PanelViewModel>)tree.ItemsSource)[0];
            List<PanelViewModel> checked_PVMs = root_PVM.Children.FindAll(pvm => pvm.IsChecked.Value);
            List<string> checked_assays = new List<string>();
            foreach (var pvm in checked_PVMs)
                checked_assays.Add(pvm.Name);

            if (checked_assays.Count == 0)
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                return;
            }
                
            // Assign barcode to samples
            int barCode = startBarCodeNum;
            int sampleCount = GlobalVars.Instance.SampleInfos.SampleCount;
            int sampleAssignedBarcodeCount = 1;
            bool isDone = false;
            for (int col = 0; col < 6; ++col)
            {
                for (int row = 0; row < 16; ++row)
                {
                    if (sampleAssignedBarcodeCount <= sampleCount)
                    {
                        SampleInfo sampleInfo = GlobalVars.Instance.SampleInfos[row, col];
                        if (sampleInfo != null)
                        {
                            if (barCode <= endBarCodeNum)
                            {
                                if(checked_assays.Contains(sampleInfo.ColorfulAssay.Name))
                                {
                                    sampleInfo.Barcode = barCode++.ToString();
                                    ++sampleAssignedBarcodeCount;
                                }
                            }
                            else
                            {
                                // All avaliable barcodes have been used
                                isDone = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        isDone = true;
                        break;   // All samples have been assigned barcode
                    }
                }

                if (isDone)
                    break;
            }

            Helper.UpdateDataGridView(dataGridView,CurStage);
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void SetInfo(string s, Color color)
        {
            if (txtInfo == null)
                return;

            txtInfo.Background = new SolidColorBrush(Colors.White);
            txtInfo.Text = s;
            txtInfo.Foreground = new SolidColorBrush(color);
        }

        private void ClearInfo()
        {
            if (txtInfo == null)
                return;

            txtInfo.Background = new SolidColorBrush(Colors.White);
            txtInfo.Text = string.Empty;
        }

        #region Barcode assignment validation
        private void ValidateBarcodeSettingApproach1()
        {
            if (txtStartBarcodeApproach1 == null || txtCount == null)
                return;

            int startBarcode = -1, barcodeCnt = -1;
            if(!int.TryParse(txtStartBarcodeApproach1.Text, out startBarcode) || startBarcode <= 0 ||
               !int.TryParse(txtCount.Text, out barcodeCnt) || barcodeCnt <= 0)
            {
                SetInfo("起始条码的数值和数量的数值都必须是大于0的整数", Colors.Red);
                btnBarcodeOk.IsEnabled = false;
            }
            else
            {
                btnBarcodeOk.IsEnabled = true;
                ClearInfo();
            }
        }

        private void ValidateBarcodeSettingApproach2()
        {
            if (txtStartBarcodeApproach2 == null || txtEndBarcode == null)
                return;

            int startBarcode = -1, endBarcode = -1;
            if(!int.TryParse(txtStartBarcodeApproach2.Text, out startBarcode) || startBarcode <= 0 ||
               !int.TryParse(txtEndBarcode.Text, out endBarcode) || endBarcode <= 0)
            {
                SetInfo("起始条码的数值和结束条码的数值都必须是大于0的整数", Colors.Red);
                btnBarcodeOk.IsEnabled = false;
            }
            else
            {
                if(endBarcode <= startBarcode)
                {
                    SetInfo("结束条码的数值必须大于起始条码的数值", Colors.Red);
                    btnBarcodeOk.IsEnabled = false;
                }
                else
                {
                    btnBarcodeOk.IsEnabled = true;
                    ClearInfo();
                }
            }
        }

        private void txtStartBarcodeApproach1_LostFocus(object sender, RoutedEventArgs e)
        {
            if(rdbStartCount.IsChecked.Value)
            {
                ValidateBarcodeSettingApproach1();
            }
        }

        private void txtCount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (rdbStartCount.IsChecked.Value)
            {
                ValidateBarcodeSettingApproach1();
            }
        }

        private void txtStartBarcodeApproach2_LostFocus(object sender, RoutedEventArgs e)
        {
            if(rdbStartEnd.IsChecked.Value)
            {
                ValidateBarcodeSettingApproach2();
            }
        }

        private void txtEndBarcode_LostFocus(object sender, RoutedEventArgs e)
        {
            if (rdbStartEnd.IsChecked.Value)
            {
                ValidateBarcodeSettingApproach2();
            }
        }

        private void rdbStartCount_Checked(object sender, RoutedEventArgs e)
        {
            ValidateBarcodeSettingApproach1();
        }

        private void rdbStartEnd_Checked(object sender, RoutedEventArgs e)
        {
            ValidateBarcodeSettingApproach2();
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.LightYellow);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
