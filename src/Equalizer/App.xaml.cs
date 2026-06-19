using System.Windows;
using WinForms = System.Windows.Forms;
using System.Drawing;

namespace Equalizer;

/// <summary>
/// Punkt wejscia aplikacji. Faza 0: aplikacja startuje do traya, bez audio.
/// Okno ustawien otwierane z menu traya. Autostart przelaczany w oknie.
/// </summary>
// 'Application' i 'MessageBox' istnieja zarowno w WPF jak i w WinForms - stad
// jawne kwalifikacje System.Windows.* tam, gdzie odwolujemy sie do wersji WPF.
public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Aplikacja zyje w trayu - nie zamyka sie po zamknieciu okna ustawien.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _trayIcon = new WinForms.NotifyIcon
        {
            // Brak wlasnego assetu w Fazie 0 - uzywamy ikony systemowej.
            // Wlasna ikona .ico dojdzie przy dopracowywaniu UI (Faza 4).
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Equalizer",
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Ustawienia", null, (_, _) => ShowSettings());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Zamknij", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;

        // Dwuklik w ikone traya = otworz ustawienia.
        _trayIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            // Zamkniecie okna tylko je chowa - aplikacja dalej zyje w trayu.
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.WindowState = WindowState.Normal;
        _settingsWindow.Activate();
    }

    private void ExitApp()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        Shutdown();
    }
}
