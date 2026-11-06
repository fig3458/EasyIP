using System;
using System.Drawing;
using System.Windows.Forms;

namespace EasyIP
{
    /// <summary>
    /// Minimal UI: read current IP, only edit the last two octets (e.g. 16.25).
    /// Mask / DNS / gateway stay on app defaults; gateway auto = x.x.x.1
    /// </summary>
    public sealed class MainFormLite : Form
    {
        private const string DefaultMask = "255.255.254.0";
        private const string DefaultDns1 = "192.168.106.3";
        private const string DefaultDns2 = "223.5.5.5";

        private string _adapterName;
        private string _prefix = "192.168.";

        private Label _lblAdapter;
        private Label _lblPrefix;
        private TextBox _txtTail;
        private Label _lblPreview;
        private Label _lblHint;
        private Button _btnApply;
        private Button _btnReload;

        public MainFormLite()
        {
            BuildUi();
            ReloadFromNic();
        }

        private void BuildUi()
        {
            Text = "EasyIP Lite — 只改后两段";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 250);
            Font = CreateUiFont(9F);
            BackColor = Color.FromArgb(248, 249, 251);
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            _lblAdapter = new Label
            {
                AutoSize = false,
                Location = new Point(20, 16),
                Size = new Size(420, 22),
                ForeColor = Color.FromArgb(90, 90, 90),
                Text = "网卡：—",
            };
            Controls.Add(_lblAdapter);

            Label tip = new Label
            {
                AutoSize = false,
                Location = new Point(20, 44),
                Size = new Size(420, 22),
                Text = "前两段固定，只改后面（例如 16.25 → 16.30）：",
            };
            Controls.Add(tip);

            Panel ipRow = new Panel
            {
                Location = new Point(20, 78),
                Size = new Size(420, 52),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };
            Controls.Add(ipRow);

            _lblPrefix = new Label
            {
                AutoSize = false,
                Location = new Point(12, 8),
                Size = new Size(130, 34),
                Font = CreateUiFont(18F),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(100, 100, 100),
                Text = "192.168.",
            };
            ipRow.Controls.Add(_lblPrefix);

            _txtTail = new TextBox
            {
                Location = new Point(150, 8),
                Size = new Size(250, 34),
                Font = CreateUiFont(18F),
                BorderStyle = BorderStyle.None,
                Text = "0.0",
            };
            _txtTail.TextChanged += delegate { UpdatePreview(); };
            ipRow.Controls.Add(_txtTail);

            _lblPreview = new Label
            {
                AutoSize = false,
                Location = new Point(20, 142),
                Size = new Size(420, 24),
                Font = CreateUiFont(10F),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = "将设置为：—",
            };
            Controls.Add(_lblPreview);

            _lblHint = new Label
            {
                AutoSize = false,
                Location = new Point(20, 168),
                Size = new Size(420, 20),
                ForeColor = Color.DimGray,
                Text = "掩码 " + DefaultMask + "  ·  网关自动 .1  ·  DNS " + DefaultDns1 + " / " + DefaultDns2,
            };
            Controls.Add(_lblHint);

            _btnApply = new Button
            {
                Text = "应用 (Enter)",
                Location = new Point(20, 200),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = CreateUiFont(10F),
                Cursor = Cursors.Hand,
            };
            _btnApply.FlatAppearance.BorderSize = 0;
            _btnApply.Click += OnApply;
            Controls.Add(_btnApply);

            _btnReload = new Button
            {
                Text = "重新读取网卡",
                Location = new Point(240, 200),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(97, 97, 97),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = CreateUiFont(10F),
                Cursor = Cursors.Hand,
            };
            _btnReload.FlatAppearance.BorderSize = 0;
            _btnReload.Click += delegate { ReloadFromNic(); };
            Controls.Add(_btnReload);

            AcceptButton = _btnApply;
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OnApply(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ReloadFromNic()
        {
            string adapter;
            string ip;
            string desc;
            if (!NetworkHelper.TryGetPreferredIpv4(out adapter, out ip, out desc))
            {
                // Fallback: first listed adapter, default prefix
                var list = NetworkHelper.GetAdapters();
                if (list.Count > 0)
                {
                    _adapterName = list[0].Name;
                    ip = NetworkHelper.GetCurrentIpv4(_adapterName);
                    desc = list[0].Description;
                }
                else
                {
                    _adapterName = null;
                    MessageBox.Show(this, "未找到可用网卡。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                _adapterName = adapter;
            }

            string prefix;
            string tail;
            if (!string.IsNullOrEmpty(ip) && NetworkHelper.TrySplitPrefixTail(ip, out prefix, out tail))
            {
                _prefix = prefix;
                _lblPrefix.Text = prefix;
                _txtTail.Text = tail;
                _txtTail.SelectAll();
            }
            else
            {
                _prefix = "192.168.";
                _lblPrefix.Text = _prefix;
                _txtTail.Text = "";
            }

            string shown = string.IsNullOrEmpty(desc) ? _adapterName : (_adapterName + "  (" + desc + ")");
            string cur = string.IsNullOrEmpty(ip) ? "无 IPv4" : ip;
            _lblAdapter.Text = "网卡：" + shown + "    当前 " + cur;
            UpdatePreview();
            _txtTail.Focus();
            _txtTail.SelectAll();
        }

        private void UpdatePreview()
        {
            string full;
            if (NetworkHelper.TryJoinPrefixTail(_prefix, _txtTail.Text, out full))
            {
                string gw = NetworkHelper.SuggestGateway(full);
                _lblPreview.Text = "将设置为：" + full + "    网关 " + gw;
                _lblPreview.ForeColor = Color.FromArgb(33, 33, 33);
                _btnApply.Enabled = true;
            }
            else
            {
                _lblPreview.Text = "将设置为：格式不正确（请输入如 16.25）";
                _lblPreview.ForeColor = Color.FromArgb(198, 40, 40);
                _btnApply.Enabled = false;
            }
        }

        private void OnApply(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_adapterName))
            {
                MessageBox.Show(this, "未选择网卡。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string full;
            if (!NetworkHelper.TryJoinPrefixTail(_prefix, _txtTail.Text, out full))
            {
                MessageBox.Show(this, "请输入正确的后两段，例如 16.25", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string gateway = NetworkHelper.SuggestGateway(full);
            string err = NetworkHelper.SetStaticIp(
                _adapterName,
                full,
                DefaultMask,
                gateway,
                DefaultDns1,
                DefaultDns2);

            if (err != null)
            {
                MessageBox.Show(this, err, "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this,
                "已改为 " + full + "\r\n网关 " + gateway + "\r\nDNS " + DefaultDns1 + " / " + DefaultDns2,
                "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Refresh display from what we just set
            _lblAdapter.Text = "网卡：" + _adapterName + "    当前 " + full;
            UpdatePreview();
        }

        private static Font CreateUiFont(float size)
        {
            string[] candidates = new string[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Tahoma" };
            foreach (string name in candidates)
            {
                try { return new Font(name, size, FontStyle.Regular, GraphicsUnit.Point); }
                catch { }
            }
            return SystemFonts.MessageBoxFont;
        }
    }
}
