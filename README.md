# Northspan engine

A stateless pick-path planner. A warehouse system already has the order and the slot map. This service takes a floor, a list of location codes, and walking rules, and returns a cart walk plus the cost of a plain aisle sweep.

The [Northspan website](https://github.com/SukrutN/northspan) is the showcase for this planner. It is a separate product. Kiln Annex is fixture data in this repository, not a set of constants inside the solver.

## Requirements

.NET 10 SDK.

## Test

```bash
dotnet test
```

The characterization tests lock the Kiln Annex ticket MR-204 (Merrow Chandlery) to the walk published on the site: 110.0 m planned, 150.8 m sweep, five stops, 1.15 m/s, 1 min 36 sec. SH-118, PD-077, and NB-012 (Night desk) are covered the same way. A second floor, Dry Store, is a different graph.

## Plan a walk

```bash
dotnet run --project src/Northspan.Cli -- walk \
  --floor fixtures/kiln-annex/floor.json \
  --ticket fixtures/kiln-annex/tickets/mr-204.json
```

Optional `--rules <file>`. When the flag is omitted the demo clock is used: 1.15 m/s, return to the gate, no dwell at the face.

```
Kiln Annex — MR-204 Merrow Chandlery
Counter refill, ship Friday

#   Location    Run         Metres  Via
1   LOA-01-E    Loam          12.2  apron
2   REE-01-W    Reed          16.1  apron
3   COB-06-W    Cobalt        22.6  apron
4   FLI-06-E    Flint         20.2  rear
5   CIN-06-W    Cinder        20.2  rear

5 stops
Planned   110.0 m   1 min 36 sec
Sweep     150.8 m   before improvement
Shorter   40.8 m   27%
```

Dry Store is the same command with `fixtures/dry-store/floor.json` and `fixtures/dry-store/tickets/ds-014.json`.

## Run the API

```bash
dotnet run --project src/Northspan.Api
```

The development profile listens on `http://127.0.0.1:47231`.

- `GET /health`
- `GET /v1/floors`
- `GET /v1/floors/{floorId}`
- `GET /v1/floors/{floorId}/tickets`
- `POST /v1/walks`
- `GET /openapi/v1.json`

Plan the published walk from the named fixture:

```bash
curl -s -X POST http://127.0.0.1:47231/v1/walks \
  -H 'content-type: application/json' \
  -d '{"floorId":"kiln-annex","ticketId":"MR-204"}'
```

Send a floor inline instead of `floorId`, or `locations` instead of `ticketId`. An unknown location is HTTP 400 with problem details. The body names the code. The service does not invent a stop.

There is no authentication, database, or message bus. The caller brings the floor and the ticket.

## Layout

| Project | Responsibility |
| --- | --- |
| `src/Northspan.Domain` | Floor, aisle distance, nearest neighbour, two-opt, serpentine sweep |
| `src/Northspan.Application` | Plan-a-walk use case, file format, validation, walking time |
| `src/Northspan.Api` | HTTP API, OpenAPI, problem details, health |
| `src/Northspan.Cli` | `northspan walk` |
| `tests/Northspan.Domain.Tests` | Tour behaviour and the published tickets |
| `tests/Northspan.Api.Tests` | The sample walk and a bad mark, through the HTTP pipeline |

`fixtures/kiln-annex` is the building the website draws. `fixtures/dry-store` is a smaller floor with different runs, so a passing test cannot be a table of the five Kiln Annex names.

See [docs/architecture.md](docs/architecture.md), [docs/customizing-a-floor.md](docs/customizing-a-floor.md), and [docs/adr/0001-heuristic-versus-exact.md](docs/adr/0001-heuristic-versus-exact.md).
