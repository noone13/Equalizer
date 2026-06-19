# Equalizer — Faza 0 (szkielet)

Czysta aplikacja WPF startująca do zasobnika systemowego (tray). **Bez audio.**

## Co jest w tej fazie

- Aplikacja startuje do traya (ikona w zasobniku, brak okna na starcie).
- Menu traya: **Ustawienia** / **Zamknij**. Dwuklik w ikonę = Ustawienia.
- Okno ustawień z przełącznikiem **autostartu** (HKCU, bez admina).
- Zamknięcie okna nie zamyka aplikacji — dalej żyje w trayu.

## Wymagania

- Windows (build i uruchomienie tylko na Windows — WPF/WinForms).
- .NET SDK 10.0 (lub inny; wtedy zmień `TargetFramework` w `Equalizer.csproj`).

## Uruchomienie deweloperskie

```
dotnet build
dotnet run
```

## Zbudowanie pojedynczego pliku .exe (dwuklik i działa)

```
dotnet publish -c Release -r win-x64
```

Plik `Equalizer.exe` znajdziesz w `bin/Release/net10.0-windows/win-x64/publish/`.

## Definicja ukończenia Fazy 0

- [x] Aplikacja startuje w tle (ikona w trayu, brak okna na starcie).
- [x] Okno ustawień otwiera się z menu traya i z dwukliku.
- [x] Przełącznik autostartu działa (wpis w HKCU\...\Run pojawia się i znika).
- [x] Zamknięcie okna nie zamyka aplikacji; „Zamknij" z menu kończy ją czysto.
- [x] Zero dźwięku — zgodnie z założeniem.

---

# Faza 1 — przechwycenie + bypass (moment prawdy)

Dźwięk systemowy przechodzi przez aplikację 1:1 na realne wyjście, bez DSP.
Pełny opis: `../../faza-1-md.md`.

## Wymóg wstępny: wirtualny kabel

1. Zainstaluj **VB-CABLE**: https://vb-audio.com/Cable/ (`VBCABLE_Setup_x64.exe`
   jako administrator), zrestartuj komputer.
2. Pojawią się urządzenia **„CABLE Input”** (odtwarzanie) i **„CABLE Output”**
   (nagrywanie).

## Jak przetestować

1. Uruchom apkę (`dotnet run`), otwórz okno ustawień.
2. **Wejście** → wybierz „CABLE Output” (podpowiada się samo).
   **Wyjście** → wybierz swoje realne słuchawki (NIE kabel!).
3. Kliknij **Start**. Status pokaże format i kierunek przepływu.
4. Teraz ustaw **CABLE Input** jako **domyślne wyjście systemu**
   (prawy klik na głośnik → Ustawienia dźwięku → urządzenie wyjściowe).
5. Odtwórz cokolwiek (YouTube, Spotify). Dźwięk powinien lecieć **w słuchawki**,
   przez naszą apkę.

> Po teście pamiętaj przywrócić swoje słuchawki jako domyślne wyjście systemu —
> inaczej bez uruchomionej apki nie usłyszysz dźwięku (domyślnym jest kabel).

## Definicja ukończenia Fazy 1

- [ ] Dźwięk z dowolnego źródła przechodzi przez apkę i gra w słuchawkach.
- [ ] Brak echa, sprzężenia, narastającego szumu.
- [ ] Brak podwójnego odtwarzania.
- [ ] Bypass włączony/wyłączony — w tej fazie brzmi tak samo (DSP jeszcze nie ma),
      ale przełącznik nie powoduje trzasku ani przerwy.
- [ ] Stop zatrzymuje czysto, Start wznawia.
- [ ] Latencja akceptowalna do słuchania muzyki.
