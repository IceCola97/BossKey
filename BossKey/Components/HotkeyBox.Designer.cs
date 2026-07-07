namespace BossKey.Components
{
    partial class HotkeyBox
    {
        /// <summary>
        /// 内部封装的文本框。
        /// </summary>
        private BossKey.Components.HotkeyTextBox textBox;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // UserControl 会自动释放其 Controls 集合中的子控件
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
            this.textBox = new BossKey.Components.HotkeyTextBox();
            this.SuspendLayout();
            // 
            // textBox
            // 
            this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBox.Location = new System.Drawing.Point(0, 0);
            this.textBox.Name = "textBox";
            this.textBox.ShortcutsEnabled = false;
            this.textBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.textBox.Size = new System.Drawing.Size(150, 23);
            this.textBox.TabIndex = 0;
            this.textBox.Text = string.Empty;
            this.textBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // HotkeyBox
            // 
            this.Controls.Add(this.textBox);
            this.Name = "HotkeyBox";
            this.Size = new System.Drawing.Size(150, 23);
            this.TabStop = false;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
