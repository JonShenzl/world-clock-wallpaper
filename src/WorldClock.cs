// Copyright (c) 2026 Jon.Shen (Jon.Shen@Foxmail.com)
// 自由复制、修改；禁止商业使用；整合或再分发需注明出处。

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

[assembly: System.Reflection.AssemblyTitle("World Clock Wallpaper")]
[assembly: System.Reflection.AssemblyProduct("World Clock Wallpaper")]
[assembly: System.Reflection.AssemblyCompany("Jon.Shen")]
[assembly: System.Reflection.AssemblyCopyright("Copyright \u00A9 2026 Jon.Shen (Jon.Shen@Foxmail.com). Non-commercial use only; attribution required.")]
[assembly: System.Reflection.AssemblyDescription("World Clock Wallpaper | Jon.Shen | Jon.shen@Foxmail.com")]
[assembly: System.Reflection.AssemblyVersion("1.5.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.5.0.0")]

namespace WorldClockWallpaper
{
    internal static class Native
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const uint GW_HWNDPREV = 3;
        public const int HWND_TOP = 0;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string cls, string win);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    }

    public class ClockWindow : Window
    {
        private readonly TextBlock _hour;
        private readonly TextBlock _minute;
        private readonly TextBlock _date;
        private readonly StackPanel _colon;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _layerTimer;
        private string _lastTime;
        private string _lastDate;
        private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
        private string _country = string.Empty;
        private string _city = string.Empty;

        private readonly LinearGradientBrush _digitBrush;
        private readonly SolidColorBrush _dateBrush;
        private readonly SolidColorBrush _labelBrush;
        private readonly Rectangle _dotTop;
        private readonly Rectangle _dotBottom;
        private readonly SolidColorBrush _redDots = new SolidColorBrush(Color.FromRgb(232, 62, 48));
        private bool? _dotsRed;

        // 天光色板：按当地小时取色（深夜 → 黎明 → 白天 → 黄昏 → 深夜）
        private static readonly double[] SkyHours = { 0, 5, 6.5, 8.5, 12, 16.5, 18.5, 20.5, 24 };
        private static readonly Color[] SkyTop =
        {
            Color.FromRgb(196, 212, 252), Color.FromRgb(196, 212, 252), Color.FromRgb(255, 214, 176),
            Color.FromRgb(255, 248, 238), Color.FromRgb(255, 255, 255), Color.FromRgb(255, 250, 242),
            Color.FromRgb(255, 204, 164), Color.FromRgb(204, 218, 254), Color.FromRgb(196, 212, 252)
        };
        private static readonly Color[] SkyBottom =
        {
            Color.FromRgb(88, 108, 162), Color.FromRgb(88, 108, 162), Color.FromRgb(214, 130, 100),
            Color.FromRgb(166, 196, 228), Color.FromRgb(152, 190, 228), Color.FromRgb(160, 194, 226),
            Color.FromRgb(222, 124, 94), Color.FromRgb(94, 114, 172), Color.FromRgb(88, 108, 162)
        };

        private static readonly string[] Weekdays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        public ClockWindow()
        {
            LoadSettings();

            double screenH = SystemParameters.PrimaryScreenHeight;

            Title = "World Clock Wallpaper";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = false;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SizeToContent = SizeToContent.WidthAndHeight;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            double timeSize = screenH * 0.105;
            double dateSize = screenH * 0.020;
            double pad = timeSize * 0.30;

            // 行距收紧：城市行与日期行 -15px，日期行与时间行 -25px
            double cityToDateGap = Math.Max(0, dateSize * 0.35 - 15);
            double dateToTimeGap = timeSize * 0.08 - 25;

            _digitBrush = new LinearGradientBrush(
                Color.FromArgb(255, 255, 255, 255),
                Color.FromArgb(255, 148, 174, 202),
                new Point(0.5, 0),
                new Point(0.5, 1));

            var timeShadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = timeSize * 0.20,
                ShadowDepth = 0,
                Opacity = 0.85
            };

            Func<string, TextBlock> digit = text => new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontWeight = FontWeights.SemiBold,
                FontSize = timeSize,
                Foreground = _digitBrush,
                Effect = timeShadow,
                LineHeight = timeSize * 1.12,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };

            _hour = digit("00");
            _minute = digit("00");

            double dotSize = timeSize * 0.092;
            _colon = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(timeSize * 0.055, 0, timeSize * 0.055, 0)
            };
            _dotTop = new Rectangle
            {
                Width = dotSize,
                Height = dotSize,
                RadiusX = dotSize * 0.2,
                RadiusY = dotSize * 0.2,
                Fill = _digitBrush,
                Effect = timeShadow
            };
            _dotBottom = new Rectangle
            {
                Width = dotSize,
                Height = dotSize,
                RadiusX = dotSize * 0.2,
                RadiusY = dotSize * 0.2,
                Fill = _digitBrush,
                Effect = timeShadow,
                Margin = new Thickness(0, timeSize * 0.155, 0, 0)
            };
            _colon.Children.Add(_dotTop);
            _colon.Children.Add(_dotBottom);

            var timeRow = new StackPanel { Orientation = Orientation.Horizontal };
            timeRow.Children.Add(_hour);
            timeRow.Children.Add(_colon);
            timeRow.Children.Add(_minute);

            _dateBrush = new SolidColorBrush(Color.FromArgb(215, 255, 255, 255));
            _labelBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));

            _date = new TextBlock
            {
                Text = "00/00",
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Light,
                FontSize = dateSize,
                Foreground = _dateBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, timeSize * 0.02 + 20, dateToTimeGap),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = dateSize * 0.9,
                    ShadowDepth = 0,
                    Opacity = 0.8
                }
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(pad)
            };

            // 标签 = 国家2位简称 + 主要城市，例如 "UK · LONDON"
            string label;
            if (_country.Length > 0 && _city.Length > 0)
            {
                label = _country + " · " + _city;
            }
            else
            {
                label = _country.Length > 0 ? _country : _city;
            }

            if (label.Length > 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = label.ToUpperInvariant(),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = dateSize * 0.70,
                    Foreground = _labelBrush,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, timeSize * 0.02 + 20, cityToDateGap),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = dateSize * 0.8,
                        ShadowDepth = 0,
                        Opacity = 0.75
                    }
                });
            }

            stack.Children.Add(_date);
            stack.Children.Add(timeRow);
            Content = stack;

            _clockTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _clockTimer.Tick += (sender, args) => RefreshTime();
            RefreshTime();
            _clockTimer.Start();

            _layerTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _layerTimer.Tick += (sender, args) => StickToDesktopLayer();
            _layerTimer.Start();
        }

        private void RefreshTime()
        {
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
            string date = now.ToString("MM/dd") + " " + Weekdays[(int)now.DayOfWeek];
            string time = now.ToString("HH:mm");

            if (date != _lastDate)
            {
                _date.Text = date;
                _lastDate = date;
            }

            if (time != _lastTime)
            {
                _hour.Text = time.Substring(0, 2);
                _minute.Text = time.Substring(3, 2);
                _lastTime = time;
                ApplySkyPalette(now);
            }

            // 9-18 点之外，时间中间的两点显示红色
            bool redDots = now.Hour < 9 || now.Hour >= 18;
            if (_dotsRed != redDots)
            {
                Brush fill = redDots ? (Brush)_redDots : _digitBrush;
                _dotTop.Fill = fill;
                _dotBottom.Fill = fill;
                _dotsRed = redDots;
            }

            _colon.Opacity = now.Millisecond < 500 ? 1.0 : 0.18;
        }

        private void ApplySkyPalette(DateTime now)
        {
            double hour = now.Hour + now.Minute / 60.0;
            int i = 0;
            while (i < SkyHours.Length - 2 && hour >= SkyHours[i + 1])
            {
                i++;
            }

            double span = SkyHours[i + 1] - SkyHours[i];
            double t = span > 0 ? (hour - SkyHours[i]) / span : 0;
            t = Math.Max(0.0, Math.Min(1.0, t));

            Color top = Lerp(SkyTop[i], SkyTop[i + 1], t);
            Color bottom = Lerp(SkyBottom[i], SkyBottom[i + 1], t);

            _digitBrush.GradientStops[0].Color = top;
            _digitBrush.GradientStops[1].Color = bottom;
            _dateBrush.Color = Color.FromArgb(215, top.R, top.G, top.B);
            _labelBrush.Color = Color.FromArgb(150, top.R, top.G, top.B);
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private void LoadSettings()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
            if (!File.Exists(path))
            {
                WriteDefaultSettings(path);
            }

            var values = new Dictionary<string, string>();
            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                    {
                        continue;
                    }

                    int split = line.IndexOf('=');
                    if (split <= 0)
                    {
                        continue;
                    }

                    values[line.Substring(0, split).Trim().ToLowerInvariant()] = line.Substring(split + 1).Trim();
                }
            }
            catch
            {
            }

            string zoneId = values.ContainsKey("timezone") ? values["timezone"] : "GMT Standard Time";
            if (zoneId.Length > 0)
            {
                try
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
                }
                catch
                {
                    _timeZone = TimeZoneInfo.Local;
                }
            }

            _country = values.ContainsKey("country") ? values["country"] : "UK";
            _city = values.ContainsKey("city") ? values["city"] : "LONDON";
        }

        private static void WriteDefaultSettings(string path)
        {
            const string content =
                "# 世界时钟壁纸设置（修改后保存，重启程序生效）\r\n" +
                "# 时区 TimeZone：填 Windows 时区 ID\r\n" +
                "#   英国伦敦 = GMT Standard Time（自动切换冬令时/夏令时）\r\n" +
                "#   中国北京 = China Standard Time\r\n" +
                "#   日本东京 = Tokyo Standard Time\r\n" +
                "#   美国纽约 = Eastern Standard Time\r\n" +
                "#   世界标准时 = UTC\r\n" +
                "TimeZone=GMT Standard Time\r\n" +
                "\r\n" +
                "# 国家标签 Country：2 位国家/地区简称，显示在日期上方\r\n" +
                "#   例如 UK 英国、CN 中国、JP 日本、US 美国，留空则不显示\r\n" +
                "Country=UK\r\n" +
                "\r\n" +
                "# 城市标签 City：主要城市，与国家组合显示为 “UK · LONDON”，留空则只显示国家\r\n" +
                "City=LONDON\r\n";

            try
            {
                File.WriteAllText(path, content, new UTF8Encoding(true));
            }
            catch
            {
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int style = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
            Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE,
                style | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            PlaceAtDesktopCorner();
            StickToDesktopLayer();
        }

        private void PlaceAtDesktopCorner()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double gap = screenH * 0.05;
            double taskbar = screenH - SystemParameters.WorkArea.Bottom;

            Left = screenW - ActualWidth - gap;
            Top = screenH - ActualHeight - gap - (taskbar > 0 ? taskbar : 0);
        }

        private void StickToDesktopLayer()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            IntPtr progman = Native.FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                return;
            }

            // 目标位置：紧贴桌面窗口上方的空档（Progman 之前的那一层）
            IntPtr floor = Native.GetWindow(progman, Native.GW_HWNDPREV);
            if (floor == hwnd)
            {
                return;
            }

            Native.SetWindowPos(hwnd, floor != IntPtr.Zero ? floor : new IntPtr(Native.HWND_TOP), 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.Run(new ClockWindow());
        }
    }
}
