using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace 快捷键窗体切换
{
    public partial class HotkeyConfigForm : Form
    {
        private TextBox hotkeyTextBox;
        private CheckBox ctrlCheckBox;
        private CheckBox altCheckBox;
        private CheckBox shiftCheckBox;
        private CheckBox winCheckBox;
        private ComboBox appComboBox;
        private TextBox pathTextBox;
        private Button browseButton;
        private Button okButton;
        private Button cancelButton;
        private CheckBox mappingCheckBox;
        private GroupBox mappedHotkeyGroup;
        private TextBox mappedHotkeyTextBox;
        private CheckBox mappedCtrlCheckBox;
        private CheckBox mappedAltCheckBox;
        private CheckBox mappedShiftCheckBox;
        private CheckBox mappedWinCheckBox;

        private HotkeyConfig currentConfig;
        private bool isCapturing = false;
        private bool isMappedCapturing = false;

        public HotkeyConfigForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(350, 350);
            this.Text = "快捷键配置";
        }

        private void InitializeComponent()
        {
            Label hotkeyLabel = new Label { Text = "快捷键:", Location = new Point(20, 20), AutoSize = true };
            hotkeyTextBox = new TextBox { Location = new Point(100, 17), Width = 200, ReadOnly = true };
            hotkeyTextBox.Click += HotkeyTextBox_Click;

            ctrlCheckBox = new CheckBox { Text = "Ctrl", Location = new Point(20, 50), AutoSize = true };
            altCheckBox = new CheckBox { Text = "Alt", Location = new Point(80, 50), AutoSize = true };
            shiftCheckBox = new CheckBox { Text = "Shift", Location = new Point(140, 50), AutoSize = true };
            winCheckBox = new CheckBox { Text = "Win", Location = new Point(200, 50), AutoSize = true };

            Label appLabel = new Label { Text = "应用名称:", Location = new Point(20, 80), AutoSize = true };
            appComboBox = new ComboBox { Location = new Point(100, 77), Width = 200 };
            appComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            appComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            appComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label pathLabel = new Label { Text = "启动路径:", Location = new Point(20, 110), AutoSize = true };
            pathTextBox = new TextBox { Location = new Point(100, 107), Width = 160 };
            browseButton = new Button { Text = "浏览", Location = new Point(265, 106), Width = 50, Height = 23 };
            browseButton.Click += BrowseButton_Click;

            mappingCheckBox = new CheckBox { 
                Text = "映射到其他快捷键", 
                Location = new Point(20, 140),
                AutoSize = true
            };
            mappingCheckBox.CheckedChanged += MappingCheckBox_CheckedChanged;

            mappedHotkeyGroup = new GroupBox {
                Text = "映射的快捷键",
                Location = new Point(20, 165),
                Size = new Size(295, 90),
                Enabled = false
            };

            Label mappedHotkeyLabel = new Label { Text = "快捷键:", Location = new Point(10, 25), AutoSize = true };
            mappedHotkeyTextBox = new TextBox { Location = new Point(80, 22), Width = 200, ReadOnly = true };
            mappedHotkeyTextBox.Click += MappedHotkeyTextBox_Click;

            mappedCtrlCheckBox = new CheckBox { Text = "Ctrl", Location = new Point(10, 55), AutoSize = true };
            mappedAltCheckBox = new CheckBox { Text = "Alt", Location = new Point(70, 55), AutoSize = true };
            mappedShiftCheckBox = new CheckBox { Text = "Shift", Location = new Point(130, 55), AutoSize = true };
            mappedWinCheckBox = new CheckBox { Text = "Win", Location = new Point(190, 55), AutoSize = true };

            mappedHotkeyGroup.Controls.AddRange(new Control[] {
                mappedHotkeyLabel, mappedHotkeyTextBox,
                mappedCtrlCheckBox, mappedAltCheckBox, mappedShiftCheckBox, mappedWinCheckBox
            });

            okButton = new Button { Text = "确定", Location = new Point(100, 270), Width = 75, Height = 25 };
            cancelButton = new Button { Text = "取消", Location = new Point(190, 270), Width = 75, Height = 25 };

            okButton.Click += OkButton_Click;
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                hotkeyLabel, hotkeyTextBox,
                ctrlCheckBox, altCheckBox, shiftCheckBox, winCheckBox,
                appLabel, appComboBox,
                pathLabel, pathTextBox, browseButton,
                mappingCheckBox, mappedHotkeyGroup,
                okButton, cancelButton
            });

            LoadProcessList();
        }

        private void LoadProcessList()
        {
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        appComboBox.Items.Add(process.ProcessName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载进程列表失败: {ex.Message}");
            }
        }

        private void HotkeyTextBox_Click(object sender, EventArgs e)
        {
            if (!isCapturing)
            {
                isCapturing = true;
                hotkeyTextBox.Text = "请按下快捷键...";
            }
        }

        private void MappingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            mappedHotkeyGroup.Enabled = mappingCheckBox.Checked;
            appComboBox.Enabled = !mappingCheckBox.Checked;
            pathTextBox.Enabled = !mappingCheckBox.Checked;
            browseButton.Enabled = !mappingCheckBox.Checked;
        }

        private void MappedHotkeyTextBox_Click(object sender, EventArgs e)
        {
            if (!isMappedCapturing)
            {
                isMappedCapturing = true;
                mappedHotkeyTextBox.Text = "请按下快捷键...";
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hotkeyTextBox.Text) || 
                (!mappingCheckBox.Checked && string.IsNullOrEmpty(appComboBox.Text)))
            {
                MessageBox.Show("请设置快捷键和应用程序！");
                return;
            }

            if (mappingCheckBox.Checked && string.IsNullOrEmpty(mappedHotkeyTextBox.Text))
            {
                MessageBox.Show("请设置映射的快捷键！");
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (isCapturing)
            {
                e.SuppressKeyPress = true;
                hotkeyTextBox.Text = e.KeyCode.ToString();
                currentConfig = new HotkeyConfig
                {
                    Key = (uint)e.KeyCode,
                    IsControl = e.Control,
                    IsAlt = e.Alt,
                    IsShift = e.Shift,
                    IsWin = e.Modifiers.HasFlag(Keys.LWin) || e.Modifiers.HasFlag(Keys.RWin)
                };
                isCapturing = false;

                ctrlCheckBox.Checked = e.Control;
                altCheckBox.Checked = e.Alt;
                shiftCheckBox.Checked = e.Shift;
                winCheckBox.Checked = e.Modifiers.HasFlag(Keys.LWin) || e.Modifiers.HasFlag(Keys.RWin);
            }
            else if (isMappedCapturing)
            {
                e.SuppressKeyPress = true;
                mappedHotkeyTextBox.Text = e.KeyCode.ToString();

                mappedCtrlCheckBox.Checked = e.Control;
                mappedAltCheckBox.Checked = e.Alt;
                mappedShiftCheckBox.Checked = e.Shift;
                mappedWinCheckBox.Checked = e.Modifiers.HasFlag(Keys.LWin) || e.Modifiers.HasFlag(Keys.RWin);

                isMappedCapturing = false;
            }
            base.OnKeyDown(e);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*";
                dialog.FilterIndex = 1;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    pathTextBox.Text = dialog.FileName;
                    appComboBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }

        public void SetConfig(HotkeyConfig config)
        {
            if (config != null)
            {
                currentConfig = config;
                hotkeyTextBox.Text = ((Keys)config.Key).ToString();
                ctrlCheckBox.Checked = config.IsControl;
                altCheckBox.Checked = config.IsAlt;
                shiftCheckBox.Checked = config.IsShift;
                winCheckBox.Checked = config.IsWin;
                appComboBox.Text = config.ProcessName;
                pathTextBox.Text = config.AppPath ?? "";

                mappingCheckBox.Checked = config.IsHotkeyMapping;
                if (config.MappedHotkey != null)
                {
                    mappedHotkeyTextBox.Text = ((Keys)config.MappedHotkey.Key).ToString();
                    mappedCtrlCheckBox.Checked = config.MappedHotkey.IsControl;
                    mappedAltCheckBox.Checked = config.MappedHotkey.IsAlt;
                    mappedShiftCheckBox.Checked = config.MappedHotkey.IsShift;
                    mappedWinCheckBox.Checked = config.MappedHotkey.IsWin;
                }
            }
        }

        public HotkeyConfig GetConfig()
        {
            if (currentConfig == null) return null;

            currentConfig.IsControl = ctrlCheckBox.Checked;
            currentConfig.IsAlt = altCheckBox.Checked;
            currentConfig.IsShift = shiftCheckBox.Checked;
            currentConfig.IsWin = winCheckBox.Checked;
            currentConfig.ProcessName = appComboBox.Text;
            currentConfig.AppPath = pathTextBox.Text;
            currentConfig.IsHotkeyMapping = mappingCheckBox.Checked;

            if (mappingCheckBox.Checked)
            {
                currentConfig.MappedHotkey = new HotkeyConfig
                {
                    Key = (uint)(Keys)Enum.Parse(typeof(Keys), mappedHotkeyTextBox.Text),
                    IsControl = mappedCtrlCheckBox.Checked,
                    IsAlt = mappedAltCheckBox.Checked,
                    IsShift = mappedShiftCheckBox.Checked,
                    IsWin = mappedWinCheckBox.Checked
                };
            }
            else
            {
                currentConfig.MappedHotkey = null;
            }

            return currentConfig;
        }
    }
} 