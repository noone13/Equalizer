using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Equalizer;

/// <summary>
/// Faza 1 - moment prawdy. Przechwytuje dzwiek z urzadzenia "wejsciowego"
/// (CABLE Output, do ktorego system kieruje caly dzwiek) i oddaje go 1:1 na
/// realne "wyjscie" (sluchawki). Brak DSP.
///
/// Architektura sygnalu:
///   System -> CABLE Input (domyslne wyjscie) -> CABLE Output (nagrywanie)
///          -> [ta klasa: capture -> bufor -> render] -> realne sluchawki
///
/// UWAGA Faza 1: dla "momentu prawdy" liczy sie poprawnosc, nie mikro-optymalizacja.
/// Uzywamy BufferedWaveProvider (ma wewnetrzny lock) - to sprawdzony wzorzec NAudio.
/// Bezlockowy ring buffer w goracej petli to swiadomie ODLOZONA optymalizacja,
/// do zrobienia PO potwierdzeniu, ze przepuszczanie dziala bez echa.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private WasapiCapture? _capture;
    private WasapiOut? _output;
    private BufferedWaveProvider? _buffer;

    private MMDevice? _captureDevice;
    private MMDevice? _renderDevice;

    // Bypass: w Fazie 1 brak slyszalnej roznicy (DSP jeszcze nie ma), ale flaga i
    // sciezka logiczna juz istnieja - gotowe na Faze 2. volatile, bo czytana z watku audio.
    private volatile bool _bypass = true;
    public bool Bypass
    {
        get => _bypass;
        set => _bypass = value;
    }

    public bool IsRunning { get; private set; }

    // Zdarzenia raportowane POZA watkiem audio (do UI / logu). Nigdy z goracej petli.
    public event Action<string>? Status;
    public event Action<Exception>? Error;

    public void Start(MMDevice captureDevice, MMDevice renderDevice)
    {
        Stop();

        try
        {
            _captureDevice = captureDevice;
            _renderDevice = renderDevice;

            _capture = new WasapiCapture(captureDevice);
            var captureFormat = _capture.WaveFormat;

            _buffer = new BufferedWaveProvider(captureFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(500),
                // Wolimy zgubic probki niz pozwolic urosnac opoznieniu (echo/lag).
                DiscardOnBufferOverflow = true,
            };

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;

            // WAZNE: wyjscie to JAWNIE wybrane urzadzenie, NIGDY domyslne -
            // bo domyslnym jest teraz wirtualny kabel => oddanie tam = sprzezenie.
            _output = new WasapiOut(renderDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);

            // Dopasowanie formatu kabla do formatu wyjscia (najczestsza roznica: sample rate).
            ISampleProvider source = _buffer.ToSampleProvider();
            var mix = renderDevice.AudioClient.MixFormat;
            if (source.WaveFormat.SampleRate != mix.SampleRate)
                source = new WdlResamplingSampleProvider(source, mix.SampleRate);

            _output.Init(source);

            _capture.StartRecording();
            _output.Play();

            IsRunning = true;
            Status?.Invoke($"Gra: {captureDevice.FriendlyName} -> {renderDevice.FriendlyName} " +
                           $"({captureFormat.SampleRate} Hz, {captureFormat.Channels} kan.)");
        }
        catch (Exception ex)
        {
            // Watchdog/bezpieczenstwo: nie wywalamy aplikacji. Sprzatamy i raportujemy.
            Stop();
            RaiseError(ex);
        }
    }

    public void Stop()
    {
        IsRunning = false;

        if (_capture is not null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            try { _capture.StopRecording(); } catch { /* ignorujemy przy zamykaniu */ }
            _capture.Dispose();
            _capture = null;
        }

        if (_output is not null)
        {
            try { _output.Stop(); } catch { /* ignorujemy przy zamykaniu */ }
            _output.Dispose();
            _output = null;
        }

        _buffer = null;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var buffer = _buffer;
        if (buffer is null)
            return;

        // Faza 1: bypass i nie-bypass robia DOKLADNIE to samo (brak DSP).
        // Po prostu przekazujemy probki dalej. Zero przetwarzania, zero logow.
        buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // Np. wyjete sluchawki / rozlaczone urzadzenie. Pelna obsluga to Faza 3,
        // ale juz teraz nie ignorujemy bledu - raportujemy poza watkiem audio.
        if (e.Exception is not null)
            RaiseError(e.Exception);
    }

    private void RaiseError(Exception ex)
    {
        var handler = Error;
        if (handler is not null)
            handler(ex);
        else
            Status?.Invoke("Blad: " + ex.Message);
    }

    public void Dispose() => Stop();
}
