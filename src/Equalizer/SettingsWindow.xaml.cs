using System.Windows;

namespace Equalizer;

/// <summary>
/// Okno ustawien. Faza 0: tylko przelacznik autostartu.
/// Suwaki pasm, presety i wskaznik stanu (aktywny/bypass) dojda w Fazie 4.
/// </summary>
public partial class SettingsWindow : Window
{
    // Zapobiega odpaleniu handlera przy programowym ustawianiu stanu checkboxa.
    private bool _initializing;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadState();
    }

    private void LoadState()
    {
        _initializing = true;
        AutostartCheckBox.IsChecked = AutostartManager.IsEnabled();
        _initializing = false;
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
            // MessageBox istnieje tez w WinForms - jawnie wskazujemy wersje WPF.
            System.Windows.MessageBox.Show(
                this,
                "Nie udało się zmienić autostartu:\n" + ex.Message,
                "Equalizer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Cofamy wizualnie do stanu faktycznego.
            LoadState();
        }
    }
}
