using System.Drawing;
using System.Windows.Forms;

namespace BossKey.Components
{
    partial class ColorPickerButton
    {
        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            _swatchPanel = new Panel();
            _btnDialog = new Button();
            _btnPick = new Button();
            SuspendLayout();

            //
            // _swatchPanel
            //
            _swatchPanel.BorderStyle = BorderStyle.None;
            _swatchPanel.Cursor = Cursors.Hand;
            _swatchPanel.Location = new Point(0, 0);
            _swatchPanel.Name = "_swatchPanel";
            _swatchPanel.Size = new Size(72, 28);
            _swatchPanel.TabIndex = 0;
            _swatchPanel.Click += SwatchPanel_Click;
            _swatchPanel.Paint += SwatchPanel_Paint;

            //
            // _btnDialog
            //
            _btnDialog.FlatStyle = FlatStyle.System;
            _btnDialog.Font = new Font("Microsoft Sans Serif", 7.5f);
            _btnDialog.Location = new Point(72, 0);
            _btnDialog.Name = "_btnDialog";
            _btnDialog.Size = new Size(20, 28);
            _btnDialog.TabIndex = 1;
            _btnDialog.Text = "…";
            _btnDialog.TextAlign = ContentAlignment.MiddleCenter;
            _btnDialog.UseVisualStyleBackColor = true;
            _btnDialog.Click += BtnDialog_Click;

            //
            // _btnPick
            //
            _btnPick.FlatStyle = FlatStyle.System;
            _btnPick.Font = new Font("Microsoft Sans Serif", 10f);
            _btnPick.Location = new Point(92, 0);
            _btnPick.Name = "_btnPick";
            _btnPick.Size = new Size(20, 28);
            _btnPick.TabIndex = 2;
            _btnPick.Text = "◉";
            _btnPick.TextAlign = ContentAlignment.MiddleCenter;
            _btnPick.UseVisualStyleBackColor = true;
            _btnPick.Click += BtnPick_Click;

            //
            // ColorPickerButton
            //
            Controls.Add(_swatchPanel);
            Controls.Add(_btnDialog);
            Controls.Add(_btnPick);
            Name = "ColorPickerButton";
            Size = new Size(112, 28);
            TabStop = false;
            ResumeLayout(false);
        }

        #endregion
    }
}
