using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class RestoreDialog : CommonDialog
    {
        public string Title { get; set; }
        public string Selected { get; set; }
        public int SelectedIndex { get; set; }
        public string[] Items { get; set; }
        public bool CanEdit { get; set; }

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(SelectForm frm = new SelectForm())
            {
                frm.Text = Title;
                frm.Items = Items;
                if(Selected != null) frm.Selected = Selected;
                else frm.SelectedIndex = SelectedIndex;
                frm.CanEdit = CanEdit;
                Form owner = (Form)Control.FromHandle(hwndOwner);
                if(owner != null) frm.TopMost = owner.TopMost;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if(flag)
                {
                    Selected = frm.Selected;
                    SelectedIndex = frm.SelectedIndex;
                }
                return flag;
            }
        }

        sealed class SelectForm : Form
        {
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

            public string Selected
            {
                get => cmbItems.Text;
                set => cmbItems.Text = value;
            }

            public string[] Items
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

            public bool CanEdit
            {
                get => cmbItems.DropDownStyle == ComboBoxStyle.DropDown;
                set => cmbItems.DropDownStyle = value ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            }

            public int SelectedIndex
            {
                get => cmbItems.SelectedIndex;
                set => cmbItems.SelectedIndex = value;
            }

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
            readonly ComboBox cmbItems = new ComboBox
            {
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DropDownHeight = 294.DpiZoom(),
                ImeMode = ImeMode.Disable
            };
            readonly Label lblInfo = new Label
            {
                Text = "恢复模式：",
                AutoSize = true
            };

            private void InitializeComponents()
            {
                Controls.AddRange(new Control[] { lblInfo, cmbItems, btnOK, btnCancel });
                int margin = 20.DpiZoom();
                int cmbItemsWidth = 240.DpiZoom();
                int lblItemMargin = 6.DpiZoom();
                lblInfo.Left = lblInfo.Top = cmbItems.Top = margin;
                cmbItems.Left = lblInfo.Right + lblItemMargin;
                cmbItems.Width = cmbItemsWidth;
                btnOK.Top = btnCancel.Top = cmbItems.Bottom + margin;
                btnOK.Left = (cmbItems.Width + lblInfo.Width + 3 * margin - lblItemMargin) / 2 - btnOK.Width;
                btnCancel.Left = btnOK.Right + margin;
                ClientSize = new Size(cmbItems.Right + margin, btnCancel.Bottom + margin);
                cmbItems.AutosizeDropDownWidth();
            }
        }
    }
}