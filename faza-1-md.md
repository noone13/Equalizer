# FAZA_1.md — Loopback + Bypass (moment prawdy)

## Po co ta faza istnieje

To jest punkt, który decyduje o powodzeniu całego projektu. EQ (Faza 2) to matematyka na dzień roboty. Tutaj rozstrzyga się jedno pytanie: **czy potrafimy przepuścić cały dźwięk systemowy przez naszą aplikację i oddać go dalej bez echa, sprzężenia i podwójnego odtwarzania?** Jak to działa stabilnie — reszta projektu to składanie klocków.

Cel fazy: dźwięk z dowolnego źródła (Spotify, przeglądarka, cokolwiek) przechodzi 1:1 przez aplikację na wyjście. Zero DSP. Plus działający bypass z fallbackiem.

## Dlaczego sam WASAPI loopback nie wystarczy

Naiwny pomysł brzmi: "złapię loopback z domyślnego urządzenia i oddam na to samo urządzenie". To nie zadziała i trzeba rozumieć dlaczego:

- WASAPI loopback nasłuchuje tego, co leci **na** dane urządzenie wyjściowe.
- Jeśli oddasz przetworzony sygnał na to **samo** urządzenie, twój output znów trafia do loopbacku → łapiesz własny sygnał → **sprzężenie / echo / narastający szum**.
- Dodatkowo: oryginalne źródło dalej gra na to urządzenie równolegle z tobą → **podwójne odtwarzanie**.

Wniosek: źródło przechwytywania i cel odtwarzania **muszą być różnymi urządzeniami**. Stąd wirtualne urządzenie.

## Właściwa architektura przepływu sygnału

```
[Spotify / przeglądarka / cokolwiek]
              │
              ▼
   ┌──────────────────────┐
   │ WIRTUALNE URZĄDZENIE  │  ← ustawione jako DOMYŚLNE wyjście systemu
   │ (np. open-source VAD) │     System myśli, że to "głośniki".
   └──────────────────────┘     Nic nie gra fizycznie — to atrapa.
              │
              │  WASAPI loopback (przechwytujemy stąd)
              ▼
   ┌──────────────────────┐
   │   NASZA APLIKACJA     │
   │  ┌────────────────┐   │
   │  │  bypass? ──tak─┼───┼──► przepuść 1:1
   │  │     │ nie      │   │
   │  │     ▼          │   │
   │  │  [DSP / EQ]    │   │   ← w Fazie 1 jeszcze puste
   │  └────────────────┘   │
   └──────────────────────┘
              │
              │  WASAPI render (oddajemy tutaj)
              ▼
   ┌──────────────────────┐
   │  REALNE WYJŚCIE       │  ← słuchawki przewodowe / Bluetooth
   │  (fizyczny dźwięk)    │     Wybierane w aplikacji, NIE w systemie.
   └──────────────────────┘
```

Klucz: system widzi jako "domyślne" urządzenie **wirtualne** (atrapę). Realne słuchawki są celem, który wybiera **nasza aplikacja**, a nie Windows. Dzięki temu przechwytywanie i odtwarzanie to dwa różne urządzenia → brak sprzężenia.

## Krok po kroku

### 1. Wirtualne urządzenie audio
- Wybrać i zainstalować gotowe, open-source wirtualne urządzenie wyjściowe (decyzja do domknięcia PRZED kodem — patrz `PROJECT.md`, ryzyko #3).
- Ustawić je jako domyślne wyjście systemu (ręcznie na czas dewelopmentu; automatycznie dopiero w Fazie 5 / instalatorze).
- **Test:** odtwórz cokolwiek → cisza w słuchawkach (bo dźwięk leci do atrapy). To jest oczekiwany, poprawny stan.

### 2. Przechwytywanie (capture)
- NAudio: `WasapiCapture` w trybie loopback na wirtualnym urządzeniu.
- Odczytać i zapamiętać format strumienia (sample rate, liczba kanałów, bit depth). NIE zakładać na sztywno.
- Wewnętrznie pracować na **float 32-bit** — konwersja przy wejściu, jeśli źródło ma inny format.

### 3. Bufor pośredni
- Ring buffer / kolejka między wątkiem przechwytywania a wątkiem odtwarzania.
- Rozmiar: kompromis między latencją a stabilnością. Start: mały, zwiększać tylko jeśli słychać przerywanie.
- Wątek audio NIE alokuje pamięci — bufory przydzielone z góry.

### 4. Odtwarzanie (render)
- NAudio: `WasapiOut` na **realnym** urządzeniu wyjściowym (na razie wybrane na sztywno w kodzie; lista urządzeń dopiero w Fazie 3).
- Format wyjścia dopasowany do formatu przechwytywania (resampling, jeśli urządzenia różnią się sample rate — przewidzieć, choć w Fazie 1 można wymusić zgodność).

### 5. Bypass (od razu, nie później)
- Flaga `bypass` (atomowa / volatile).
- `bypass = true` → próbki kopiowane wejście→wyjście bez dotykania.
- `bypass = false` → w Fazie 1 to samo (DSP jeszcze puste), ale ścieżka logiczna już istnieje i jest gotowa na Fazę 2.
- Przełącznik dostępny z traya i z okna.

### 6. Watchdog / fallback
- Jeśli wątek przetwarzania rzuci wyjątek lub przestanie nadążać → automatyczne przejście w bypass, sygnał leci dalej.
- Aplikacja **nigdy** nie zostawia użytkownika z ciszą z powodu własnego błędu.
- Log zdarzenia poza gorącą pętlą (nie w wątku audio).

## Definicja ukończenia Fazy 1

Wszystkie poniższe muszą być spełnione, zanim ruszymy do Fazy 2:

- [ ] Dźwięk z dowolnego źródła (np. Spotify + przeglądarka jednocześnie) przechodzi przez aplikację i gra w realnym wyjściu.
- [ ] Brak echa, sprzężenia, narastającego szumu.
- [ ] Brak podwójnego odtwarzania.
- [ ] Bypass przełącza się natychmiast, bez trzasku, bez przerwy w dźwięku.
- [ ] Wymuszony błąd w ścieżce przetwarzania → automatyczny fallback do bypassu, dźwięk gra dalej.
- [ ] Latencja akceptowalna do słuchania muzyki (nie mierzymy jeszcze BT — to Faza 3).

## Pułapki, które się pojawią (uprzedzam)

- **Sample rate mismatch.** Wirtualne urządzenie i realne słuchawki mogą mieć różny sample rate. Albo wymusić zgodny (Faza 1), albo resampling (najpóźniej Faza 3).
- **Wyłączność (exclusive mode).** Niektóre aplikacje próbują przejąć urządzenie na wyłączność. Trzymać się trybu współdzielonego (shared).
- **Rozłączenie urządzenia w trakcie.** Wyjęcie słuchawek w Fazie 1 może wywalić strumień. Pełna obsługa dopiero Faza 3, ale warto już nie ignorować wyjątku.
- **Latencja vs przerywanie.** Za mały bufor → trzaski. Za duży → opóźnienie. Stroić dopiero gdy reszta działa.

## Czego w tej fazie NIE robimy

- Żadnego DSP / EQ — to Faza 2.
- Żadnej listy urządzeń ani przełączania w UI — to Faza 3.
- Żadnego instalatora ani automatycznego ustawiania domyślnego urządzenia — to Faza 5.
- Realne wyjście wybrane na sztywno w kodzie. Wystarczy, że gra.
