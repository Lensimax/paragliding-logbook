# ParagLog

Paragliding logbook: flight and ground-handling tracking with GPS track visualization.

See `SPEC.md` and `ARCHITECTURE.md` for the product spec and code layout.

## Development

Backend (ASP.NET Core, targets `net10.0`):

```
cd backend
dotnet build ParagLog.sln
dotnet test ParagLog.sln
```

Viewer (Vite + React + TypeScript):

```
cd viewer
npm install
npm run dev
npm run test
```

Full stack via Docker Compose:

```
cd deploy
docker compose up --build
```
