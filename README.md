# UDPGame – prediction, reconciliation og interpolation

Projektet er en bevidst lille demonstration af netværkskoncepterne fra
præsentationens slide 57–74. Der er én autoritativ server, to klienter og én
bold. Den første klient, der joiner, ejer bolden.

Klientens rendering kører uafhængigt og så hurtigt som maskinen tillader.
Lokalt input simuleres og sendes stabilt med 60 Hz, mens serverens tickrate kan
være langt lavere. Klienten viser både sin FPS og serverens tickrate på skærmen.

## Det vigtigste dataflow

1. Ejeren sender nummererede bevægelsesinput – aldrig en position.
2. Serverens receive-tråd lægger input i en trådsikker kø.
3. Tick-tråden behandler input og opdaterer den autoritative position.
4. Serveren sender snapshots til alle klienter.
5. Ejeren bruger prediction og reconciliation.
6. Klienter interpolerer objekter, de ikke selv ejer.

## Hvor findes koncepterne?

- `UDPGame/GameObjects/NetworkGameObject.cs`: prediction, reconciliation,
  inputhistorik, snapshot-rækkefølge og interpolation for alle netværksobjekter.
- `UDPGame/GameObjects/Ball.cs`: oversætter kun W/A/S/D til bevægelsesinput.
- `UDPGameServer/Server.cs`: receive-tråd, tick/snapshot-tråd, autoritet og join.
- `UDPGameServer/GameWorld.cs`: den autoritative tilstand og bevægelsesregel.
- `UDPGameShared/NetworkMessages.cs`: de fem små delte beskedtyper.
- `UDPGameShared/MessageSerializer.cs`: den simple serialisering: én type-byte
  efterfulgt af beskedens JSON-data.

Tickrate sendes i en separat `ServerSettingsMessage` ved join og ved ændringer;
den gentages ikke i hvert snapshot.

## Kør demoen

Kør `StartUp.bat`. Scriptet bygger løsningen og starter én server og to klienter.

På klienten:

- `W` / `A` / `S` / `D`: flyt bolden i begge dimensioner (kun ejeren).
- `P`: slå prediction til/fra.
- `R`: slå reconciliation til/fra.
- `I`: slå interpolation til/fra.
- `+` / `-`: ændr simuleret latency med 25 ms i begge retninger.

I serverens konsol:

- `tick 3`: sæt serverens tickrate til 3 Hz.
- `tick 20`: sæt serverens tickrate til 20 Hz.
