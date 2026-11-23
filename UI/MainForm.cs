using DollarQuoteMonitor.Services;
using DollarQuoteMonitor.Models;

namespace DollarQuoteMonitor.UI;

public class MainForm : Form
{
    // ── Cores do tema ──────────────────────────────────────────────
    private static readonly Color BgColor       = Color.FromArgb(30, 40, 30);
    private static readonly Color GreenAccent   = Color.FromArgb(255, 255, 255);   // branco
    private static readonly Color GreenDim      = Color.FromArgb(220, 220, 180);   // amarelo claro
    private static readonly Color TextMuted     = Color.FromArgb(230, 230, 200);   // creme
    private static readonly Color ErrorColor    = Color.FromArgb(255, 100, 100);   // vermelho claro

    // ── Controles ──────────────────────────────────────────────────
    private readonly Label  _iconLabel      = new();
    private readonly Label  _quoteLabel     = new();
    private readonly Label  _timeLabel      = new();
    private readonly Label  _statusLabel    = new();
    private readonly Panel  _spinnerPanel   = new();
    private Image? _backgroundImage;

    // ── Lógica ─────────────────────────────────────────────────────
    private readonly CurrencyService _service = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly System.Windows.Forms.Timer _spinnerTimer = new();
    private int  _spinnerAngle = 0;
    private bool _isUpdating   = false;

    public MainForm()
    {
        InitializeComponent();
        LoadBackgroundImage();
        _ = FetchQuoteAsync();          // primeira busca imediata

        _refreshTimer.Interval = 30_000;
        _refreshTimer.Tick += async (_, _) => await FetchQuoteAsync();
        _refreshTimer.Start();
    }

