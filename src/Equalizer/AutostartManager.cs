using Microsoft.Win32;

namespace Equalizer;

/// <summary>
/// Autostart z systemem przez klucz Run biezacego uzytkownika (HKCU).
/// HKCU nie wymaga uprawnien administratora - swiadomy wybor, zeby Faza 0
/// nie potrzebowala UAC. (Rejestracja APO i jej UAC to dopiero Faza 1.)
/// </summary>
public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Equalizer";

    /// <summary>Sciezka do biezacego pliku .exe (dziala tez dla single-file publish).</summary>
    private static string? ExecutablePath => Environment.ProcessPath;

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var stored = key?.GetValue(ValueName) as string;
        if (string.IsNullOrEmpty(stored))
            return false;

        // Wpis moze byc nieaktualny (apka przeniesiona) - traktujemy taki jak wlaczony,
        // ale przy ponownym ustawieniu nadpiszemy go aktualna sciezka.
        return true;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            var path = ExecutablePath;
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Nie udalo sie ustalic sciezki do pliku exe.");

            key.SetValue(ValueName, $"\"{path}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
