# Equalizer — Faza 0 (szkielet)

Czysta aplikacja WPF startująca do zasobnika systemowego (tray). **Bez audio.**

## Co jest w tej fazie

- Aplikacja startuje do traya (ikona w zasobniku, brak okna na starcie).
- Menu traya: **Ustawienia** / **Zamknij**. Dwuklik w ikonę = Ustawienia.
- Okno ustawień z przełącznikiem **autostartu** (HKCU, bez admina).
- Zamknięcie okna nie zamyka aplikacji — dalej żyje w trayu.

## Wymagania

- Windows (build i uruchomienie tylko na Windows — WPF/WinForms).
- .NET SDK 8.0 (lub nowszy; wtedy zmień `TargetFramework` w `Equalizer.csproj`).

## Uruchomienie deweloperskie

```
dotnet build
dotnet run
```

## Zbudowanie pojedynczego pliku .exe (dwuklik i działa)

```
dotnet publish -c Release -r win-x64
```

Plik `Equalizer.exe` znajdziesz w `bin/Release/net8.0-windows/win-x64/publish/`.

## Definicja ukończenia Fazy 0

- [ ] Aplikacja startuje w tle (ikona w trayu, brak okna na starcie).
- [ ] Okno ustawień otwiera się z menu traya i z dwukliku.
- [ ] Przełącznik autostartu działa (wpis w HKCU\...\Run pojawia się i znika).
- [ ] Zamknięcie okna nie zamyka aplikacji; „Zamknij" z menu kończy ją czysto.
- [ ] Zero dźwięku — zgodnie z założeniem.
