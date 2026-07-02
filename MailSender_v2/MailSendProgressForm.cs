using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MailSender_v2.Mailing;

namespace MailSender_v2
{
    internal sealed class MailSendProgressForm : Form
    {
        private readonly Label _summaryLabel = new Label();
        private readonly Label _currentLabel = new Label();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _closeButton = new Button();
        private readonly Dictionary<string, int> _rowIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private bool _isSending = true;

        public MailSendProgressForm()
        {
            Text = "발송 현황";
            StartPosition = FormStartPosition.CenterParent;
            Width = 760;
            Height = 520;
            MinimumSize = new Size(620, 420);
            Font = new Font("맑은 고딕", 9F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(14),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            _summaryLabel.AutoSize = false;
            _summaryLabel.Dock = DockStyle.Fill;
            _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(_summaryLabel, 0, 0);

            _currentLabel.AutoSize = false;
            _currentLabel.Dock = DockStyle.Fill;
            _currentLabel.TextAlign = ContentAlignment.MiddleLeft;
            _currentLabel.Text = "현재 발송: -";
            root.Controls.Add(_currentLabel, 0, 1);

            ConfigureGrid();
            root.Controls.Add(_grid, 0, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
            };
            _closeButton.Text = "닫기";
            _closeButton.Width = 110;
            _closeButton.Height = 30;
            _closeButton.Enabled = false;
            _closeButton.Click += (sender, args) => Close();
            buttonPanel.Controls.Add(_closeButton);
            root.Controls.Add(buttonPanel, 0, 3);

            UpdateSummary();
        }

        public void InitializeTargets(IEnumerable<RecipientListItem> dbRecipients, IEnumerable<RecipientListItem> manualRecipients)
        {
            AddTargets("DB", dbRecipients);
            AddTargets("수동", manualRecipients);
            UpdateSummary();
        }

        public void UpdateProgress(MailSendProgressUpdate update)
        {
            if (update == null || update.Recipient == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action<MailSendProgressUpdate>(UpdateProgress), update);
                return;
            }

            var key = NormalizeEmail(update.Recipient.Email);
            if (!_rowIndexes.TryGetValue(key, out var rowIndex) || rowIndex < 0 || rowIndex >= _grid.Rows.Count)
            {
                return;
            }

            var row = _grid.Rows[rowIndex];
            row.Cells["StatusColumn"].Value = ToStatusText(update.State);
            row.Cells["MessageColumn"].Value = update.Message ?? "";
            ApplyRowStyle(row, update.State);

            if (update.State == MailSendProgressState.Sending)
            {
                _currentLabel.Text = $"현재 발송: {update.Recipient.Email}";
            }

            UpdateSummary();
        }

        public void SetCurrentStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetCurrentStatus), message);
                return;
            }

            _currentLabel.Text = $"현재 상태: {message}";
        }

        public void MarkCompleted(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(MarkCompleted), message);
                return;
            }

            _isSending = false;
            _closeButton.Enabled = true;
            _currentLabel.Text = $"현재 상태: {message}";
            UpdateSummary();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isSending && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show(this, "발송 중에는 닫을 수 없습니다. 발송은 계속 진행 중입니다.", "발송 진행 중", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            base.OnFormClosing(e);
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TypeColumn", HeaderText = "구분", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmailColumn", HeaderText = "이메일", FillWeight = 38 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusColumn", HeaderText = "상태", FillWeight = 17 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MessageColumn", HeaderText = "메시지", FillWeight = 30 });
        }

        private void AddTargets(string type, IEnumerable<RecipientListItem> recipients)
        {
            foreach (var recipient in recipients ?? Enumerable.Empty<RecipientListItem>())
            {
                var key = NormalizeEmail(recipient.Email);
                if (key.Length == 0 || _rowIndexes.ContainsKey(key))
                {
                    continue;
                }

                var rowIndex = _grid.Rows.Add(type, recipient.Email, "대기", "");
                _rowIndexes[key] = rowIndex;
            }
        }

        private void UpdateSummary()
        {
            var total = _grid.Rows.Count;
            var success = CountStatus("성공");
            var failure = CountStatus("실패");
            var completed = success + failure;
            _summaryLabel.Text = $"전체 {total:N0}건 / 완료 {completed:N0}건 / 성공 {success:N0}건 / 실패 {failure:N0}건";
        }

        private int CountStatus(string status)
        {
            return _grid.Rows
                .Cast<DataGridViewRow>()
                .Count(row => string.Equals(Convert.ToString(row.Cells["StatusColumn"].Value), status, StringComparison.Ordinal));
        }

        private static void ApplyRowStyle(DataGridViewRow row, MailSendProgressState state)
        {
            switch (state)
            {
                case MailSendProgressState.Success:
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 110, 45);
                    break;
                case MailSendProgressState.Failure:
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(190, 40, 40);
                    break;
                default:
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(20, 80, 160);
                    break;
            }
        }

        private static string ToStatusText(MailSendProgressState state)
        {
            switch (state)
            {
                case MailSendProgressState.Success:
                    return "성공";
                case MailSendProgressState.Failure:
                    return "실패";
                default:
                    return "발송 중";
            }
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? "").Trim().ToLowerInvariant();
        }
    }
}
