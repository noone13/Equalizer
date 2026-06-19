# CLAUDE.md — zasady pracy w tym repo

Krótki przewodnik, żeby praca nie rozjechała się między sesjami. Pełna
specyfikacja: `project-md.md`. Wiążące decyzje (nadpisujące spec): `DECISIONS.md`.
Plan Fazy 1: `faza-1-md.md`.

## Co budujemy (w jednym zdaniu)

Systemowy equalizer audio na **Windows 11**, dla jednego użytkownika, który nie
konfiguruje systemu — ma być „otwieram i działa". Stack: **C# / .NET**.
Przechwytywanie/efekt przez **APO** (droga B — patrz `DECISIONS.md`), nie przez
wirtualny kabel.

## Zasady kodu (z `project-md.md`, sekcja "Zasady pracy z kodem")

- **Punktowe edycje, nie przepisywanie** działających fragmentów.
- Jak się czegoś nie da zrobić w danym podejściu — **powiedzieć wprost**,
  nie udawać działającego rozwiązania.
- **Każda faza zamknięta i przetestowana przed następną.** Małe, celowane kroki.

## Wątek audio — żelazne reguły

- **Zero alokacji pamięci** w gorącej pętli.
- **Zero blokowania** (locków, I/O).
- **Zero logów** w wątku audio — logowanie wyłącznie poza nim.
- Suwaki UI tylko ustawiają współczynniki; **przeliczanie filtrów poza wątkiem audio**.
- Rozmiar bufora i format próbek ustalany **raz przy starcie** strumienia;
  wewnętrznie **float 32-bit**.

## Bezpieczeństwo (krytyczny wymóg)

- **Bypass to ścieżka logiczna, nie kosmetyka.** Bypass aktywny LUB wyjątek/zawis
  DSP → sygnał przechodzi 1:1.
- **Watchdog:** jeśli przetwarzanie nie nadąża / pada → automatyczny fallback.
- **Aplikacja nigdy nie zostawia użytkownika z ciszą z powodu własnego błędu.**

## Tryb pracy z autorem

- Claude jest na **Linuksie** — nie zbuduje ani nie odsłucha aplikacji Windows.
- **Claude pisze kod → autor buduje i odsłuchuje → wraca z wynikiem**
  (działa / trzaski / echo / błąd). To jedyny tryb weryfikacji dla audio realtime.

## Status faz

- [ ] Faza 0 — szkielet (tray, okno ustawień, autostart). Zero audio.
- [ ] Faza 1 — przechwycenie + bypass (moment prawdy). Patrz `faza-1-md.md`.
- [ ] Faza 2 — DSP / EQ (biquad peaking, do 10 pasm).
- [ ] Faza 3 — obsługa wyjścia przewodowego + rozłączenie urządzenia (BT wycięty).
- [ ] Faza 4 — UI (ładne, schludne) + presety.
- [ ] Faza 5 — instalator (podpisany!) + praca w tle + czysta deinstalacja.

## Git

- Pracujemy na branchu: `claude/compassionate-pasteur-jmywij`.
- Nie tworzyć PR bez wyraźnej prośby autora.
