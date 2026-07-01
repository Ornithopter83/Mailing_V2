using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MailSender_v2.Config;
using MailSender_v2.Data;
using MailSender_v2.Mailing;
using MailSender_v2.Upload;
using Newtonsoft.Json;

namespace MailSender_v2
{
    public partial class MainForm : Form
    {
        private readonly TextBox _uploadLogTextBox = new TextBox();
        private readonly TextBox _uploadFilePathTextBox = new TextBox();
        private readonly DataGridView _uploadResultGrid = new DataGridView();
        private readonly TextBox _sendLogTextBox = new TextBox();
        private readonly Label _statusLabel = new Label();
        private readonly Label _uploadDateLabel = new Label();
        private readonly Label _selectedCountLabel = new Label();
        private readonly Label _uploadInfoDateValueLabel = CreateLabel("-");
        private readonly Label _uploadInfoFileNameValueLabel = CreateLabel("-");
        private readonly Label _uploadInfoSheetCountValueLabel = CreateLabel("-");
        private readonly Label _uploadInfoSheetNameValueLabel = CreateLabel("-");
        private readonly Label _uploadInfoRowCountValueLabel = CreateLabel("-");
        private readonly DataGridView _recipientGrid = new DataGridView();
        private readonly List<RecipientListItem> _recipientRows = new List<RecipientListItem>();
        private readonly List<string> _activityLogs = new List<string>();
        private readonly List<string> _attachmentPaths = new List<string>();
        private Button _uploadStartButton;
        private Button _saveTextButton;
        private Button _saveExcelButton;
        private Button _queryRecipientsButton;
        private Button _manualCompleteButton;
        private Button _sendButton;
        private TextBox _toTextBox;
        private TextBox _subjectTextBox;
        private TextBox _ccTextBox;
        private RichTextBox _bodyTextBox;
        private ListView _attachmentListView;
        private NumericUpDown _sendCountNumeric;
        private RadioButton _sortDescRadio;
        private CheckBox _excludeSentCheckBox;
        private CheckBox _excludeBlockedCheckBox;
        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private AppSettings _settings = new AppSettings();
        private UploadProcessResult _lastUploadResult;

        public MainForm()
        {
            _settings = AppSettings.LoadOrCreate(_configPath);
            InitializeComponent();
            BuildRuntimeLayout();
        }

        private void InitializeComponent()
        {
            Text = "입찰공고 메일링 시스템";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1280, 760);
            Size = new Size(1360, 820);
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Load += MainForm_Load;
        }

        private void BuildRuntimeLayout()
        {
            SuspendLayout();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
            };

            tabs.TabPages.Add(CreateUploadTab());
            tabs.TabPages.Add(CreateMailTab());

