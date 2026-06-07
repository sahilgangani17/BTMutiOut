using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BTMultiOut
{
    public class MainForm : Form
    {
        // ── Engine ────────────────────────────────────────────────────────────
        private readonly MultiOutputEngine _engine = new();

        // ── Controls ──────────────────────────────────────────────────────────
        private CheckedListBox _outputList  = null!;
        private ComboBox       _cboSource   = null!;
        private Button         _btnToggle   = null!;
        private Button         _btnRefresh  = null!;
        private Label          _lblStatus   = null!;
        private Panel          _headerPanel = null!;
        private Panel          _bodyPanel   = null!;
        private Panel          _footerPanel = null!;
        private NotifyIcon     _trayIcon    = null!;
        private Label          _lblWarning  = null!;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color C_BG      = Color.FromArgb(15,  17,  26);
        private static readonly Color C_PANEL   = Color.FromArgb(22,  26,  40);
        private static readonly Color C_ACCENT  = Color.FromArgb(0,   200, 160);
        private static readonly Color C_TEXT    = Color.FromArgb(220, 225, 240);
        private static readonly Color C_MUTED   = Color.FromArgb(100, 110, 140);
        private static readonly Color C_DANGER  = Color.FromArgb(255, 80,  80);
        private static readonly Color C_SUCCESS = Color.FromArgb(0,   200, 120);
        private static readonly Color C_WARN    = Color.FromArgb(255, 190, 50);

        // ── State ─────────────────────────────────────────────────────────────
        private bool _running;
        private List<(string Id, string Name)> _devices = new();

        public MainForm()
        {
            InitUI();
            LoadDevices();
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI
        // ═════════════════════════════════════════════════════════════════════

        private void InitUI()
        {
            Text            = "BT MultiOut";
            Size            = new Size(480, 620);
            MinimumSize     = new Size(380, 540);
            BackColor       = C_BG;
            ForeColor       = C_TEXT;
            Font            = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.CenterScreen;

            // ── Header ────────────────────────────────────────────────────────
            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = C_PANEL };
            _headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(C_ACCENT, 2f);
                e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "BT MultiOut", Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = C_ACCENT, AutoSize = true, Location = new Point(20, 12)
            };
            var lblSub = new Label
            {
                Text = "Route system audio to multiple Bluetooth devices with low latency",
                Font = new Font("Segoe UI", 8.5f), ForeColor = C_MUTED,
                AutoSize = false, Width = 430, Height = 18, Location = new Point(22, 50)
            };
            _headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Body ──────────────────────────────────────────────────────────
            _bodyPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(20, 14, 20, 8) };

            // Source selector
            var lblSource = MakeLabel("CAPTURE SOURCE", 8f, C_MUTED, bold: true);
            lblSource.Location = new Point(20, 14);

            var lblSourceHint = MakeLabel(
                "The device Windows plays audio to. BT MultiOut listens here and forwards to outputs below.",
                8f, C_MUTED);
            lblSourceHint.AutoSize = false;
            lblSourceHint.Height = 32;

            _cboSource = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = C_PANEL,
                ForeColor     = C_TEXT,
                Font          = new Font("Segoe UI", 10f),
                FlatStyle     = FlatStyle.Flat,
                Height        = 30,
            };
            _cboSource.SelectedIndexChanged += (_, _) => CheckSourceOutputOverlap();

            // Warning label
            _lblWarning = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_WARN,
                AutoSize  = false,
                Height    = 22,
                Visible   = false
            };

            // Output list
            var lblOutputs = MakeLabel("OUTPUT DEVICES  (check to receive audio)", 8f, C_MUTED, bold: true);

            _outputList = new CheckedListBox
            {
                BackColor    = C_PANEL,
                ForeColor    = C_TEXT,
                Font         = new Font("Segoe UI", 10f),
                BorderStyle  = BorderStyle.None,
                CheckOnClick = true,
                DrawMode     = DrawMode.OwnerDrawFixed,
                ItemHeight   = 30,
            };
            _outputList.DrawItem        += DrawDeviceItem;
            _outputList.ItemCheck       += (_, _) => BeginInvoke(new Action(CheckSourceOutputOverlap));

            _btnRefresh = new Button
            {
                Text      = "⟳  Refresh",
                Size      = new Size(100, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 40, 60),
                ForeColor = C_MUTED,
                Font      = new Font("Segoe UI", 8.5f),
                Cursor    = Cursors.Hand,
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(50, 60, 90);
            _btnRefresh.Click += (_, _) => LoadDevices();

            _lblStatus = new Label
            {
                Text      = "● Idle — select devices and press Start",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = C_MUTED,
                AutoSize  = false,
                Height    = 22,
                Dock      = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            _bodyPanel.Controls.AddRange(new Control[]
            {
                lblSource, lblSourceHint, _cboSource,
                _lblWarning, lblOutputs, _outputList, _btnRefresh, _lblStatus
            });
            _bodyPanel.Resize += LayoutBody;

            // ── Footer ────────────────────────────────────────────────────────
            _footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = C_PANEL };
            _footerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(C_ACCENT, 2f);
                e.Graphics.DrawLine(pen, 0, 0, _footerPanel.Width, 0);
            };

            _btnToggle = new Button
            {
                Text      = "▶  Start Streaming",
                Size      = new Size(200, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_ACCENT,
                ForeColor = Color.FromArgb(10, 20, 30),
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
            };
            _btnToggle.FlatAppearance.BorderSize         = 0;
            _btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 220, 180);
            _btnToggle.Click += ToggleStreaming;
            RoundButton(_btnToggle, 8);

            var lblHint = new Label
            {
                Text      = "Tip: set your source to VB-CABLE\nto avoid double-play on speakers.",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = C_MUTED,
                AutoSize  = false,
                Width     = 220,
                Height    = 36,
                Location  = new Point(230, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _footerPanel.Controls.AddRange(new Control[] { _btnToggle, lblHint });
            _footerPanel.Resize += (_, _) =>
                _btnToggle.Location = new Point(20, (_footerPanel.Height - _btnToggle.Height) / 2);

            // ── Tray ──────────────────────────────────────────────────────────
            _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Visible = false, Text = "BT MultiOut" };
            _trayIcon.DoubleClick += (_, _) => RestoreWindow();
            var tray = new ContextMenuStrip();
            tray.Items.Add("Show",  null, (_, _) => RestoreWindow());
            tray.Items.Add("Stop",  null, (_, _) => { if (_running) StopEngine(); });
            tray.Items.Add("-");
            tray.Items.Add("Quit",  null, (_, _) => { StopEngine(); Application.Exit(); });
            _trayIcon.ContextMenuStrip = tray;

            Resize      += OnFormResize;
            FormClosing += OnFormClosing;

            Controls.Add(_bodyPanel);
            Controls.Add(_footerPanel);
            Controls.Add(_headerPanel);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Layout
        // ═════════════════════════════════════════════════════════════════════

        private void LayoutBody(object? sender, EventArgs e)
        {
            int pad = 20, w = _bodyPanel.ClientSize.Width - pad * 2;

            // Source section
            var lblSource = _bodyPanel.Controls[0]; // "CAPTURE SOURCE"
            lblSource.Location = new Point(pad, 14);

            var lblHint = _bodyPanel.Controls[1];
            lblHint.Location = new Point(pad, 34);
            lblHint.Width    = w;

            _cboSource.Location = new Point(pad, 68);
            _cboSource.Width    = w;

            _lblWarning.Location = new Point(pad, 100);
            _lblWarning.Width    = w;

            // Output section
            var lblOut = _bodyPanel.Controls[4]; // "OUTPUT DEVICES"
            lblOut.Location = new Point(pad, 126);

            _btnRefresh.Location = new Point(_bodyPanel.ClientSize.Width - pad - _btnRefresh.Width, 122);

            int listTop = 148;
            _outputList.Location = new Point(pad, listTop);
            _outputList.Size     = new Size(w, _bodyPanel.ClientSize.Height - listTop - 28);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Drawing
        // ═════════════════════════════════════════════════════════════════════

        private void DrawDeviceItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel  = (e.State & DrawItemState.Selected) != 0;
            bool chk  = _outputList.GetItemChecked(e.Index);
            e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(30,40,60) : C_PANEL), e.Bounds);

            var box = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 7, 16, 16);
            e.Graphics.DrawRectangle(new Pen(chk ? C_ACCENT : C_MUTED, 1.5f), box);
            if (chk)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(C_ACCENT, 2f);
                e.Graphics.DrawLines(p, new[]
                {
                    new Point(box.X+3, box.Y+8), new Point(box.X+6, box.Y+12), new Point(box.X+13, box.Y+4)
                });
            }

            var tr = new Rectangle(e.Bounds.X + 34, e.Bounds.Y, e.Bounds.Width - 34, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, _outputList.Items[e.Index]?.ToString() ?? "",
                new Font("Segoe UI", 10f), tr, chk ? C_TEXT : C_MUTED,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            using var sep = new Pen(Color.FromArgb(35, 45, 65), 1);
            e.Graphics.DrawLine(sep, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Device loading
        // ═════════════════════════════════════════════════════════════════════

        private void LoadDevices()
        {
            _devices = MultiOutputEngine.GetOutputDevices();
            var (defId, defName) = MultiOutputEngine.GetDefaultOutputDevice();

            // Source combo
            _cboSource.Items.Clear();
            int defIdx = 0;
            for (int i = 0; i < _devices.Count; i++)
            {
                var (id, name) = _devices[i];
                string label = id == defId ? $"{name}  ★ (Windows default)" : name;
                _cboSource.Items.Add(label);
                if (id == defId) defIdx = i;
            }
            if (_cboSource.Items.Count > 0) _cboSource.SelectedIndex = defIdx;

            // Output list
            _outputList.Items.Clear();
            foreach (var (_, name) in _devices)
                _outputList.Items.Add(name, false);

            SetStatus($"Found {_devices.Count} audio output device(s).", C_MUTED);
            CheckSourceOutputOverlap();
        }

        // ── Warn if the capture source is also checked as an output ───────────
        private void CheckSourceOutputOverlap()
        {
            if (_cboSource.SelectedIndex < 0 || _cboSource.SelectedIndex >= _devices.Count)
            { _lblWarning.Visible = false; return; }

            var (srcId, srcName) = _devices[_cboSource.SelectedIndex];
            bool overlap = false;
            for (int i = 0; i < _outputList.Items.Count; i++)
            {
                if (_outputList.GetItemChecked(i) && _devices[i].Id == srcId)
                { overlap = true; break; }
            }

            if (overlap)
            {
                _lblWarning.Text    = $"⚠  '{srcName}' is both the source AND an output → double-play!  Uncheck it below.";
                _lblWarning.Visible = true;
            }
            else
            {
                _lblWarning.Visible = false;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Streaming
        // ═════════════════════════════════════════════════════════════════════

        private void ToggleStreaming(object? sender, EventArgs e)
        {
            if (!_running) StartEngine(); else StopEngine();
        }

        private void StartEngine()
        {
            var selected = new List<(string, string)>();
            for (int i = 0; i < _outputList.Items.Count; i++)
                if (_outputList.GetItemChecked(i))
                    selected.Add(_devices[i]);

            if (selected.Count == 0) { SetStatus("⚠  Check at least one output device.", C_DANGER); return; }
            if (_cboSource.SelectedIndex < 0) { SetStatus("⚠  Select a capture source.", C_DANGER); return; }

            string captureId = _devices[_cboSource.SelectedIndex].Id;

            try
            {
                _engine.Start(captureId, selected);
                _running = true;
                _btnToggle.Text      = "■  Stop Streaming";
                _btnToggle.BackColor = C_DANGER;
                _btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 110, 110);
                _cboSource.Enabled   = false;
                _outputList.Enabled  = false;
                _btnRefresh.Enabled  = false;
                SetStatus($"● Streaming → {selected.Count} device(s)  |  low-latency mode", C_SUCCESS);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", C_DANGER);
            }
        }

        private void StopEngine()
        {
            try { _engine.Stop(); } catch { }
            _running = false;
            _btnToggle.Text      = "▶  Start Streaming";
            _btnToggle.BackColor = C_ACCENT;
            _btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 220, 180);
            _cboSource.Enabled   = true;
            _outputList.Enabled  = true;
            _btnRefresh.Enabled  = true;
            SetStatus("● Idle — select devices and press Start", C_MUTED);
        }

        private void SetStatus(string text, Color color)
        { _lblStatus.Text = text; _lblStatus.ForeColor = color; }

        // ═════════════════════════════════════════════════════════════════════
        // Tray / close
        // ═════════════════════════════════════════════════════════════════════

        private void OnFormResize(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            Hide();
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(2000, "BT MultiOut",
                _running ? "Still streaming in background." : "Minimised to tray.", ToolTipIcon.Info);
        }
        private void RestoreWindow() { Show(); WindowState = FormWindowState.Normal; _trayIcon.Visible = false; }
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        { StopEngine(); _trayIcon.Visible = false; _engine.Dispose(); }

        // ═════════════════════════════════════════════════════════════════════
        // Helpers
        // ═════════════════════════════════════════════════════════════════════

        private static Label MakeLabel(string text, float size, Color color, bool bold = false) =>
            new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                AutoSize  = true
            };

        private static void RoundButton(Button btn, int r)
        {
            void Apply() => btn.Region = new Region(RoundRect(new Rectangle(0, 0, btn.Width, btn.Height), r));
            Apply();
            btn.Resize += (_, _) => Apply();
        }

        private static GraphicsPath RoundRect(Rectangle rc, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(rc.X,           rc.Y,            r*2, r*2, 180, 90);
            p.AddArc(rc.Right-r*2,   rc.Y,            r*2, r*2, 270, 90);
            p.AddArc(rc.Right-r*2,   rc.Bottom-r*2,   r*2, r*2,   0, 90);
            p.AddArc(rc.X,           rc.Bottom-r*2,   r*2, r*2,  90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
