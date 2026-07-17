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
            colorPickerButton = new ColorPickerButton();
            checkTopmost = new CheckBox();
            labelVolume = new Label();
            labelOpacity = new Label();
            trackVolume = new TrackBar();
            checkVolume = new CheckBox();
            hotkeyAutoHide = new HotkeyBox();
            checkAutoHide = new CheckBox();
            trackOpacity = new TrackBar();
            checkTransparentColor = new CheckBox();
            checkOpacity = new CheckBox();
            timerLock = new System.Windows.Forms.Timer(components);
            buttonConfigs = new Button();
            contextMenuPreferences = new ContextMenuStrip(components);
            menuPreferencesRememberCloseAction = new ToolStripMenuItem();
            menuPreferencesRecentWindowCount = new ToolStripMenuItem();
            menuPreferencesRecentWindowCountMax5 = new ToolStripMenuItem();
            menuPreferencesRecentWindowCountMax10 = new ToolStripMenuItem();
            menuPreferencesRecentWindowCountMax20 = new ToolStripMenuItem();
            groupConf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackOpacity).BeginInit();
            contextMenuPreferences.SuspendLayout();
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
            listWindows.Size = new Size(425, 399);
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
            groupConf.Controls.Add(colorPickerButton);
            groupConf.Controls.Add(checkTopmost);
            groupConf.Controls.Add(labelVolume);
            groupConf.Controls.Add(labelOpacity);
            groupConf.Controls.Add(trackVolume);
            groupConf.Controls.Add(checkVolume);
            groupConf.Controls.Add(hotkeyAutoHide);
            groupConf.Controls.Add(checkAutoHide);
            groupConf.Controls.Add(trackOpacity);
            groupConf.Controls.Add(checkTransparentColor);
            groupConf.Controls.Add(checkOpacity);
            groupConf.Location = new Point(443, 45);
            groupConf.Name = "groupConf";
            groupConf.Size = new Size(200, 399);
            groupConf.TabIndex = 2;
            groupConf.TabStop = false;
            groupConf.Text = "窗口配置";
            // 
            // colorPickerButton
            // 
            colorPickerButton.Location = new Point(27, 283);
            colorPickerButton.Name = "colorPickerButton";
            colorPickerButton.SelectedColor = Color.White;
            colorPickerButton.Size = new Size(167, 28);
            colorPickerButton.TabIndex = 9;
            colorPickerButton.TabStop = false;
            colorPickerButton.Visible = false;
            colorPickerButton.SelectedColorChanged += ColorPickerButton_SelectedColorChanged;
            // 
            // checkTopmost
            // 
            checkTopmost.AutoSize = true;
            checkTopmost.Location = new Point(6, 223);
            checkTopmost.Name = "checkTopmost";
            checkTopmost.Size = new Size(140, 24);
            checkTopmost.TabIndex = 8;
            checkTopmost.Text = "启用窗口置顶(&T)";
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
            checkVolume.Size = new Size(141, 24);
            checkVolume.TabIndex = 4;
            checkVolume.Text = "启用音量控制(&V)";
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
            checkAutoHide.Size = new Size(158, 24);
            checkAutoHide.TabIndex = 2;
            checkAutoHide.Text = "启用快捷键隐藏(&H)";
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
            // checkTransparentColor
            // 
            checkTransparentColor.AutoSize = true;
            checkTransparentColor.Location = new Point(6, 253);
            checkTransparentColor.Name = "checkTransparentColor";
            checkTransparentColor.Size = new Size(146, 24);
            checkTransparentColor.TabIndex = 0;
            checkTransparentColor.Text = "启用遮罩颜色(&M)";
            checkTransparentColor.UseVisualStyleBackColor = true;
            checkTransparentColor.Visible = false;
            checkTransparentColor.CheckedChanged += CheckTransparentColor_CheckedChanged;
            // 
            // checkOpacity
            // 
            checkOpacity.AutoSize = true;
            checkOpacity.Location = new Point(6, 26);
            checkOpacity.Name = "checkOpacity";
            checkOpacity.Size = new Size(128, 24);
            checkOpacity.TabIndex = 0;
            checkOpacity.Text = "启用半透明(&O)";
            checkOpacity.UseVisualStyleBackColor = true;
            checkOpacity.CheckedChanged += CheckOpacity_CheckedChanged;
            // 
            // timerLock
            // 
            timerLock.Enabled = true;
            timerLock.Interval = 50;
            timerLock.Tick += TimerLock_Tick;
            // 
            // buttonConfigs
            // 
            buttonConfigs.Location = new Point(561, 12);
            buttonConfigs.Name = "buttonConfigs";
            buttonConfigs.Size = new Size(82, 27);
            buttonConfigs.TabIndex = 3;
            buttonConfigs.Text = "设置...(&P)";
            buttonConfigs.UseVisualStyleBackColor = true;
            buttonConfigs.Click += ButtonConfigs_Click;
            // 
            // contextMenuPreferences
            // 
            contextMenuPreferences.ImageScalingSize = new Size(20, 20);
            contextMenuPreferences.Items.AddRange(new ToolStripItem[] { menuPreferencesRememberCloseAction, menuPreferencesRecentWindowCount });
            contextMenuPreferences.Name = "contextMenuPreferences";
            contextMenuPreferences.Size = new Size(201, 52);
            // 
            // menuPreferencesRememberCloseAction
            // 
            menuPreferencesRememberCloseAction.Name = "menuPreferencesRememberCloseAction";
            menuPreferencesRememberCloseAction.Size = new Size(200, 24);
            menuPreferencesRememberCloseAction.Text = "记住关闭选择(&C)";
            menuPreferencesRememberCloseAction.Click += MenuPreferencesRememberCloseAction_Click;
            // 
            // menuPreferencesRecentWindowCount
            // 
            menuPreferencesRecentWindowCount.DropDownItems.AddRange(new ToolStripItem[] { menuPreferencesRecentWindowCountMax5, menuPreferencesRecentWindowCountMax10, menuPreferencesRecentWindowCountMax20 });
            menuPreferencesRecentWindowCount.Name = "menuPreferencesRecentWindowCount";
            menuPreferencesRecentWindowCount.Size = new Size(200, 24);
            menuPreferencesRecentWindowCount.Text = "最近窗口数量...(&R)";
            // 
            // menuPreferencesRecentWindowCountMax5
            // 
            menuPreferencesRecentWindowCountMax5.Name = "menuPreferencesRecentWindowCountMax5";
            menuPreferencesRecentWindowCountMax5.Size = new Size(224, 26);
            menuPreferencesRecentWindowCountMax5.Text = "最多5条(&5)";
            menuPreferencesRecentWindowCountMax5.Click += MenuPreferencesRecentWindowCountMax5_Click;
            // 
            // menuPreferencesRecentWindowCountMax10
            // 
            menuPreferencesRecentWindowCountMax10.Name = "menuPreferencesRecentWindowCountMax10";
            menuPreferencesRecentWindowCountMax10.Size = new Size(224, 26);
            menuPreferencesRecentWindowCountMax10.Text = "最多10条(&0)";
            menuPreferencesRecentWindowCountMax10.Click += MenuPreferencesRecentWindowCountMax10_Click;
            // 
            // menuPreferencesRecentWindowCountMax20
            // 
            menuPreferencesRecentWindowCountMax20.Name = "menuPreferencesRecentWindowCountMax20";
            menuPreferencesRecentWindowCountMax20.Size = new Size(224, 26);
            menuPreferencesRecentWindowCountMax20.Text = "最多20条(&2)";
            menuPreferencesRecentWindowCountMax20.Click += MenuPreferencesRecentWindowCountMax20_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(655, 456);
            Controls.Add(buttonConfigs);
            Controls.Add(groupConf);
            Controls.Add(textSearch);
            Controls.Add(listWindows);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BossKey";
            TopMost = true;
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            KeyUp += FormMain_KeyUp;
            groupConf.ResumeLayout(false);
            groupConf.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackOpacity).EndInit();
            contextMenuPreferences.ResumeLayout(false);
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
        private CheckBox checkTransparentColor;
        private ColorPickerButton colorPickerButton;
        private Button buttonConfigs;
        private ContextMenuStrip contextMenuPreferences;
        private ToolStripMenuItem menuPreferencesRememberCloseAction;
        private ToolStripMenuItem menuPreferencesRecentWindowCount;
        private ToolStripMenuItem menuPreferencesRecentWindowCountMax5;
        private ToolStripMenuItem menuPreferencesRecentWindowCountMax10;
        private ToolStripMenuItem menuPreferencesRecentWindowCountMax20;
    }
}
