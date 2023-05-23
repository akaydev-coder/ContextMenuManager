using BluePointLilac.Methods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class BackupDialog : CommonDialog
    {
        public string Title { get; set; }

        public string CmbTitle { get; set; }
        public string[] CmbItems { get; set; }
        public int CmbSelectedIndex { get; set; }
        public string CmbSelectedText { get; set; }

        public string DgvTitle { get; set; }
        public string[] DgvItems { get; set; }
        public List<string> DgvSelectedItems { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(SelectForm frm = new SelectForm())
            {
                frm.Text = Title;
                frm.CmbTitle = CmbTitle;
                frm.CmbItems = CmbItems;
                frm.DgvTitle = DgvTitle;
                frm.DgvItems = DgvItems;
                if (CmbSelectedText != null) frm.CmbSelectedText = CmbSelectedText;
                else frm.CmbSelectedIndex = CmbSelectedIndex;
                Form owner = (Form)Control.FromHandle(hwndOwner);
                if(owner != null) frm.TopMost = owner.TopMost;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if(flag)
                {
                    CmbSelectedText = frm.CmbSelectedText;
                    CmbSelectedIndex = frm.CmbSelectedIndex;
                    DgvSelectedItems = frm.DgvSelectedItems;
                }
                return flag;
            }
        }

        sealed class SelectForm : Form
        {
            /*************************************外部函数***********************************/

            public SelectForm()
            {
                SuspendLayout();
                AcceptButton = btnOK;
                CancelButton = btnCancel;
                Font = SystemFonts.MenuFont;
                ShowIcon = ShowInTaskbar = false;
                MaximizeBox = MinimizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                StartPosition = FormStartPosition.CenterParent;
                InitializeComponents();
                ResumeLayout();
            }

            /*************************************外部属性***********************************/

            public string CmbTitle
            {
                get => cmbInfo.Text;
                set {
                    cmbInfo.Text = value;
                    cmbItems.Left = cmbInfo.Right;
                    cmbItems.Width -= cmbInfo.Width;
                }
            }
            public string[] CmbItems
            {
                get
                {
                    string[] value = new string[cmbItems.Items.Count];
                    cmbItems.Items.CopyTo(value, 0);
                    return value;
                }
                set
                {
                    cmbItems.Items.Clear();
                    cmbItems.Items.AddRange(value);
                }
            }
            // cmb选中项目索引
            public int CmbSelectedIndex
            {
                get => cmbItems.SelectedIndex;
                set => cmbItems.SelectedIndex = value;
            }
            // cmb选中项目内容
            public string CmbSelectedText
            {
                get => cmbItems.Text;
                set => cmbItems.Text = value;
            }

            public string DgvTitle
            {
                get => dgvInfo.Text;
                set => dgvInfo.Text = value;
            }
            private string[] dgvItemsValue;
            public string[] DgvItems
            {
                get
                {
                    return dgvItemsValue;
                }
                set
                {
                    dgvItemsValue = value;
                    ShowDgvList(value);
                }
            }
            // dgv选中的项目
            private readonly List<string> dgvSelectedItems = new List<string>();
            public List<string> DgvSelectedItems
            {
                get => dgvSelectedItems;
            }

            /*************************************内部控件***********************************/

            readonly Label dgvInfo = new Label { AutoSize = true };
            readonly DataGridView dgvItems = new DataGridView
            {
                ColumnHeadersVisible = false,   // 隐藏列标题
                RowHeadersVisible = false,  // 隐藏行标题
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // 整行一起选中
                BackgroundColor = SystemColors.Control,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                MultiSelect = false,
                ReadOnly = true,
            };

            readonly Label cmbInfo = new Label { AutoSize = true };
            readonly ComboBox cmbItems = new ComboBox
            {
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DropDownHeight = 300.DpiZoom(),
                DropDownStyle = ComboBoxStyle.DropDownList, // 用户不可增加新项目
                ImeMode = ImeMode.Disable
            };

            readonly Button btnOK = new Button
            {
                DialogResult = DialogResult.OK,
                Text = ResourceString.OK,
                AutoSize = true
            };
            readonly Button btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = ResourceString.Cancel,
                AutoSize = true
            };

            /*************************************内部函数***********************************/

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { dgvInfo, dgvItems, cmbInfo, cmbItems, btnOK, btnCancel });
                int margin = 20.DpiZoom();
                int cmbItemsWidth = 240.DpiZoom();
                int dgvHeight = 300.DpiZoom();
                dgvInfo.Top = margin;
                dgvInfo.Left = dgvItems.Left = cmbInfo.Left = margin;
                dgvItems.Top = dgvInfo.Bottom + 5.DpiZoom();
                dgvItems.Height = dgvHeight;
                cmbInfo.Top = cmbItems.Top = dgvItems.Bottom + margin;
                cmbItems.Left = cmbInfo.Right;
                cmbItems.Width = cmbItemsWidth;
                btnOK.Top = btnCancel.Top = cmbItems.Bottom + margin;
                btnOK.Left = (cmbItems.Width + cmbInfo.Width + 2 * margin - margin) / 2 - btnOK.Width;
                btnCancel.Left = btnOK.Right + margin;
                ClientSize = new Size(cmbItems.Right + margin, btnCancel.Bottom + margin);
                dgvItems.Width = ClientSize.Width - 2 * margin;
                cmbItems.AutosizeDropDownWidth();
            }

            private void ShowDgvList(string[] value)
            {
                // 显示1列数据列
                int headLength = 1;
                dgvItems.ColumnCount = headLength;
                dgvItems.Columns[headLength - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                for (int m = 0; m < headLength; m++)
                {
                    dgvItems.Columns[m].HeaderText = value[m];
                }
                for (int i = 0; i < value.Length; i++)
                {
                    string[] line = new string[headLength];
                    string temp = value[i];
                    line[0] = temp;
                    dgvItems.Rows.Add(line);
                }
                // 增加复选框列
                DataGridViewCheckBoxColumn checkbox = new DataGridViewCheckBoxColumn()
                {
                    TrueValue = true,
                    FalseValue = false,
                    DataPropertyName = "IsChecked",
                    Width = 50.DpiZoom(),
                    Resizable = DataGridViewTriState.False, // 列大小不改变
                };
                // 插入到第0列
                dgvItems.Columns.Insert(0, checkbox);
                dgvItems.CellMouseClick += (sender, e) => { Dgv_CellMouseClick(sender, e); };
            }

            private void Dgv_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
            {
                // 不对序号列和标题列处理
                if (e.RowIndex != -1 && e.ColumnIndex != -1)
                {
                    // 复选框列的值进行改变
                    if ((bool)dgvItems.Rows[e.RowIndex].Cells[0].EditedFormattedValue == true)
                    {
                        dgvItems.Rows[e.RowIndex].Cells[0].Value = false;
                        dgvSelectedItems.Remove(DgvItems[e.RowIndex]);
                    }
                    else
                    {
                        dgvItems.Rows[e.RowIndex].Cells[0].Value = true;
                        dgvSelectedItems.Add(DgvItems[e.RowIndex]);
                    }
                }
            }
        }
    }
}