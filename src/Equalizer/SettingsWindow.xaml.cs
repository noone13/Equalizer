using System.Windows;
using NAudio.CoreAudioApi;

namespace Equalizer;

/// <summary>
/// Okno ustawien. Faza 1: wybor wejscia/wyjscia, Start/Stop, Bypass, status.
/// Silnik audio (AudioEngine) jest jeden na cala aplikacje i zyje tak dlugo
/// jak okno - przeniesiemy go "wyzej" (do App) gdy dojdzie praca w tle (Faza 5).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AudioEngine _engine = new();
    private bool _initializing;

    public SettingsWindow()
    {
        InitializeComponent();

        _engine.Status += OnEngineStatus;
        _engine.Error += OnEngineError;

        LoadDevices();
        LoadAutostartState();
    }

    private void LoadDevices()
    {
        CaptureCombo.ItemsSource = AudioDevices.Capture();
        RenderCombo.ItemsSource = AudioDevices.Render();

        // Podpowiedz: zaznacz "CABLE Output" jako wejscie, jesli istnieje.
        SelectByName(CaptureCombo, "CABLE Output");
        if (RenderCombo.Items.Count > 0 && RenderCombo.SelectedItem is null)
            RenderCombo.SelectedIndex = 0;
    }

    private static void SelectByName(System.Windows.Controls.ComboBox combo, string fragment)
    {
        foreach (var item in combo.Items)
        {
            if (item is MMDevice device &&
                device.FriendlyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private void LoadAutostartState()
    {
        _initializing = true;
        AutostartCheckBox.IsChecked = AutostartManager.IsEnabled();
        _initializing = false;
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.IsRunning)
        {
            _engine.Stop();
            StartStopButton.Content = "Start";
            StatusText.Text = "Zatrzymane.";
            return;
        }

        if (CaptureCombo.SelectedItem is not MMDevice capture ||
            RenderCombo.SelectedItem is not MMDevice render)
        {
            StatusText.Text = "Wybierz wejście i wyjście.";
            return;
        }

        _engine.Bypass = BypassCheckBox.IsChecked == true;
        _engine.Start(capture, render);

        if (_engine.IsRunning)
            StartStopButton.Content = "Stop";
    }

    private void BypassCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Tylko przelacza flage - przeliczanie/sciezka DSP poza watkiem audio (Faza 2).
        _engine.Bypass = BypassCheckBox.IsChecked == true;
    }

    private void OnEngineStatus(string message)
    {
        // Zdarzenia z silnika moga przyjsc z innego watku - wracamy na watek UI.
        Dispatcher.Invoke(() => StatusText.Text = message);
    }

    private void OnEngineError(Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            StartStopButton.Content = "Start";
            StatusText.Text = "Błąd: " + ex.Message;
        });
    }

    private void AutostartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing)
            return;

        try
        {
            AutostartManager.SetEnabled(AutostartCheckBox.IsChecked == true);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                this,
                "Nie udało się zmienić autostartu:\n" + ex.Message,
                "Equalizer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            LoadAutostartState();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Faza 1: silnik zyje z oknem. Zatrzymujemy dzwiek przy zamknieciu okna.
        // (W Fazie 5 silnik przeniesiemy do App, zeby gral bez otwartego okna.)
        _engine.Status -= OnEngineStatus;
        _engine.Error -= OnEngineError;
        _engine.Dispose();
        base.OnClosed(e);
    }
}
