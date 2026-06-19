# PROJECT.md — Systemowy Equalizer Audio (Windows)

## Cel projektu

Standalone aplikacja na Windows, która przechwytuje **cały** dźwięk systemowy (Spotify, przeglądarka, dowolny program), przepuszcza go przez wielopasmowy equalizer (DSP w czasie rzeczywistym) i oddaje na wybrane wyjście (słuchówki przewodowe lub Bluetooth). Aplikacja działa w tle (tray), startuje z systemem, ma instalator.

## Kluczowe wymagania funkcjonalne

1. Przechwytywanie całego dźwięku systemowego, niezależnie od źródła.
2. Wielopasmowy EQ (docelowo 10 pasm) z regulacją w czasie rzeczywistym.
3. Wybór urządzenia wyjściowego (przewodowe / Bluetooth).
4. **BYPASS** — natychmiastowe ominięcie DSP, dźwięk leci czysty. Krytyczny wymóg bezpieczeństwa (patrz niżej).
5. Praca w tle: tray icon, autostart, okno ustawień otwierane na żądanie.
6. Presety EQ: zapis / wczytanie / przełączanie.
7. Instalator (Inno Setup).

## Stack technologiczny

- **Język / platforma:** C# / .NET (najnowszy stabilny .NET).
- **Audio:** NAudio (WASAPI loopback + output). Dojrzała, ogromna baza przykładów.
- **DSP:** własne filtry biquad w czystym C# (peaking EQ per pasmo).
- **UI:** WPF lub WinUI dla okna ustawień; tray przez NotifyIcon / odpowiednik.
- **Instalator:** Inno Setup.
- **Warstwa przechwytywania:** preferowane gotowe, open-source wirtualne urządzenie audio jako "domyślne" wyjście systemu; aplikacja działa jako most między nim a realnym wyjściem. NIE piszemy własnego sterownika jądra.

## Decyzje architektoniczne (NIE zmieniać bez rozmowy)

- **Bez sterownika trybu jądra.** Cała logika DSP żyje w userspace. Przechwytywanie opiera się na gotowym wirtualnym urządzeniu audio, nie na własnym sterowniku.
- **Bypass to ścieżka sprzętowo-logiczna, nie kosmetyka.** Gdy bypass jest aktywny LUB gdy DSP rzuci wyjątek / zawiesi się, sygnał musi przejść 1:1 na wyjście. Aplikacja nigdy nie może zostawić użytkownika bez dźwięku z powodu własnego błędu. Watchdog: jeśli pętla DSP nie nadąża / pada, automatyczny fallback do bypassu.
- **Stała struktura buforów.** Rozmiar bufora i format próbek ustalany raz przy starcie strumienia; DSP operuje na znanym formacie (preferowane float 32-bit wewnętrznie).
- **DSP rozdzielony od UI.** Suwaki UI tylko ustawiają współczynniki; przeliczanie filtrów dzieje się poza wątkiem audio. Wątek audio nigdy nie alokuje pamięci i nie blokuje.

## Fazy realizacji

Każda faza musi się budować i być testowalna **osobno**, zanim ruszymy dalej. Małe, celowane kroki. Nie przepisywać działającego kodu — robić punktowe zmiany.

### Faza 0 — Szkielet
- Pusta aplikacja .NET startująca do traya.
- Okno ustawień (otwierane z menu traya).
- Autostart z systemem (do przełączenia w UI).
- **Definicja ukończenia:** aplikacja startuje w tle, autostart działa, okno się otwiera. Zero audio.

### Faza 1 — Loopback + Bypass (MOMENT PRAWDY)
- Przez NAudio: przechwycenie systemowego audio i oddanie 1:1 na wyjście, bez przetwarzania.
- Od razu wbudowany przełącznik **bypass** (w tej fazie bypass = całość, bo DSP jeszcze nie ma — ale ścieżka logiczna i fallback istnieją).
- Obsługa sprzężeń / podwójnego odtwarzania (dlatego wirtualne urządzenie jako domyślne, a apka jako most).
- **Definicja ukończenia:** dźwięk z dowolnego źródła przechodzi przez aplikację, brak zniekształceń, brak echa, bypass działa. Jeśli ta faza działa stabilnie — reszta jest prosta.

### Faza 2 — DSP (EQ)
- Filtr biquad (peaking) — najpierw jedno pasmo.
- Rozszerzenie do 10 pasm (standardowe częstotliwości środkowe).
- Współczynniki przeliczane poza wątkiem audio; wątek audio tylko aplikuje.
- **Definicja ukończenia:** ruch suwakiem słyszalnie zmienia brzmienie, bez trzasków przy zmianie, bypass wciąż natychmiastowy.

### Faza 3 — Wybór wyjścia
- Lista urządzeń wyjściowych, przełączanie w locie.
- Obsługa rozłączenia urządzenia (np. wyjęcie słuchawek) bez crashu.
- **Bluetooth:** pomiar realnej latencji. Latencja kodeka BT jest niezależna od naszego kodu — udokumentować, nie próbować "naprawiać".
- **Definicja ukończenia:** przełączanie słuchawki ↔ BT działa, rozłączenie nie wywala apki.

### Faza 4 — UI + presety
- Okno z suwakami pasm, czytelny układ.
- Zapis / wczytanie / przełączanie presetów.
- Wskaźnik stanu (aktywny / bypass).
- **Definicja ukończenia:** pełny interfejs, presety trwałe między uruchomieniami.

### Faza 5 — Instalator + praca w tle
- Inno Setup: instalacja aplikacji + (jeśli potrzeba) wirtualnego urządzenia audio.
- Autostart jako proces w tle.
- Czysta deinstalacja (przywrócenie domyślnego urządzenia systemu!).
- **Definicja ukończenia:** świeża instalacja na czystym systemie działa od kliknięcia do grającego EQ; deinstalacja nie zostawia systemu bez dźwięku.

## Znane ryzyka (uczciwie)

1. **Faza 1 to wąskie gardło całego projektu.** Czyste przepuszczenie audio przez aplikację bez sprzężenia/echa wymaga wirtualnego urządzenia jako domyślnego wyjścia. To jest najtrudniejszy punkt, nie EQ.
2. **Latencja Bluetooth** będzie zauważalna (kodek SBC/AAC/aptX) i niezależna od naszego kodu. OK dla muzyki, problem dla wideo/gier. Tylko dokumentujemy.
3. **Wirtualne urządzenie audio** może wymagać własnego instalatora / uprawnień administratora. Wybór konkretnego open-source rozwiązania trzeba domknąć przed Fazą 1.
4. **Format / sample rate.** Różne źródła mogą mieć różny sample rate; resampling do wspólnego formatu musi być przewidziany.

## Zasady pracy z kodem (preferencje)

- Punktowe edycje zamiast przepisywania działających fragmentów.
- Jak coś się nie da zrobić w danym podejściu — powiedzieć wprost, nie udawać działającego rozwiązania.
- Każda faza zamknięta i przetestowana przed następną.
- Wątek audio: zero alokacji, zero blokowania, zero logów w gorącej pętli.
