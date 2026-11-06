using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EasyIP
{
    public sealed class MainForm : Form
    {
        private const string DefaultIpPrefix = "192.168.";
        private const string DefaultMask = "255.255.254.0";
        private const string DefaultDns1 = "192.168.106.3";
        private const string DefaultDns2 = "223.5.5.5";

        private readonly ContactStore _store;
        private bool _syncingGateway;

        private ComboBox _cmbAdapter;
        private TextBox _txtIp;
        private TextBox _txtMask;
        private TextBox _txtGateway;
        private TextBox _txtDns1;
        private TextBox _txtDns2;
        private CheckBox _chkAutoGateway;
        private ListView _lvContacts;
        private TextBox _txtContactName;
        private TextBox _txtContactIp;
        private TextBox _txtContactNote;
        private Label _lblStatus;
        private Label _lblDbPath;

        public MainForm()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _store = new ContactStore(Path.Combine(baseDir, "contacts.db"));
            BuildUi();
            LoadAdapters();
            RefreshContactList();
            ApplyDefaults();
        }

        private void BuildUi()
        {
            Text = "EasyIP — 便携 IP 配置工具";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(720, 560);
            Size = new Size(780, 620);
            Font = CreateUiFont();
            BackColor = Color.FromArgb(245, 247, 250);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            Controls.Add(root);

            GroupBox gbNet = new GroupBox
            {
                Text = "网络配置",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
            };
            root.Controls.Add(gbNet, 0, 0);

            TableLayoutPanel net = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
            };
            net.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            net.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            net.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            net.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 5; i++)
                net.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            gbNet.Controls.Add(net);

            net.Controls.Add(MakeLabel("网卡"), 0, 0);
            _cmbAdapter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 6, 3, 3),
            };
            net.Controls.Add(_cmbAdapter, 1, 0);
            net.SetColumnSpan(_cmbAdapter, 2);

            Button btnRefresh = new Button
            {
                Text = "刷新",
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 4, 3, 4),
            };
            btnRefresh.Click += delegate { LoadAdapters(); };
            net.Controls.Add(btnRefresh, 3, 0);

            net.Controls.Add(MakeLabel("IP 地址"), 0, 1);
            _txtIp = MakeTextBox();
            _txtIp.TextChanged += OnIpChanged;
            net.Controls.Add(_txtIp, 1, 1);

            net.Controls.Add(MakeLabel("子网掩码"), 2, 1);
            _txtMask = MakeTextBox();
            net.Controls.Add(_txtMask, 3, 1);

            net.Controls.Add(MakeLabel("网关"), 0, 2);
            _txtGateway = MakeTextBox();
            net.Controls.Add(_txtGateway, 1, 2);

            _chkAutoGateway = new CheckBox
            {
                Text = "自动网关 (.1)",
                Checked = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 8, 3, 3),
            };
            _chkAutoGateway.CheckedChanged += delegate
            {
                if (_chkAutoGateway.Checked) SyncGatewayFromIp();
            };
            net.Controls.Add(_chkAutoGateway, 2, 2);
            net.SetColumnSpan(_chkAutoGateway, 2);

            net.Controls.Add(MakeLabel("首选 DNS"), 0, 3);
            _txtDns1 = MakeTextBox();
            net.Controls.Add(_txtDns1, 1, 3);

            net.Controls.Add(MakeLabel("备用 DNS"), 2, 3);
            _txtDns2 = MakeTextBox();
            net.Controls.Add(_txtDns2, 3, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            net.Controls.Add(actions, 0, 4);
            net.SetColumnSpan(actions, 4);

            Button btnApply = MakeButton("应用静态 IP", Color.FromArgb(25, 118, 210));
            btnApply.Click += OnApplyStatic;
            actions.Controls.Add(btnApply);

            Button btnDhcp = MakeButton("恢复 DHCP", Color.FromArgb(97, 97, 97));
            btnDhcp.Click += OnApplyDhcp;
            actions.Controls.Add(btnDhcp);

            Button btnDefaults = MakeButton("恢复默认值", Color.FromArgb(67, 160, 71));
            btnDefaults.Click += delegate { ApplyDefaults(); };
            actions.Controls.Add(btnDefaults);

            Button btnReadCurrent = MakeButton("读取当前 IP", Color.FromArgb(123, 31, 162));
            btnReadCurrent.Click += OnReadCurrentIp;
            actions.Controls.Add(btnReadCurrent);

            GroupBox gbContacts = new GroupBox
            {
                Text = "联系人数据库（电脑名 / 备注 ↔ IP）",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
            };
            root.Controls.Add(gbContacts, 0, 1);

            TableLayoutPanel contactRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            contactRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contactRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            gbContacts.Controls.Add(contactRoot);

            _lvContacts = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
            };
            _lvContacts.Columns.Add("名称", 160);
            _lvContacts.Columns.Add("IP 地址", 140);
            _lvContacts.Columns.Add("备注", 200);
            _lvContacts.Columns.Add("更新时间", 150);
            _lvContacts.DoubleClick += delegate { ApplySelectedContact(); };
            _lvContacts.SelectedIndexChanged += OnContactSelected;
            contactRoot.Controls.Add(_lvContacts, 0, 0);

            Panel editPanel = new Panel { Dock = DockStyle.Fill };
            contactRoot.Controls.Add(editPanel, 0, 1);

            TableLayoutPanel edit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
            };
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            edit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            edit.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            edit.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            editPanel.Controls.Add(edit);

            edit.Controls.Add(MakeLabel("名称"), 0, 0);
            _txtContactName = MakeTextBox();
            _txtContactName.Text = Environment.MachineName;
            edit.Controls.Add(_txtContactName, 1, 0);

            edit.Controls.Add(MakeLabel("IP"), 2, 0);
            _txtContactIp = MakeTextBox();
            edit.Controls.Add(_txtContactIp, 3, 0);

            edit.Controls.Add(MakeLabel("备注"), 4, 0);
            _txtContactNote = MakeTextBox();
            edit.Controls.Add(_txtContactNote, 5, 0);

            FlowLayoutPanel contactBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
            };
            edit.Controls.Add(contactBtns, 0, 1);
            edit.SetColumnSpan(contactBtns, 6);

            Button btnAdd = MakeButton("新增 / 保存", Color.FromArgb(25, 118, 210));
            btnAdd.Click += OnSaveContact;
            contactBtns.Controls.Add(btnAdd);

            Button btnUse = MakeButton("套用到上方", Color.FromArgb(0, 151, 167));
            btnUse.Click += delegate { ApplySelectedContact(); };
            contactBtns.Controls.Add(btnUse);

            Button btnFillFromForm = MakeButton("从上方填入", Color.FromArgb(67, 160, 71));
            btnFillFromForm.Click += delegate
            {
                _txtContactIp.Text = _txtIp.Text.Trim();
                if (string.IsNullOrWhiteSpace(_txtContactName.Text))
                    _txtContactName.Text = Environment.MachineName;
            };
            contactBtns.Controls.Add(btnFillFromForm);

            Button btnDelete = MakeButton("删除选中", Color.FromArgb(198, 40, 40));
            btnDelete.Click += OnDeleteContact;
            contactBtns.Controls.Add(btnDelete);

            Button btnClearSel = MakeButton("清空编辑", Color.FromArgb(97, 97, 97));
            btnClearSel.Click += delegate
            {
                _lvContacts.SelectedItems.Clear();
                _txtContactName.Text = Environment.MachineName;
                _txtContactIp.Clear();
                _txtContactNote.Clear();
            };
            contactBtns.Controls.Add(btnClearSel);

            Panel statusPanel = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(statusPanel, 0, 2);

            _lblStatus = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 420,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "就绪（修改 IP 需要管理员权限，本程序已请求提权）",
            };
            statusPanel.Controls.Add(_lblStatus);

            _lblDbPath = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Text = "数据库: " + _store.DbPath,
            };
            statusPanel.Controls.Add(_lblDbPath);
        }

        private static Font CreateUiFont()
        {
            string[] candidates = new string[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Tahoma" };
            foreach (string name in candidates)
            {
                try
                {
                    return new Font(name, 9F, FontStyle.Regular, GraphicsUnit.Point);
                }
                catch
                {
                }
            }
            return SystemFonts.MessageBoxFont;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3),
            };
        }

        private static TextBox MakeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 6, 3, 3),
            };
        }

        private static Button MakeButton(string text, Color back)
        {
            Button b = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(100, 30),
                Margin = new Padding(3, 4, 6, 4),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void ApplyDefaults()
        {
            _txtIp.Text = DefaultIpPrefix;
            _txtMask.Text = DefaultMask;
            _txtDns1.Text = DefaultDns1;
            _txtDns2.Text = DefaultDns2;
            _chkAutoGateway.Checked = true;
            SyncGatewayFromIp();
            SetStatus("已恢复默认值：掩码 " + DefaultMask + "，DNS " + DefaultDns1 + " / " + DefaultDns2);
        }

        private void LoadAdapters()
        {
            string selected = null;
            AdapterInfo current = _cmbAdapter.SelectedItem as AdapterInfo;
            if (current != null) selected = current.Name;

            _cmbAdapter.Items.Clear();
            foreach (AdapterInfo a in NetworkHelper.GetAdapters())
                _cmbAdapter.Items.Add(a);
            _cmbAdapter.DisplayMember = "Display";

            if (_cmbAdapter.Items.Count > 0)
            {
                int idx = 0;
                if (!string.IsNullOrEmpty(selected))
                {
                    for (int i = 0; i < _cmbAdapter.Items.Count; i++)
                    {
                        if (string.Equals(((AdapterInfo)_cmbAdapter.Items[i]).Name, selected, StringComparison.OrdinalIgnoreCase))
                        {
                            idx = i;
                            break;
                        }
                    }
                }
                _cmbAdapter.SelectedIndex = idx;
            }
            SetStatus("已刷新网卡列表，共 " + _cmbAdapter.Items.Count + " 个");
        }

        private void OnIpChanged(object sender, EventArgs e)
        {
            if (_chkAutoGateway.Checked)
                SyncGatewayFromIp();
        }

        private void SyncGatewayFromIp()
        {
            if (_syncingGateway) return;
            _syncingGateway = true;
            try
            {
                string gw = NetworkHelper.SuggestGateway(_txtIp.Text);
                if (!string.IsNullOrEmpty(gw))
                    _txtGateway.Text = gw;
            }
            finally
            {
                _syncingGateway = false;
            }
        }

        private string SelectedAdapterName()
        {
            AdapterInfo ai = _cmbAdapter.SelectedItem as AdapterInfo;
            return ai == null ? null : ai.Name;
        }

        private void OnApplyStatic(object sender, EventArgs e)
        {
            string adapter = SelectedAdapterName();
            string err = NetworkHelper.SetStaticIp(
                adapter,
                _txtIp.Text,
                _txtMask.Text,
                _txtGateway.Text,
                _txtDns1.Text,
                _txtDns2.Text);
            if (err != null)
            {
                MessageBox.Show(this, err, "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("设置失败");
                return;
            }
            MessageBox.Show(this,
                "已应用静态 IP：\r\n" +
                _txtIp.Text.Trim() + "\r\n掩码 " + _txtMask.Text.Trim() +
                "\r\n网关 " + _txtGateway.Text.Trim() +
                "\r\nDNS " + _txtDns1.Text.Trim() + " / " + _txtDns2.Text.Trim(),
                "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("静态 IP 已应用 → " + _txtIp.Text.Trim());
        }

        private void OnApplyDhcp(object sender, EventArgs e)
        {
            string err = NetworkHelper.SetDhcp(SelectedAdapterName());
            if (err != null)
            {
                MessageBox.Show(this, err, "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("DHCP 失败");
                return;
            }
            MessageBox.Show(this, "已切换为自动获取 IP / DNS（DHCP）。", "成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("已恢复 DHCP");
        }

        private void OnReadCurrentIp(object sender, EventArgs e)
        {
            string adapter = SelectedAdapterName();
            if (string.IsNullOrEmpty(adapter))
            {
                MessageBox.Show(this, "请先选择网卡。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string ip = NetworkHelper.GetCurrentIpv4(adapter);
            if (string.IsNullOrEmpty(ip))
            {
                SetStatus("未读取到 IPv4 地址");
                MessageBox.Show(this, "该网卡当前没有 IPv4 地址。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _txtIp.Text = ip;
            if (_chkAutoGateway.Checked) SyncGatewayFromIp();
            _txtContactIp.Text = ip;
            SetStatus("已读取当前 IP：" + ip);
        }

        private void RefreshContactList()
        {
            _lvContacts.Items.Clear();
            foreach (ContactRecord c in _store.Items)
            {
                ListViewItem item = new ListViewItem(c.Name ?? "");
                item.SubItems.Add(c.IpAddress ?? "");
                item.SubItems.Add(c.Note ?? "");
                item.SubItems.Add(c.UpdatedAt ?? "");
                item.Tag = c.Id;
                _lvContacts.Items.Add(item);
            }
        }

        private void OnContactSelected(object sender, EventArgs e)
        {
            if (_lvContacts.SelectedItems.Count == 0) return;
            string id = _lvContacts.SelectedItems[0].Tag as string;
            ContactRecord c = _store.FindById(id);
            if (c == null) return;
            _txtContactName.Text = c.Name;
            _txtContactIp.Text = c.IpAddress;
            _txtContactNote.Text = c.Note;
        }

        private void ApplySelectedContact()
        {
            if (_lvContacts.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "请先在列表中选择一条记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string id = _lvContacts.SelectedItems[0].Tag as string;
            ContactRecord c = _store.FindById(id);
            if (c == null) return;
            _txtIp.Text = c.IpAddress;
            if (_chkAutoGateway.Checked) SyncGatewayFromIp();
            SetStatus("已套用联系人：" + c.Name + " → " + c.IpAddress);
        }

        private void OnSaveContact(object sender, EventArgs e)
        {
            string name = _txtContactName.Text.Trim();
            string ip = _txtContactIp.Text.Trim();
            string note = _txtContactNote.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "请填写名称（如电脑名 desktop-01）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string normalized;
            if (!NetworkHelper.TryParseIpv4(ip, out normalized))
            {
                MessageBox.Show(this, "请填写正确的 IP 地址。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_lvContacts.SelectedItems.Count > 0)
            {
                string id = _lvContacts.SelectedItems[0].Tag as string;
                ContactRecord existing = _store.FindById(id);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.IpAddress = normalized;
                    existing.Note = note;
                    _store.Update(existing);
                    RefreshContactList();
                    SetStatus("已更新联系人：" + name);
                    return;
                }
            }

            _store.Add(name, normalized, note);
            RefreshContactList();
            SetStatus("已新增联系人：" + name + " — " + normalized);
        }

        private void OnDeleteContact(object sender, EventArgs e)
        {
            if (_lvContacts.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "请先选择要删除的记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string id = _lvContacts.SelectedItems[0].Tag as string;
            ContactRecord c = _store.FindById(id);
            string title = c == null ? "该记录" : (c.Name + " / " + c.IpAddress);
            if (MessageBox.Show(this, "确定删除 " + title + " ？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _store.Delete(id);
            RefreshContactList();
            SetStatus("已删除：" + title);
        }

        private void SetStatus(string text)
        {
            _lblStatus.Text = text;
        }
    }
}
