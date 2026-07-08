using BossKey.Components;

namespace BossKey
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            listWindows = new ListView();
            columnHeader = new ColumnHeader();
            imageWindow = new ImageList(components);
            textSearch = new TextBox();
            groupConf = new GroupBox();
            checkTopmost = new CheckBox();
            labelVolume = new Label();
            labelOpacity = new Label();
            trackVolume = new TrackBar();
            checkVolume = new CheckBox();
            hotkeyAutoHide = new HotkeyBox();
            checkAutoHide = new CheckBox();
            trackOpacity = new TrackBar();
            checkOpacity = new CheckBox();
            timerLock = new System.Windows.Forms.Timer(components);
            groupConf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackOpacity).BeginInit();
            SuspendLayout();
            // 
            // listWindows
            // 
            listWindows.Activation = ItemActivation.OneClick;
            listWindows.Columns.AddRange(new ColumnHeader[] { columnHeader });
            listWindows.FullRowSelect = true;
            listWindows.HeaderStyle = ColumnHeaderStyle.None;
            listWindows.LargeImageList = imageWindow;
            listWindows.Location = new Point(12, 45);
            listWindows.Name = "listWindows";
            listWindows.Size = new Size(425, 393);
            listWindows.SmallImageList = imageWindow;
            listWindows.TabIndex = 0;
            listWindows.UseCompatibleStateImageBehavior = false;
            listWindows.View = View.Details;
            listWindows.SelectedIndexChanged += ListWindows_SelectedIndexChanged;
            // 
            // imageWindow
            // 
            imageWindow.ColorDepth = ColorDepth.Depth32Bit;
            imageWindow.ImageSize = new Size(16, 16);
            imageWindow.TransparentColor = Color.Transparent;
            // 
            // textSearch
            // 
            textSearch.Location = new Point(12, 12);
            textSearch.Name = "textSearch";
            textSearch.Size = new Size(425, 27);
            textSearch.TabIndex = 1;
            textSearch.TextChanged += TextSearch_TextChanged;
            // 
            // groupConf
            // 
            groupConf.Controls.Add(checkTopmost);
            groupConf.Controls.Add(labelVolume);
            groupConf.Controls.Add(labelOpacity);
            groupConf.Controls.Add(trackVolume);
            groupConf.Controls.Add(checkVolume);
            groupConf.Controls.Add(hotkeyAutoHide);
            groupConf.Controls.Add(checkAutoHide);
            groupConf.Controls.Add(trackOpacity);
            groupConf.Controls.Add(checkOpacity);
            groupConf.Location = new Point(443, 12);
            groupConf.Name = "groupConf";
            groupConf.Size = new Size(200, 426);
            groupConf.TabIndex = 2;
            groupConf.TabStop = false;
            groupConf.Text = "窗口配置";
            // 
            // checkTopmost
            // 
            checkTopmost.AutoSize = true;
            checkTopmost.Location = new Point(6, 223);
            checkTopmost.Name = "checkTopmost";
            checkTopmost.Size = new Size(121, 24);
            checkTopmost.TabIndex = 8;
            checkTopmost.Text = "启用窗口置顶";
            checkTopmost.UseVisualStyleBackColor = true;
            checkTopmost.CheckedChanged += CheckTopmost_CheckedChanged;
            // 
            // labelVolume
            // 
            labelVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelVolume.AutoSize = true;
            labelVolume.Enabled = false;
            labelVolume.Location = new Point(145, 157);
            labelVolume.Name = "labelVolume";
            labelVolume.Size = new Size(49, 20);
            labelVolume.TabIndex = 7;
            labelVolume.Text = "100%";
            labelVolume.TextAlign = ContentAlignment.TopRight;
            // 
            // labelOpacity
            // 
            labelOpacity.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelOpacity.AutoSize = true;
            labelOpacity.Enabled = false;
            labelOpacity.Location = new Point(145, 27);
            labelOpacity.Name = "labelOpacity";
            labelOpacity.Size = new Size(49, 20);
            labelOpacity.TabIndex = 6;
            labelOpacity.Text = "100%";
            labelOpacity.TextAlign = ContentAlignment.TopRight;
            // 
            // trackVolume
            // 
            trackVolume.AutoSize = false;
            trackVolume.Enabled = false;
            trackVolume.Location = new Point(27, 186);
            trackVolume.Maximum = 100;
            trackVolume.Name = "trackVolume";
            trackVolume.Size = new Size(167, 31);
            trackVolume.TabIndex = 5;
            trackVolume.TickFrequency = 10;
            trackVolume.Value = 100;
            trackVolume.Scroll += TrackVolume_Scroll;
            // 
            // checkVolume
            // 
            checkVolume.AutoSize = true;
            checkVolume.Location = new Point(6, 156);
            checkVolume.Name = "checkVolume";
            checkVolume.Size = new Size(121, 24);
            checkVolume.TabIndex = 4;
            checkVolume.Text = "启用音量控制";
            checkVolume.UseVisualStyleBackColor = true;
            checkVolume.CheckedChanged += CheckVolume_CheckedChanged;
            // 
            // hotkeyAutoHide
            // 
            hotkeyAutoHide.Enabled = false;
            hotkeyAutoHide.Location = new Point(27, 123);
            hotkeyAutoHide.Name = "hotkeyAutoHide";
            hotkeyAutoHide.Size = new Size(167, 27);
            hotkeyAutoHide.TabIndex = 3;
            hotkeyAutoHide.TabStop = false;
            hotkeyAutoHide.HotkeyChanged += HotkeyAutoHide_HotkeyChanged;
            // 
            // checkAutoHide
            // 
            checkAutoHide.AutoSize = true;
            checkAutoHide.Location = new Point(6, 93);
            checkAutoHide.Name = "checkAutoHide";
            checkAutoHide.Size = new Size(136, 24);
            checkAutoHide.TabIndex = 2;
            checkAutoHide.Text = "启用快捷键隐藏";
            checkAutoHide.UseVisualStyleBackColor = true;
            checkAutoHide.CheckedChanged += CheckAutoHide_CheckedChanged;
            // 
            // trackOpacity
            // 
            trackOpacity.AutoSize = false;
            trackOpacity.Enabled = false;
            trackOpacity.Location = new Point(27, 56);
            trackOpacity.Maximum = 255;
            trackOpacity.Name = "trackOpacity";
            trackOpacity.Size = new Size(167, 31);
            trackOpacity.TabIndex = 1;
            trackOpacity.TickFrequency = 16;
            trackOpacity.Value = 255;
            trackOpacity.Scroll += TrackOpacity_Scroll;
            // 
            // checkOpacity
            // 
            checkOpacity.AutoSize = true;
            checkOpacity.Location = new Point(6, 26);
            checkOpacity.Name = "checkOpacity";
            checkOpacity.Size = new Size(106, 24);
            checkOpacity.TabIndex = 0;
            checkOpacity.Text = "启用半透明";
            checkOpacity.UseVisualStyleBackColor = true;
            checkOpacity.CheckedChanged += CheckOpacity_CheckedChanged;
            // 
            // timerLock
            // 
            timerLock.Enabled = true;
            timerLock.Interval = 50;
            timerLock.Tick += TimerLock_Tick;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(655, 450);
            Controls.Add(groupConf);
            Controls.Add(textSearch);
            Controls.Add(listWindows);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BossKey";
            TopMost = true;
            Load += FormMain_Load;
            KeyUp += FormMain_KeyUp;
            groupConf.ResumeLayout(false);
            groupConf.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackOpacity).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listWindows;
        private TextBox textSearch;
        private GroupBox groupConf;
        private CheckBox checkOpacity;
        private TrackBar trackOpacity;
        private CheckBox checkAutoHide;
        private Components.HotkeyBox hotkeyAutoHide;
        private Label labelOpacity;
        private TrackBar trackVolume;
        private CheckBox checkVolume;
        private CheckBox checkTopmost;
        private Label labelVolume;
        private ImageList imageWindow;
        private ColumnHeader columnHeader;
        private System.Windows.Forms.Timer timerLock;
    }
}
