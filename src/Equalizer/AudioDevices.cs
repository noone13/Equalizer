using NAudio.CoreAudioApi;

namespace Equalizer;

/// <summary>
/// Wyliczanie aktywnych urzadzen audio. Faza 1: uzytkownik wybiera recznie
/// "wejscie" (CABLE Output) i "wyjscie" (realne sluchawki). Pelna lista i
/// przelaczanie w locie to dopiero Faza 3.
/// </summary>
public static class AudioDevices
{
    /// <summary>Urzadzenia nagrywajace (stad przechwytujemy - np. "CABLE Output").</summary>
    public static List<MMDevice> Capture() => Enumerate(DataFlow.Capture);

    /// <summary>Urzadzenia odtwarzajace (tu oddajemy dzwiek - realne sluchawki).</summary>
    public static List<MMDevice> Render() => Enumerate(DataFlow.Render);

    private static List<MMDevice> Enumerate(DataFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        var list = new List<MMDevice>();
        foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            list.Add(device);
        return list;
    }
}