            var statusPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
            };
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            _statusLabel.Text = "상태: 대기";
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            _uploadDateLabel.Text = "업로드 일시: -";
            _uploadDateLabel.Dock = DockStyle.Fill;
            _uploadDateLabel.TextAlign = ContentAlignment.MiddleRight;

            statusPanel.Controls.Add(_statusLabel, 0, 0);
            statusPanel.Controls.Add(_uploadDateLabel, 1, 0);

            root.Controls.Add(tabs, 0, 0);
            root.Controls.Add(statusPanel, 0, 1);
            Controls.Add(root);

            ResumeLayout(false);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await InitializeSupabaseAsync();
        }

        private async Task InitializeSupabaseAsync()
        {
            try
            {
                _settings = AppSettings.LoadOrCreate(_configPath);
                AppendUploadLog($"[시작] 설정 파일 로드: {_configPath}");

                var initializer = new DatabaseInitializer(_settings.Supabase);
                var result = await initializer.CheckAsync(CancellationToken.None);

                if (!result.IsConfigured)
                {
                    _statusLabel.Text = "상태: Supabase 설정 필요";
                    AppendUploadLog("[Supabase] SupabaseUrl 또는 SupabaseAnonKey가 설정되지 않았습니다.");
                    AppendUploadLog("[Supabase] config.json에 Supabase.Url, Supabase.AnonKey를 입력하세요.");
                    return;
                }

                foreach (var tableResult in result.TableResults)
                {
                    var state = tableResult.IsAccessible ? "성공" : "실패";
                    AppendUploadLog($"[Supabase] {tableResult.TableName}: {state} - {tableResult.Message}");
                }

                _statusLabel.Text = result.IsSuccessful
                    ? "상태: Supabase 연결 확인 완료"
                    : "상태: Supabase 연결 확인 실패";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "상태: Supabase 초기화 오류";
                AppendUploadLog($"[Supabase] 초기화 오류: {ex.Message}");
                MessageBox.Show(
                    $"Supabase 설정 또는 기본 테이블 확인 중 오류가 발생했습니다.\n\n{ex.Message}",
                    "Supabase 초기화 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void AppendUploadLog(string message)
        {
            if (_uploadLogTextBox.InvokeRequired)
            {
                _uploadLogTextBox.Invoke(new Action<string>(AppendUploadLog), message);
                return;
            }

            AppendActivityLog(message);

            if (_uploadLogTextBox.Text.StartsWith("[대기]", StringComparison.Ordinal))
            {
                _uploadLogTextBox.Clear();
            }

            var builder = new StringBuilder(_uploadLogTextBox.Text);
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(message);
            _uploadLogTextBox.Text = builder.ToString();
            _uploadLogTextBox.SelectionStart = _uploadLogTextBox.TextLength;
            _uploadLogTextBox.ScrollToCaret();
        }

        private void AppendSendLog(string message)
        {
            if (_sendLogTextBox.InvokeRequired)
            {
                _sendLogTextBox.Invoke(new Action<string>(AppendSendLog), message);
                return;
            }

            AppendActivityLog(message);

            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            var builder = new StringBuilder(_sendLogTextBox.Text);
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
            _sendLogTextBox.Text = builder.ToString();
            _sendLogTextBox.SelectionStart = _sendLogTextBox.TextLength;
            _sendLogTextBox.ScrollToCaret();
        }

        private void AppendActivityLog(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lock (_activityLogs)
            {
                _activityLogs.Add(line);
                if (_activityLogs.Count > 1000)
                {
                    _activityLogs.RemoveRange(0, _activityLogs.Count - 1000);
                }
            }
        }

        private void ShowLogWindow()
        {
            string text;
            lock (_activityLogs)
            {
                text = string.Join(Environment.NewLine, _activityLogs);
            }

            using (var form = new Form())
            using (var textBox = new TextBox())
            {
                form.Text = "로그";
                form.StartPosition = FormStartPosition.CenterParent;
                form.Size = new Size(560, 360);
                textBox.Dock = DockStyle.Fill;
                textBox.Multiline = true;
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.ReadOnly = true;
                textBox.Text = text;
                form.Controls.Add(textBox);
                form.ShowDialog(this);
            }
        }

        private TabPage CreateUploadTab()
        {
            var page = new TabPage("1. 목록 추가 (입찰공고 파일 업로드)");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));

            layout.Controls.Add(CreateExcelUploadGroup(), 0, 0);
            layout.Controls.Add(CreateUploadInfoGroup(), 1, 0);
            layout.Controls.Add(CreateUploadResultGroup(), 0, 1);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 1), 2);
            layout.Controls.Add(CreateUploadLogGroup(), 0, 2);
            layout.Controls.Add(CreateUploadActionsGroup(), 1, 2);

            page.Controls.Add(layout);
            return page;
        }

        private GroupBox CreateExcelUploadGroup()
        {
            var group = CreateGroupBox("1. 엑셀 파일 업로드");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            layout.Controls.Add(CreateLabel("파일 경로"), 0, 0);
            _uploadFilePathTextBox.Dock = DockStyle.Fill;

            var selectFileButton = new Button { Text = "파일 선택", Dock = DockStyle.Fill };
            selectFileButton.Click += SelectExcelFileButton_Click;

            _uploadStartButton = new Button { Text = "업로드 및 처리 시작", Dock = DockStyle.Left, Width = 160 };
            _uploadStartButton.Click += async (sender, args) => await StartUploadAsync();

            layout.Controls.Add(_uploadFilePathTextBox, 1, 0);
            layout.Controls.Add(selectFileButton, 2, 0);
            layout.Controls.Add(_uploadStartButton, 1, 1);

            group.Controls.Add(layout);
            return group;
        }

        private GroupBox CreateUploadInfoGroup()
        {
            var group = CreateGroupBox("업로드 정보");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddInfoRow(layout, 0, "업로드 일시", _uploadInfoDateValueLabel);
            AddInfoRow(layout, 1, "파일명", _uploadInfoFileNameValueLabel);
            AddInfoRow(layout, 2, "전체 시트 수", _uploadInfoSheetCountValueLabel);
            AddInfoRow(layout, 3, "처리된 시트", _uploadInfoSheetNameValueLabel);
            AddInfoRow(layout, 4, "전체 데이터 행 수", _uploadInfoRowCountValueLabel);

            group.Controls.Add(layout);
            return group;
        }

        private GroupBox CreateUploadResultGroup()
        {
            var group = CreateGroupBox("2. 업로드 상세 결과");
            _uploadResultGrid.Dock = DockStyle.Fill;
            _uploadResultGrid.AllowUserToAddRows = false;
            _uploadResultGrid.AllowUserToDeleteRows = false;
            _uploadResultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _uploadResultGrid.BackgroundColor = Color.White;
            _uploadResultGrid.RowHeadersVisible = false;
            _uploadResultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            _uploadResultGrid.Columns.Add("Category", "구분");
            _uploadResultGrid.Columns.Add("Description", "설명");
            _uploadResultGrid.Columns.Add("Count", "건수");
            _uploadResultGrid.Columns.Add("Status", "상태");
            _uploadResultGrid.Columns.Add("Memo", "비고");

            UpdateUploadResultGrid(new UploadProcessResult());

            group.Controls.Add(_uploadResultGrid);
            return group;
        }

        private GroupBox CreateUploadLogGroup()
        {
            var group = CreateGroupBox("3. 작업 로그 (최근 50건)");
            _uploadLogTextBox.Dock = DockStyle.Fill;
            _uploadLogTextBox.Multiline = true;
            _uploadLogTextBox.ScrollBars = ScrollBars.Vertical;
            _uploadLogTextBox.ReadOnly = true;
            _uploadLogTextBox.Text = "[대기] 업로드 작업을 시작하면 로그가 표시됩니다.";

            var logButton = new Button
            {
                Text = "전체 로그 보기",
                Width = 120,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            logButton.Left = panel.Width - logButton.Width - 10;
            logButton.Top = 10;
            logButton.Click += (sender, args) => ShowLogWindow();
            logButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _uploadLogTextBox.Top = 48;
            _uploadLogTextBox.Left = 10;
            _uploadLogTextBox.Width = panel.Width - 20;
            _uploadLogTextBox.Height = panel.Height - 58;
            _uploadLogTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel.Controls.Add(logButton);
            panel.Controls.Add(_uploadLogTextBox);

            group.Controls.Add(panel);
            return group;
        }

        private GroupBox CreateUploadActionsGroup()
        {
            var group = CreateGroupBox("4. 작업");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12),
            };
            for (int i = 0; i < 4; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            _saveTextButton = new Button { Text = "결과 텍스트 저장", Dock = DockStyle.Fill, Enabled = false };
            _saveTextButton.Click += SaveTextButton_Click;

            _saveExcelButton = new Button { Text = "결과 엑셀 저장", Dock = DockStyle.Fill, Enabled = false };
            _saveExcelButton.Click += SaveExcelButton_Click;

            layout.Controls.Add(_saveTextButton, 0, 0);
            layout.Controls.Add(_saveExcelButton, 0, 1);
            layout.Controls.Add(new Button { Text = "DB 초기화", Dock = DockStyle.Fill }, 0, 2);
            var logButton = new Button { Text = "로그 보기", Dock = DockStyle.Fill };
            logButton.Click += (sender, args) => ShowLogWindow();
            layout.Controls.Add(logButton, 0, 3);

            group.Controls.Add(layout);
            return group;
        }

        private TabPage CreateMailTab()
        {
            var page = new TabPage("2. 발송 대상 / 메일 작성");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            layout.Controls.Add(CreateMailComposeGroup(), 0, 0);
            layout.Controls.Add(CreateRecipientsGroup(), 1, 0);

            page.Controls.Add(layout);
            return page;
        }

        private GroupBox CreateMailComposeGroup()
        {
            var group = CreateGroupBox("메일 작성");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            layout.Controls.Add(CreateLabel("수신"), 0, 0);
            _toTextBox = new TextBox { Dock = DockStyle.Fill, Text = _settings.DefaultTo };
            layout.Controls.Add(_toTextBox, 1, 0);
            layout.SetColumnSpan(layout.GetControlFromPosition(1, 0), 2);

            layout.Controls.Add(CreateLabel("참조"), 0, 1);
            _ccTextBox = new TextBox { Dock = DockStyle.Fill, Text = _settings.DefaultCc };
            layout.Controls.Add(_ccTextBox, 1, 1);
            layout.SetColumnSpan(layout.GetControlFromPosition(1, 1), 2);

            layout.Controls.Add(CreateLabel("제목"), 0, 2);
            _subjectTextBox = new TextBox { Dock = DockStyle.Fill, Text = _settings.Subject };
            layout.Controls.Add(_subjectTextBox, 1, 2);
            layout.SetColumnSpan(layout.GetControlFromPosition(1, 2), 2);

            layout.Controls.Add(CreateLabel("본문"), 0, 3);
            var bodyPanel = CreateBodyEditorPanel();
            layout.Controls.Add(bodyPanel, 1, 3);
            layout.SetColumnSpan(bodyPanel, 2);

            layout.Controls.Add(CreateLabel("첨부 파일"), 0, 4);
            layout.Controls.Add(CreateAttachmentPanel(), 1, 4);
            layout.Controls.Add(CreateAttachmentButtonsPanel(), 2, 4);

            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            var draftButton = new Button { Text = "임시저장", Width = 120, Height = 36 };
            draftButton.Click += SaveDraftButton_Click;
            var loadButton = new Button { Text = "불러오기", Width = 120, Height = 36 };
            loadButton.Click += LoadDraftButton_Click;
            var previewButton = new Button { Text = "미리보기", Width = 120, Height = 36 };
            previewButton.Click += PreviewButton_Click;
            _sendButton = new Button { Text = "발송하기", Width = 150, Height = 36, BackColor = Color.FromArgb(0, 102, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _sendButton.Click += async (sender, args) => await SendMailAsync();
            bottomPanel.Controls.Add(draftButton);
            bottomPanel.Controls.Add(loadButton);
            bottomPanel.Controls.Add(previewButton);
            bottomPanel.Controls.Add(_sendButton);
            layout.Controls.Add(bottomPanel, 1, 6);
            layout.SetColumnSpan(bottomPanel, 2);

            group.Controls.Add(layout);
            return group;
        }

        private Control CreateBodyEditorPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            toolbar.Controls.Add(new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList });
            ((ComboBox)toolbar.Controls[0]).Items.AddRange(new object[] { "맑은 고딕", "굴림", "Arial" });
            ((ComboBox)toolbar.Controls[0]).SelectedIndex = 0;
            toolbar.Controls.Add(new ComboBox { Width = 64, DropDownStyle = ComboBoxStyle.DropDownList });
            ((ComboBox)toolbar.Controls[1]).Items.AddRange(new object[] { "10", "11", "12", "14", "16" });
            ((ComboBox)toolbar.Controls[1]).SelectedIndex = 0;
            toolbar.Controls.Add(new Button { Text = "B", Width = 32 });
            toolbar.Controls.Add(new Button { Text = "I", Width = 32 });
            toolbar.Controls.Add(new Button { Text = "U", Width = 32 });
            toolbar.Controls.Add(new Button { Text = "링크", Width = 56 });
            var imageButton = new Button { Text = "이미지 삽입", Width = 100 };
            imageButton.Click += InsertImageButton_Click;
            toolbar.Controls.Add(imageButton);

            _bodyTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = _settings.DefaultBodyText,
                BorderStyle = BorderStyle.FixedSingle,
            };

            panel.Controls.Add(toolbar, 0, 0);
            panel.Controls.Add(_bodyTextBox, 0, 1);
            return panel;
        }

        private Control CreateAttachmentPanel()
        {
            _attachmentListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
            };
            _attachmentListView.Columns.Add("파일명", 300);
            _attachmentListView.Columns.Add("크기", 100, HorizontalAlignment.Right);
            return _attachmentListView;
        }

        private Control CreateAttachmentButtonsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            var addButton = new Button { Text = "파일 추가", Dock = DockStyle.Fill };
            addButton.Click += AddAttachmentButton_Click;
            var deleteButton = new Button { Text = "삭제", Dock = DockStyle.Fill };
            deleteButton.Click += DeleteAttachmentButton_Click;
            panel.Controls.Add(addButton, 0, 0);
            panel.Controls.Add(deleteButton, 0, 1);
            return panel;
        }

        private GroupBox CreateRecipientsGroup()
        {
            var group = CreateGroupBox("발송 대상 조회");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));

            layout.Controls.Add(CreateRecipientSearchPanel(), 0, 0);
            layout.Controls.Add(CreateRecipientGrid(), 0, 1);

            _selectedCountLabel.Text = "선택된 대상: 0건";
            _selectedCountLabel.Dock = DockStyle.Fill;
            _selectedCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(_selectedCountLabel, 0, 2);

            layout.Controls.Add(CreateRecipientActionsPanel(), 0, 3);

            group.Controls.Add(layout);
            return group;
        }

        private Control CreateRecipientSearchPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var sortGroup = new GroupBox { Text = "공고일자 정렬", Dock = DockStyle.Fill };
            var sortPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            _sortDescRadio = new RadioButton { Text = "내림차순", Checked = true, Width = 90 };
            sortPanel.Controls.Add(_sortDescRadio);
            sortPanel.Controls.Add(new RadioButton { Text = "오름차순", Width = 90 });
            sortGroup.Controls.Add(sortPanel);

            var countPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            countPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            countPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            countPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            countPanel.Controls.Add(CreateLabel("발송 건수"), 0, 0);
            _sendCountNumeric = new NumericUpDown { Value = 50, Minimum = 1, Maximum = 10000, Dock = DockStyle.Fill };
            countPanel.Controls.Add(_sendCountNumeric, 1, 0);
            countPanel.Controls.Add(CreateLabel("건"), 2, 0);

            panel.Controls.Add(sortGroup, 0, 0);
            panel.Controls.Add(countPanel, 1, 0);
            _excludeSentCheckBox = new CheckBox { Text = "이미 발송된 이메일 제외", Checked = true, Dock = DockStyle.Fill };
            _excludeBlockedCheckBox = new CheckBox { Text = "차단 목록 제외", Checked = true, Dock = DockStyle.Fill };
            _queryRecipientsButton = new Button { Text = "조회", Dock = DockStyle.Right, Width = 120 };
            _queryRecipientsButton.Click += async (sender, args) => await LoadRecipientsAsync();
            panel.Controls.Add(_excludeSentCheckBox, 0, 1);
            panel.Controls.Add(_excludeBlockedCheckBox, 1, 1);
            panel.Controls.Add(_queryRecipientsButton, 1, 2);

            return panel;
        }

        private Control CreateRecipientGrid()
        {
            _recipientGrid.Dock = DockStyle.Fill;
            _recipientGrid.AllowUserToAddRows = false;
            _recipientGrid.AllowUserToDeleteRows = false;
            _recipientGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _recipientGrid.BackgroundColor = Color.White;
            _recipientGrid.RowHeadersVisible = false;
            _recipientGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "", Width = 36, FillWeight = 20 });
            _recipientGrid.Columns.Add("Email", "이메일");
            _recipientGrid.Columns.Add("Agency", "기관명");
            _recipientGrid.Columns.Add("NoticeDate", "공고일자");
            _recipientGrid.CurrentCellDirtyStateChanged += (sender, args) =>
            {
                if (_recipientGrid.IsCurrentCellDirty)
                {
                    _recipientGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _recipientGrid.CellValueChanged += (sender, args) => UpdateSelectedCount();
            return _recipientGrid;
        }

        private Control CreateRecipientActionsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
            };
            for (int i = 0; i < 4; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            var selectAllButton = new Button { Text = "전체 선택", Dock = DockStyle.Fill };
            selectAllButton.Click += (sender, args) => SetAllRecipientsSelected(true);
            var clearButton = new Button { Text = "선택 해제", Dock = DockStyle.Fill };
            clearButton.Click += (sender, args) => SetAllRecipientsSelected(false);
            _manualCompleteButton = new Button { Text = "발송완료 처리", Dock = DockStyle.Fill };
            _manualCompleteButton.Click += async (sender, args) => await MarkSelectedAsSentAsync();
            var logButton = new Button { Text = "로그 보기", Dock = DockStyle.Fill };
            logButton.Click += (sender, args) => ShowLogWindow();
            panel.Controls.Add(selectAllButton, 0, 0);
            panel.Controls.Add(clearButton, 0, 1);
            panel.Controls.Add(_manualCompleteButton, 0, 2);
            panel.Controls.Add(logButton, 0, 3);
            return panel;
        }

        private static GroupBox CreateGroupBox(string text)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
            };
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
            };
        }

        private void SelectExcelFileButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dialog.Title = "입찰공고 메일링 리스트 선택";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _uploadFilePathTextBox.Text = dialog.FileName;
                }
            }
        }

        private async Task StartUploadAsync()
        {
            var filePath = _uploadFilePathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("업로드할 Excel 파일을 선택하세요.", "파일 미선택", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show("선택한 파일이 존재하지 않습니다.", "파일 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(".xlsx 파일만 업로드할 수 있습니다.", "확장자 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetUploadBusy(true);
            try
            {
                _settings = AppSettings.LoadOrCreate(_configPath);
                AppendUploadLog($"[업로드] 파일 읽기 시작: {Path.GetFileName(filePath)}");

                var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var blockedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var canUploadToSupabase = _settings.Supabase.IsConfigured;

                if (canUploadToSupabase)
                {
                    AppendUploadLog("[Supabase] 기존 이메일/차단 목록 조회 중...");
                    var repository = new SupabaseUploadRepository(_settings.Supabase);
                    existingEmails = await repository.GetExistingRecipientEmailsAsync(CancellationToken.None);
                    blockedEmails = await repository.GetBlockedEmailsAsync(CancellationToken.None);
                    AppendUploadLog($"[Supabase] 기존 이메일 {existingEmails.Count:N0}건, 차단 목록 {blockedEmails.Count:N0}건 로드");
                }
                else
                {
                    AppendUploadLog("[Supabase] 설정이 없어 DB 반영 없이 파일 처리 결과만 표시합니다.");
                }

                var processor = new ExcelUploadProcessor();
                var result = processor.Process(filePath, existingEmails, blockedEmails);
                _lastUploadResult = result;

                foreach (var log in result.Logs)
                {
                    AppendUploadLog($"[업로드] {log}");
                }

                if (canUploadToSupabase)
                {
                    AppendUploadLog($"[Supabase] Recipients 업서트 및 UploadHistory 저장 중...");
                    var repository = new SupabaseUploadRepository(_settings.Supabase);
                    await repository.UploadAsync(result, CancellationToken.None);
                    AppendUploadLog("[Supabase] DB 반영 완료");
                }

                UpdateUploadInfo(result);
                UpdateUploadResultGrid(result);
                _saveTextButton.Enabled = true;
                _saveExcelButton.Enabled = true;
                _statusLabel.Text = canUploadToSupabase ? "상태: 업로드 완료" : "상태: 파일 처리 완료 (Supabase 미설정)";
                _uploadDateLabel.Text = $"업로드 일시: {result.UploadedAt:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "상태: 업로드 오류";
                AppendUploadLog($"[오류] {ex.Message}");
                MessageBox.Show($"업로드 처리 중 오류가 발생했습니다.\n\n{ex.Message}", "업로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetUploadBusy(false);
            }
        }

        private void SaveTextButton_Click(object sender, EventArgs e)
        {
            if (!EnsureUploadResultExists())
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text Files (*.txt)|*.txt";
                dialog.FileName = $"UploadResult_{DateTime.Now:yyMMdd-HHmm}.txt";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    UploadResultExporter.SaveText(_lastUploadResult, dialog.FileName);
                    AppendUploadLog($"[저장] 결과 텍스트 저장: {dialog.FileName}");
                }
            }
        }

        private void SaveExcelButton_Click(object sender, EventArgs e)
        {
            if (!EnsureUploadResultExists())
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dialog.FileName = $"UploadResult_{DateTime.Now:yyMMdd-HHmm}.xlsx";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    UploadResultExporter.SaveExcel(_lastUploadResult, dialog.FileName);
                    AppendUploadLog($"[저장] 결과 Excel 저장: {dialog.FileName}");
                }
            }
        }

        private bool EnsureUploadResultExists()
        {
            if (_lastUploadResult != null)
            {
                return true;
            }

            MessageBox.Show("저장할 업로드 결과가 없습니다.", "결과 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private void SetUploadBusy(bool isBusy)
        {
            if (_uploadStartButton != null)
            {
                _uploadStartButton.Enabled = !isBusy;
            }

            _statusLabel.Text = isBusy ? "상태: 업로드 처리 중" : _statusLabel.Text;
        }

        private void UpdateUploadInfo(UploadProcessResult result)
        {
            _uploadInfoDateValueLabel.Text = result.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss");
            _uploadInfoFileNameValueLabel.Text = result.FileName;
            _uploadInfoSheetCountValueLabel.Text = result.SheetCount.ToString("N0");
            _uploadInfoSheetNameValueLabel.Text = result.SheetName;
            _uploadInfoRowCountValueLabel.Text = result.TotalRows.ToString("N0");
        }

        private void UpdateUploadResultGrid(UploadProcessResult result)
        {
            _uploadResultGrid.Rows.Clear();
            foreach (var row in result.CreateSummaryRows())
            {
                _uploadResultGrid.Rows.Add(row.Category, row.Description, row.Count.ToString("N0"), row.Status, row.Memo);
            }
        }

        private async Task LoadRecipientsAsync()
        {
            _settings = AppSettings.LoadOrCreate(_configPath);
            if (!_settings.Supabase.IsConfigured)
            {
                MessageBox.Show("Supabase 설정이 필요합니다.", "설정 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetRecipientBusy(true);
            try
            {
                var repository = new SupabaseRecipientRepository(_settings.Supabase);
                var recipients = await repository.SearchRecipientsAsync(
                    _sortDescRadio.Checked,
                    (int)_sendCountNumeric.Value,
                    _excludeSentCheckBox.Checked,
                    _excludeBlockedCheckBox.Checked,
                    CancellationToken.None);

                _recipientRows.Clear();
                _recipientRows.AddRange(recipients);
                _recipientGrid.Rows.Clear();
                foreach (var recipient in recipients)
                {
                    var rowIndex = _recipientGrid.Rows.Add(
                        true,
                        recipient.Email,
                        recipient.AgencyName,
                        recipient.NoticeDate?.ToString("yyyy-MM-dd") ?? "");
                    _recipientGrid.Rows[rowIndex].Tag = recipient;
                }

                UpdateSelectedCount();
                AppendSendLog($"[대상 조회] {recipients.Count:N0}건 조회 완료");
                _statusLabel.Text = $"상태: 발송 대상 조회 완료 ({recipients.Count:N0}건)";
            }
            catch (Exception ex)
            {
                AppendSendLog($"[대상 조회 오류] {ex.Message}");
                MessageBox.Show($"발송 대상 조회 중 오류가 발생했습니다.\n\n{ex.Message}", "대상 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetRecipientBusy(false);
            }
        }

        private async Task MarkSelectedAsSentAsync()
        {
            var selected = GetSelectedRecipients();
            if (selected.Count == 0)
            {
                MessageBox.Show("발송완료 처리할 대상을 선택하세요.", "대상 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"{selected.Count:N0}건을 실제 메일 발송 없이 발송완료 처리하시겠습니까?",
                "발송완료 처리 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _settings = AppSettings.LoadOrCreate(_configPath);
            if (!_settings.Supabase.IsConfigured)
            {
                MessageBox.Show("Supabase 설정이 필요합니다.", "설정 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetRecipientBusy(true);
            try
            {
                var now = DateTime.Now;
                var histories = selected.Select(item => new SendHistoryDto
                {
                    RecipientId = item.Id > 0 ? item.Id : (long?)null,
                    Email = item.Email,
                    Subject = _subjectTextBox?.Text?.Trim(),
                    Status = "ManuallyMarkedSent",
                    Method = "Manual",
                    Memo = "사용자가 발송 없이 발송완료 처리",
                    ProcessedAt = now,
                }).ToList();

                var repository = new SupabaseRecipientRepository(_settings.Supabase);
                await repository.InsertSendHistoriesAsync(histories, CancellationToken.None);
                AppendSendLog($"[수동완료] {selected.Count:N0}건 발송완료 처리");
                await LoadRecipientsAsync();
            }
            catch (Exception ex)
            {
                AppendSendLog($"[수동완료 오류] {ex.Message}");
                MessageBox.Show($"발송완료 처리 중 오류가 발생했습니다.\n\n{ex.Message}", "발송완료 처리 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetRecipientBusy(false);
            }
        }

        private async Task SendMailAsync()
        {
            if (!TryCreateDraft(out var draft, out var validationMessage))
            {
                MessageBox.Show(validationMessage, "메일 작성 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selected = DistinctRecipients(GetSelectedRecipients());
            var manualRecipients = CreateManualRecipients(draft.DefaultTo, selected);
            if (selected.Count + manualRecipients.Count == 0)
            {
                MessageBox.Show("DB 조회 대상 또는 수동 수신자를 1명 이상 지정하세요.", "대상 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _settings = AppSettings.LoadOrCreate(_configPath);
            var confirm = MessageBox.Show(
                $"DB 선택 대상 {selected.Count:N0}건, 수동 수신자 {manualRecipients.Count:N0}건의 메일을 실제 SMTP로 발송하시겠습니까?",
                "SMTP 발송 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetRecipientBusy(true);
            if (_sendButton != null)
            {
                _sendButton.Enabled = false;
            }

            try
            {
                var service = new MailSendService();
                var results = new List<MailSendResult>();

                if (selected.Count > 0)
                {
                    AppendSendLog($"[SMTP] DB 조회 대상 발송 시작: {selected.Count:N0}건");
                    results.AddRange(await service.SendAsync(selected, draft, _settings, AppendSendLog, CancellationToken.None));
                }

                if (manualRecipients.Count > 0)
                {
                    AppendSendLog($"[SMTP] 수동 입력 대상 발송 시작: {manualRecipients.Count:N0}건");
                    results.AddRange(await service.SendAsync(manualRecipients, draft, _settings, AppendSendLog, CancellationToken.None));
                }

                var histories = results.Select(result => new SendHistoryDto
                {
                    RecipientId = result.Recipient.Id > 0 ? result.Recipient.Id : (long?)null,
                    Email = result.Recipient.Email,
                    Subject = draft.Subject,
                    Status = result.IsSuccess ? "Sent" : "Failed",
                    Method = "Smtp",
                    Memo = result.Message,
                    ProcessedAt = DateTime.Now,
                }).ToList();

                if (_settings.Supabase.IsConfigured)
                {
                    var repository = new SupabaseRecipientRepository(_settings.Supabase);
                    await repository.InsertSendHistoriesAsync(histories, CancellationToken.None);
                    AppendSendLog("[SMTP] 발송 이력 DB 기록 완료");
                }
                else
                {
                    AppendSendLog("[SMTP] Supabase 설정이 없어 DB 이력은 기록하지 않았습니다.");
                }

                var reportPath = MailSendService.SaveReport(results, AppDomain.CurrentDomain.BaseDirectory);
                AppendSendLog($"[SMTP] 텍스트 보고서 생성: {reportPath}");
                MessageBox.Show("발송 처리가 완료되었습니다.", "발송 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_excludeSentCheckBox.Checked)
                {
                    await LoadRecipientsAsync();
                }
            }
            catch (Exception ex)
            {
                AppendSendLog($"[SMTP 오류] {ex.Message}");
                MessageBox.Show($"SMTP 발송 중 오류가 발생했습니다.\n\n{ex.Message}", "SMTP 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetRecipientBusy(false);
                if (_sendButton != null)
                {
                    _sendButton.Enabled = true;
                }
            }
        }

        private void SaveDraftButton_Click(object sender, EventArgs e)
        {
            if (!TryCreateDraft(out var draft, out var message))
            {
                MessageBox.Show(message, "임시저장 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON Files (*.json)|*.json";
                dialog.FileName = $"MailDraft_{DateTime.Now:yyMMdd-HHmm}.json";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(draft, Formatting.Indented), Encoding.UTF8);
                AppendSendLog($"[임시저장] {dialog.FileName}");
                MessageBox.Show("임시저장되었습니다.", "임시저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadDraftButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON Files (*.json)|*.json";
                dialog.Title = "메일 설정 불러오기";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var draft = JsonConvert.DeserializeObject<MailDraft>(json);
                    if (draft == null)
                    {
                        MessageBox.Show("불러올 수 있는 메일 설정이 없습니다.", "불러오기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ApplyDraftToEditor(draft);
                    AppendSendLog($"[불러오기] {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"메일 설정을 불러오는 중 오류가 발생했습니다.\n\n{ex.Message}", "불러오기 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            if (!TryCreateDraft(out var draft, out var message))
            {
                MessageBox.Show(message, "미리보기 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var preview = new StringBuilder();
            var selected = DistinctRecipients(GetSelectedRecipients());
            var manualRecipients = CreateManualRecipients(draft.DefaultTo, selected);
            preview.AppendLine($"수동 수신자: {manualRecipients.Count:N0}건");
            preview.AppendLine($"DB 선택 대상: {selected.Count:N0}건");
            preview.AppendLine($"제목: {draft.Subject}");
            preview.AppendLine($"참조: {draft.DefaultCc}");
            preview.AppendLine($"첨부: {draft.AttachmentPaths.Count:N0}개");
            preview.AppendLine();
            preview.AppendLine(draft.DefaultBodyText);
            MessageBox.Show(preview.ToString(), "메일 미리보기", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddAttachmentButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "첨부 파일 선택";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (var fileName in dialog.FileNames)
                {
                    AddAttachment(fileName);
                }
            }
        }

        private void DeleteAttachmentButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in _attachmentListView.SelectedItems)
            {
                var path = item.Tag as string;
                _attachmentPaths.Remove(path);
                _attachmentListView.Items.Remove(item);
            }
        }

        private void InsertImageButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "본문 이미지 선택";
                dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                AddAttachment(dialog.FileName);
                _bodyTextBox.SelectedText = $"{Environment.NewLine}[이미지: {Path.GetFileName(dialog.FileName)}]{Environment.NewLine}";
                AppendSendLog($"[이미지] 본문 이미지 참조 추가: {Path.GetFileName(dialog.FileName)}");
            }
        }

        private bool TryCreateDraft(out MailDraft draft, out string message)
        {
            draft = null;
            var to = (_toTextBox?.Text ?? "").Trim();
            foreach (var address in SplitMailAddresses(to))
            {
                if (!IsValidMailAddress(address))
                {
                    message = $"수신 이메일 형식이 올바르지 않습니다: {address}";
                    return false;
                }
            }

            var subject = (_subjectTextBox?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(subject))
            {
                message = "제목을 입력하세요.";
                return false;
            }

            var cc = (_ccTextBox?.Text ?? "").Trim();
            foreach (var address in SplitMailAddresses(cc))
            {
                if (!IsValidMailAddress(address))
                {
                    message = $"참조 이메일 형식이 올바르지 않습니다: {address}";
                    return false;
                }
            }

            foreach (var path in _attachmentPaths)
            {
                if (!File.Exists(path))
                {
                    message = $"첨부파일이 존재하지 않습니다: {path}";
                    return false;
                }
            }

            draft = new MailDraft
            {
                DefaultTo = to,
                DefaultCc = cc,
                Subject = subject,
                DefaultBodyText = _bodyTextBox?.Text ?? "",
                AttachmentPaths = _attachmentPaths.ToList(),
            };
            message = null;
            return true;
        }

        private void ApplyDraftToEditor(MailDraft draft)
        {
            _toTextBox.Text = draft.DefaultTo ?? "";
            _ccTextBox.Text = draft.DefaultCc ?? "";
            _subjectTextBox.Text = string.IsNullOrWhiteSpace(draft.Subject) ? _settings.Subject : draft.Subject;
            _bodyTextBox.Text = string.IsNullOrWhiteSpace(draft.DefaultBodyText) ? _settings.DefaultBodyText : draft.DefaultBodyText;

            _attachmentPaths.Clear();
            _attachmentListView.Items.Clear();
            foreach (var path in draft.AttachmentPaths ?? new List<string>())
            {
                if (File.Exists(path))
                {
                    AddAttachment(path);
                }
            }
        }

        private List<RecipientListItem> GetSelectedRecipients()
        {
            var selected = new List<RecipientListItem>();
            foreach (DataGridViewRow row in _recipientGrid.Rows)
            {
                var isSelected = row.Cells[0].Value is bool value && value;
                if (isSelected && row.Tag is RecipientListItem item)
                {
                    selected.Add(item);
                }
            }

            return selected;
        }

        private static List<RecipientListItem> DistinctRecipients(IEnumerable<RecipientListItem> recipients)
        {
            var result = new List<RecipientListItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var recipient in recipients ?? Enumerable.Empty<RecipientListItem>())
            {
                var email = NormalizeEmail(recipient.Email);
                if (string.IsNullOrWhiteSpace(email) || !seen.Add(email))
                {
                    continue;
                }

                result.Add(recipient);
            }

            return result;
        }

        private static List<RecipientListItem> CreateManualRecipients(string value, IEnumerable<RecipientListItem> alreadySelected)
        {
            var seen = new HashSet<string>(
                (alreadySelected ?? Enumerable.Empty<RecipientListItem>())
                    .Select(item => NormalizeEmail(item.Email))
                    .Where(item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);

            var recipients = new List<RecipientListItem>();
            foreach (var email in SplitMailAddresses(value))
            {
                var normalized = NormalizeEmail(email);
                if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
                {
                    continue;
                }

                recipients.Add(new RecipientListItem
                {
                    Id = 0,
                    Email = email,
                    NormalizedEmail = normalized,
                    AgencyName = "수동 입력",
                });
            }

            return recipients;
        }

        private void SetAllRecipientsSelected(bool selected)
        {
            foreach (DataGridViewRow row in _recipientGrid.Rows)
            {
                row.Cells[0].Value = selected;
            }

            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            _selectedCountLabel.Text = $"선택된 대상: {GetSelectedRecipients().Count:N0}건";
        }

        private void SetRecipientBusy(bool isBusy)
        {
            if (_queryRecipientsButton != null)
            {
                _queryRecipientsButton.Enabled = !isBusy;
            }

            if (_manualCompleteButton != null)
            {
                _manualCompleteButton.Enabled = !isBusy;
            }
        }

        private void AddAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || _attachmentPaths.Contains(filePath))
            {
                return;
            }

            _attachmentPaths.Add(filePath);
            var info = new FileInfo(filePath);
            var item = new ListViewItem(new[] { info.Name, $"{Math.Max(1, info.Length / 1024):N0} KB" }) { Tag = filePath };
            _attachmentListView.Items.Add(item);
            AppendSendLog($"[첨부] 추가: {info.Name}");
        }

        private static IEnumerable<string> SplitMailAddresses(string value)
        {
            return (value ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        private static string NormalizeEmail(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }

        private static bool IsValidMailAddress(string value)
        {
            try
            {
                var address = new MailAddress(value);
                return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void AddInfoRow(TableLayoutPanel layout, int row, string name, Control valueControl)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.Controls.Add(CreateLabel(name), 0, row);
            layout.Controls.Add(valueControl, 1, row);
        }
    }
}