    private void LoadBackgroundImage()
    {
        try
        {
            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "dollar_bg.jpg");
            if (File.Exists(imagePath))
            {
                _backgroundImage = Image.FromFile(imagePath);
            }
        }
        catch
        {
            // Se falhar ao carregar, continua sem background
        }
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // ── Janela ─────────────────────────────────────────────────
        Text            = "Dollar Quote Monitor";
        Size            = new Size(360, 280);
        MinimumSize     = new Size(360, 280);
        MaximumSize     = new Size(360, 280);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = BgColor;
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f, FontStyle.Regular);
        
        // Habilitar double buffering para evitar flicker
        DoubleBuffered  = true;
        
        // Evento de pintura customizado para desenhar o background
        Paint += MainForm_Paint;

        // ── Ícone de nota de dólar (emoji / texto) ─────────────────
        _iconLabel.Text      = "$ 1";
        _iconLabel.Font      = new Font("Segoe UI", 48f, FontStyle.Bold);
        _iconLabel.ForeColor = GreenAccent;
        _iconLabel.AutoSize  = false;
        _iconLabel.Size      = new Size(160, 80);
        _iconLabel.Location  = new Point(100, 14);
        _iconLabel.TextAlign = ContentAlignment.MiddleCenter;
        _iconLabel.BackColor = Color.Transparent;

        // ── Valor da cotação ───────────────────────────────────────
        _quoteLabel.Text      = "Carregando...";
        _quoteLabel.Font      = new Font("Segoe UI", 38f, FontStyle.Bold);
        _quoteLabel.ForeColor = GreenAccent;
        _quoteLabel.AutoSize  = false;
        _quoteLabel.Size      = new Size(340, 70);
        _quoteLabel.Location  = new Point(10, 98);
        _quoteLabel.TextAlign = ContentAlignment.MiddleCenter;
        _quoteLabel.BackColor = Color.Transparent;

        // ── Horário da última atualização ──────────────────────────
        _timeLabel.Text      = string.Empty;
        _timeLabel.Font      = new Font("Segoe UI", 9f);
        _timeLabel.ForeColor = TextMuted;
        _timeLabel.AutoSize  = false;
        _timeLabel.Size      = new Size(340, 20);
        _timeLabel.Location  = new Point(10, 218);
        _timeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _timeLabel.BackColor = Color.Transparent;

        // ── Status / erro ──────────────────────────────────────────
        _statusLabel.Text      = string.Empty;
        _statusLabel.Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic);
        _statusLabel.ForeColor = ErrorColor;
        _statusLabel.AutoSize  = false;
        _statusLabel.Size      = new Size(340, 20);
        _statusLabel.Location  = new Point(10, 198);
        _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        _statusLabel.BackColor = Color.Transparent;

        // ── Spinner (animação de atualização) ──────────────────────
        _spinnerPanel.Size      = new Size(16, 16);
        _spinnerPanel.Location  = new Point(170, 228);
        _spinnerPanel.BackColor = Color.Transparent;
        _spinnerPanel.Paint    += SpinnerPanel_Paint;

        _spinnerTimer.Interval = 60;
        _spinnerTimer.Tick    += (_, _) =>
        {
            _spinnerAngle = (_spinnerAngle + 30) % 360;
            _spinnerPanel.Invalidate();
        };

        // ── Rodapé ─────────────────────────────────────────────────
        var footer = new Label
        {
            Text      = "USD → BRL  •  Frankfurter API",
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = GreenDim,
            AutoSize  = false,
            Size      = new Size(340, 18),
            Location  = new Point(10, 250),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        Controls.AddRange(new Control[]
        {
            _iconLabel, _quoteLabel, _timeLabel,
            _statusLabel, _spinnerPanel, footer
        });

        ResumeLayout(false);
    }

    // ── Desenho do background com opacidade ────────────────────────
    private void MainForm_Paint(object? sender, PaintEventArgs e)
    {
        if (_backgroundImage == null) return;

        var g = e.Graphics;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        // Opacidade 65%
        var colorMatrix = new System.Drawing.Imaging.ColorMatrix
        {
            Matrix33 = 0.65f
        };

        var imageAttributes = new System.Drawing.Imaging.ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix,
            System.Drawing.Imaging.ColorMatrixFlag.Default,
            System.Drawing.Imaging.ColorAdjustType.Bitmap);

        // Cobrir toda a área do cliente
        g.DrawImage(_backgroundImage,
            new Rectangle(0, 0, ClientSize.Width, ClientSize.Height),
            0, 0, _backgroundImage.Width, _backgroundImage.Height,
            GraphicsUnit.Pixel,
            imageAttributes);

        // Overlay escuro semitransparente sobre toda a tela para legibilidade dos textos
        using var overlayBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0));
        g.FillRectangle(overlayBrush, 0, 0, ClientSize.Width, ClientSize.Height);
    }

    // ── Animação do spinner ────────────────────────────────────────
    private void SpinnerPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (!_isUpdating) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var pen = new Pen(GreenAccent, 2.5f);
        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        pen.EndCap   = System.Drawing.Drawing2D.LineCap.Round;

        g.DrawArc(pen, 1, 1, 13, 13, _spinnerAngle, 270);
    }

    // ── Busca da cotação ───────────────────────────────────────────
    private async Task FetchQuoteAsync()
    {
        SetUpdating(true);

        try
        {
            var quote = await _service.GetQuoteAsync("USD-BRL");
            UpdateUI(quote);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetUpdating(false);
        }
    }

    private void SetUpdating(bool updating)
    {
        if (InvokeRequired) { Invoke(() => SetUpdating(updating)); return; }

        _isUpdating = updating;
        if (updating)
            _spinnerTimer.Start();
        else
        {
            _spinnerTimer.Stop();
            _spinnerPanel.Invalidate();
        }
    }

    private void UpdateUI(QuoteModel quote)
    {
        if (InvokeRequired) { Invoke(() => UpdateUI(quote)); return; }

        _quoteLabel.ForeColor = GreenAccent;
        _quoteLabel.Text      = quote.BidValue.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        _timeLabel.Text       = $"Última atualização: {DateTime.Now:HH:mm:ss}";
        _statusLabel.Text     = string.Empty;
    }

    private void ShowError(string message)
    {
        if (InvokeRequired) { Invoke(() => ShowError(message)); return; }

        _statusLabel.Text     = "⚠ API indisponível";
        _statusLabel.ForeColor = ErrorColor;

        // Mantém o último valor, apenas muda a cor para indicar desatualização
        if (_quoteLabel.Text != "Carregando...")
            _quoteLabel.ForeColor = Color.FromArgb(160, 160, 100);

        _timeLabel.Text = $"Falha em: {DateTime.Now:HH:mm:ss}  ({message[..Math.Min(40, message.Length)]})";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        _spinnerTimer.Stop();
        _backgroundImage?.Dispose();
        base.OnFormClosed(e);
    }
}
