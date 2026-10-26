using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace widget;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _clockTimer;
    private Forms.NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _clockTimer.Tick += ClockTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Hide from Alt+Tab
        Win32.SetToolWindow(this);

        // Restore Position
        RestorePosition();

        // Apply all settings (Colors, Sizes, Modes, Formats)
        ApplyAllSettings();

        // System Tray Icon
        InitSystemTray();

        // Start timer
        UpdateClockDisplay();
        _clockTimer.Start();
    }

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (_settings.WindowMode == "Wallpaper")
        {
            Win32.SendToBottom(this);
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_settings.WindowMode == "Wallpaper")
        {
            Win32.SendToBottom(this);
        }
    }

    private void RestorePosition()
    {
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        if (_settings.WindowLeft >= 0 && _settings.WindowTop >= 0 &&
            _settings.WindowLeft < screenWidth - 100 && _settings.WindowTop < screenHeight - 60)
        {
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        else
        {
            // Default position: Centered horizontally, ~10% from the top (like wallpaper widget in the image)
            Left = (screenWidth - 420) / 2;
            Top = 60;
        }
    }

    private void SavePosition()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.Save();
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClockDisplay();
    }

    private void UpdateClockDisplay()
    {
        DateTime now = DateTime.Now;

        // Row 1: Time
        if (_settings.Is24Hour)
        {
            TimeMainText.Text = now.ToString("HH:mm");
            AmPmText.Visibility = Visibility.Collapsed;
        }
        else
        {
            TimeMainText.Text = now.ToString("hh:mm");
            AmPmText.Text = now.ToString("tt", CultureInfo.InvariantCulture).ToUpper();
            AmPmText.Visibility = Visibility.Visible;
        }

        // Seconds
        if (_settings.ShowSeconds)
        {
            TimeSecondsText.Text = now.ToString(":ss");
            TimeSecondsText.Visibility = Visibility.Visible;
        }
        else
        {
            TimeSecondsText.Visibility = Visibility.Collapsed;
        }

        // Row 2: Date
        if (_settings.Language == "en_short")
        {
            var enCulture = new CultureInfo("en-US");
            DateText.Text = now.ToString("ddd, d MMM yyyy", enCulture);
        }
        else if (_settings.Language == "th")
        {
            var thCulture = new CultureInfo("th-TH");
            string dayOfWeek = now.ToString("dddd", thCulture);
            string day = now.Day.ToString();
            string month = now.ToString("MMMM", thCulture);
            int year = now.Year;
            DateText.Text = $"{dayOfWeek}, {day} {month} {year}";
        }
        else
        {
            // Default English: "Wednesday, 21 September 2026" (matches the screenshot)
            var enCulture = new CultureInfo("en-US");
            DateText.Text = now.ToString("dddd, d MMMM yyyy", enCulture);
        }
    }

    private void ApplyAllSettings()
    {
        // Window Mode
        ApplyWindowMode(_settings.WindowMode);

        // Text Colors
        ApplyTextColor(_settings.TextColorHex, save: false);

        // Font Weight
        ApplyFontWeight(_settings.FontWeight, save: false);

        // Drop Shadow
        ApplyDropShadow(_settings.ShowShadow, save: false);

        // Scale
        ApplyScale(_settings.Scale, save: false);

        // Time / Date Menu Checks
        Menu24Hour.IsChecked = _settings.Is24Hour;
        Menu12Hour.IsChecked = !_settings.Is24Hour;
        MenuShowSeconds.IsChecked = _settings.ShowSeconds;

        MenuDateFullEn.IsChecked = (_settings.Language == "en");
        MenuDateShortEn.IsChecked = (_settings.Language == "en_short");
        MenuDateTh.IsChecked = (_settings.Language == "th");

        // Click-through & Auto Start
        ApplyClickThroughState();
        MenuAutoStart.IsChecked = AutoStart.IsEnabled();
    }

    private void ApplyWindowMode(string mode)
    {
        _settings.WindowMode = mode;
        MenuModeWallpaper.IsChecked = (mode == "Wallpaper");
        MenuModeTopmost.IsChecked = (mode == "Topmost");
        MenuModeNormal.IsChecked = (mode == "Normal");

        if (mode == "Topmost")
        {
            Topmost = true;
        }
        else if (mode == "Wallpaper")
        {
            Topmost = false;
            Win32.SendToBottom(this);
        }
        else
        {
            Topmost = false;
        }

        _settings.Save();
    }

    private void ApplyTextColor(string hex, bool save = true)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            var mainBrush = new SolidColorBrush(color);

            // Slightly lighter/softer for date and seconds
            var dateColor = System.Windows.Media.Color.FromArgb(
                (byte)(color.A * 0.88),
                (byte)(color.R * 0.95),
                (byte)(color.G * 0.95),
                (byte)(color.B * 0.95)
            );
            var dateBrush = new SolidColorBrush(dateColor);

            TimeMainText.Foreground = mainBrush;
            TimeSecondsText.Foreground = dateBrush;
            AmPmText.Foreground = dateBrush;
            DateText.Foreground = dateBrush;

            _settings.TextColorHex = hex;
            if (save) _settings.Save();
        }
        catch
        {
        }
    }

    private void ApplyFontWeight(string weightStr, bool save = true)
    {
        FontWeight weight = weightStr switch
        {
            "Light" => FontWeights.Light,
            "SemiBold" => FontWeights.SemiBold,
            _ => FontWeights.Normal
        };

        TimeMainText.FontWeight = weight;
        MenuWeightLight.IsChecked = (weightStr == "Light");
        MenuWeightNormal.IsChecked = (weightStr == "Normal");
        MenuWeightSemiBold.IsChecked = (weightStr == "SemiBold");

        _settings.FontWeight = weightStr;
        if (save) _settings.Save();
    }

    private void ApplyDropShadow(bool enable, bool save = true)
    {
        _settings.ShowShadow = enable;
        MenuDropShadow.IsChecked = enable;

        double opacity = enable ? 0.45 : 0.0;
        TimeShadowEffect.Opacity = opacity;
        DateShadowEffect.Opacity = opacity;

        if (save) _settings.Save();
    }

    private void ApplyScale(double scale, bool save = true)
    {
        scale = Math.Round(scale, 2);
        _settings.Scale = scale;
        WidgetScaleTransform.ScaleX = scale;
        WidgetScaleTransform.ScaleY = scale;
        if (save) _settings.Save();
    }

    private void ApplyClickThroughState()
    {
        Win32.SetClickThrough(this, _settings.ClickThrough);
        MenuClickThrough.IsChecked = _settings.ClickThrough;
    }

    private void ToggleClickThrough()
    {
        _settings.ClickThrough = !_settings.ClickThrough;
        ApplyClickThroughState();
        _settings.Save();

        if (_settings.ClickThrough)
        {
            _trayIcon?.ShowBalloonTip(3000, "Desktop Wallpaper Clock",
                "Click-Through mode enabled.\n(Double-click tray icon to unlock)",
                Forms.ToolTipIcon.Info);
        }
    }

    // Mouse Interaction
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_settings.ClickThrough)
        {
            DragMove();
            SavePosition();
            if (_settings.WindowMode == "Wallpaper")
            {
                Win32.SendToBottom(this);
            }
        }
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_settings.ClickThrough)
        {
            ClockContextMenu.IsOpen = true;
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_settings.ClickThrough) return;

        // Smooth zoom with scroll wheel
        double step = e.Delta > 0 ? 0.05 : -0.05;
        double newScale = Math.Clamp(_settings.Scale + step, 0.5, 3.0);
        ApplyScale(newScale);
    }

    // System Tray
    private void InitSystemTray()
    {
        try
        {
            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Desktop Wallpaper Clock\n(Double-click to toggle Click-Through)",
                Icon = CreateClockIcon(),
                Visible = true
            };

            var trayMenu = new Forms.ContextMenuStrip();

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("🕒 Desktop Wallpaper Clock") { Enabled = false });
            trayMenu.Items.Add(new Forms.ToolStripSeparator());

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("🔒 Toggle Click-Through", null, (s, e) =>
            {
                ToggleClickThrough();
            }));

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("🖼️ Desktop Wallpaper Layer", null, (s, e) =>
            {
                ApplyWindowMode("Wallpaper");
            }));

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("🔝 Always on Top", null, (s, e) =>
            {
                ApplyWindowMode("Topmost");
            }));

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("🚀 Start with Windows", null, (s, e) =>
            {
                bool newVal = !AutoStart.IsEnabled();
                AutoStart.Set(newVal);
                MenuAutoStart.IsChecked = newVal;
                _settings.StartWithWindows = newVal;
                _settings.Save();
            }));

            trayMenu.Items.Add(new Forms.ToolStripMenuItem("⚙ Reset Position", null, (s, e) =>
            {
                ResetPosition();
            }));

            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add(new Forms.ToolStripMenuItem("❌ Exit", null, (s, e) =>
            {
                Close();
            }));

            _trayIcon.ContextMenuStrip = trayMenu;

            _trayIcon.DoubleClick += (s, e) =>
            {
                ToggleClickThrough();
            };
        }
        catch
        {
        }
    }

    private System.Drawing.Icon CreateClockIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            using var brush = new SolidBrush(System.Drawing.Color.FromArgb(230, 20, 24, 33));
            g.FillEllipse(brush, 1, 1, 30, 30);

            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 255, 255, 255), 2f);
            g.DrawEllipse(pen, 1, 1, 30, 30);

            using var handPen = new System.Drawing.Pen(System.Drawing.Color.White, 2f);
            g.DrawLine(handPen, 16, 16, 16, 7);
            g.DrawLine(handPen, 16, 16, 23, 16);
        }
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void ResetPosition()
    {
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
        Top = 60;
        SavePosition();
        if (_settings.WindowMode == "Wallpaper")
        {
            Win32.SendToBottom(this);
        }
    }

    // Context Menu Event Handlers
    private void MenuModeWallpaper_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowMode("Wallpaper");
    }

    private void MenuModeTopmost_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowMode("Topmost");
    }

    private void MenuModeNormal_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowMode("Normal");
    }

    private void MenuClickThrough_Click(object sender, RoutedEventArgs e)
    {
        ToggleClickThrough();
    }

    private void MenuAutoStart_Click(object sender, RoutedEventArgs e)
    {
        bool enable = MenuAutoStart.IsChecked;
        AutoStart.Set(enable);
        _settings.StartWithWindows = enable;
        _settings.Save();
    }

    private void MenuColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string hex)
        {
            ApplyTextColor(hex);
        }
    }

    private void MenuCustomColor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var colorDialog = new Forms.ColorDialog
            {
                FullOpen = true,
                Color = ColorTranslator.FromHtml(_settings.TextColorHex)
            };

            if (colorDialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string hex = $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
                ApplyTextColor(hex);
            }
        }
        catch
        {
        }
    }

    private void MenuScale_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string scaleStr &&
            double.TryParse(scaleStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double scale))
        {
            ApplyScale(scale);
        }
    }

    private void MenuWeight_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string weightStr)
        {
            ApplyFontWeight(weightStr);
        }
    }

    private void MenuDropShadow_Click(object sender, RoutedEventArgs e)
    {
        ApplyDropShadow(MenuDropShadow.IsChecked);
    }

    private void Menu24Hour_Click(object sender, RoutedEventArgs e)
    {
        _settings.Is24Hour = true;
        Menu24Hour.IsChecked = true;
        Menu12Hour.IsChecked = false;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void Menu12Hour_Click(object sender, RoutedEventArgs e)
    {
        _settings.Is24Hour = false;
        Menu24Hour.IsChecked = false;
        Menu12Hour.IsChecked = true;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void MenuShowSeconds_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowSeconds = MenuShowSeconds.IsChecked;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void MenuDateFullEn_Click(object sender, RoutedEventArgs e)
    {
        _settings.Language = "en";
        MenuDateFullEn.IsChecked = true;
        MenuDateShortEn.IsChecked = false;
        MenuDateTh.IsChecked = false;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void MenuDateShortEn_Click(object sender, RoutedEventArgs e)
    {
        _settings.Language = "en_short";
        MenuDateFullEn.IsChecked = false;
        MenuDateShortEn.IsChecked = true;
        MenuDateTh.IsChecked = false;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void MenuDateTh_Click(object sender, RoutedEventArgs e)
    {
        _settings.Language = "th";
        MenuDateFullEn.IsChecked = false;
        MenuDateShortEn.IsChecked = false;
        MenuDateTh.IsChecked = true;
        _settings.Save();
        UpdateClockDisplay();
    }

    private void MenuResetPosition_Click(object sender, RoutedEventArgs e)
    {
        ResetPosition();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _clockTimer.Stop();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        SavePosition();
    }
}