# DECISIONS.md — ustalenia wiążące

Ten plik notuje decyzje podjęte w trakcie rozmowy, które **nadpisują lub
doprecyzowują** `project-md.md`. Jeśli coś tu jest sprzeczne z `project-md.md`,
ten plik wygrywa. Nie zmieniać bez rozmowy.

## Dla kogo i po co

- Aplikacja powstaje dla **konkretnego użytkownika** (ojca autora, jedna maszyna).
- Użytkownik **nie zna się na konfiguracji systemu** — nie chodzi o wzrok ani
  o sprawność, tylko o to, że nie ma obsługiwać skomplikowanych ustawień Windows.
- Codzienne użycie musi sprowadzać się do: **włącza komputer → dźwięk gra przez EQ**.
  Żadnego wybierania urządzeń, żadnej konfiguracji.

## Zakres — zmiany względem `project-md.md`

- **Bluetooth: WYCIĘTY.** Tylko wyjście przewodowe. To upraszcza architekturę
  i odblokowuje drogę APO (patrz niżej). Faza 3 ogranicza się do obsługi
  przewodowego wyjścia i rozłączenia urządzenia.

## Decyzja architektoniczna: droga B — APO

Zamiast wirtualnego urządzenia + mostu (droga A) wybieramy **własny APO
(Audio Processing Object)**, model jak Equalizer APO.

**Dlaczego, mimo że `project-md.md` pierwotnie zakładał wirtualne urządzenie:**

- **Tryb awarii.** Przy wirtualnym kablu domyślnym wyjściem systemu jest atrapa —
  jeśli nasza apka nie wstanie / padnie, użytkownik zostaje z **ciszą**, której
  sam nie naprawi. Przy APO domyślnym wyjściem zostaje realne urządzenie, więc
  awaria degraduje się do **„dźwięk gra, tylko bez EQ"** — co jest akceptowalne.
- **Przezroczystość.** APO wpina się w łańcuch efektów Audio Engine (user mode,
  audiodg.exe) bez atrapy — efekt „otwieram i działa" bez konfiguracji.
- **Koszt licencji.** APO (model open-source) jest darmowy także do dystrybucji;
  wirtualny kabel (VB-CABLE) wymagałby płatnej licencji na redystrybucję.
- **Bluetooth był jedynym poważnym minusem APO — a został wycięty.**

**Świadomy koszt tej decyzji:**

- APO to model Microsoftu — rejestracja per-endpoint, własna struktura DLL,
  inny kształt kodu niż NAudio + most.
- Crash APO może chwilowo uderzyć w audiodg (Windows się z tego podnosi,
  wyłączając wadliwy APO → degradacja do „bez EQ").
- Watchdog/bypass nadal wymagane (wymóg bezpieczeństwa z `project-md.md`).

## Środowisko docelowe

- **System: Windows 11.** APO jest wrażliwe na wersję systemu — sposób
  rejestracji efektu na Win11 trzeba potwierdzić przed Fazą 1.
- **Instalacja: użytkownik instaluje samodzielnie** (stan na teraz).
  To najtrudniejszy wariant — patrz ryzyka.

## Najtrudniejszy element projektu = INSTALATOR (nie EQ)

Po wycięciu BT i wyborze APO główne ryzyko przeniosło się z DSP na instalację:

1. **SmartScreen.** Niepodpisany instalator na Win11 pokazuje ostrzeżenie
   „System Windows ochronił Twój komputer". Użytkownik może się przestraszyć
   i zrezygnować. → Potrzebny **certyfikat code-signing** (koszt + weryfikacja
   tożsamości; do kupienia przez autora — Claude tego nie wygeneruje).
2. **UAC / admin.** Rejestracja APO wymaga uprawnień administratora —
   jedno okno UAC, instalator musi przygotować użytkownika.
3. **Restart.** Wpięcie APO zwykle wymaga restartu — instalator musi
   przeprowadzić użytkownika przez to za rękę.

**Rekomendacja (otwarta):** jednorazowa instalacja zdalna przez autora
(AnyDesk/TeamViewer) kasuje praktycznie całe to ryzyko. Wariant „użytkownik
instaluje sam" pozostaje najtrudniejszy.

## UI

- **Ładny, schludny, normalny interfejs.** NIE przewymiarowane suwaki „dla seniora".
- Presety nazwane po ludzku (np. „Muzyka", „Film", „Wyraźniejsza mowa").
- Codzienne użycie bez konieczności jakiejkolwiek konfiguracji.

## Ograniczenie środowiska deweloperskiego

- Claude pracuje na **Linuksie** — **nie zbuduje ani nie odsłucha** aplikacji
  Windows. Kod pisze Claude; budowanie, uruchamianie i odsłuch po stronie autora.
