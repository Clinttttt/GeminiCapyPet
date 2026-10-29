# Gate 1 characterization: 2026-10-29

Status: **partially implemented**. The test suite was added in a working tree based on
`19a9f1e`; it has not been committed, pushed, published, or interactively deployed.
This file records checks, not a claim that the full milestone is finished.

## Repeatable automated checks

On Windows with the .NET 10 SDK, from the repository root:

```powershell
dotnet test .\tests\Capies.Characterization.Tests\Capies.Characterization.Tests.csproj -c Release
dotnet build .\GeminiCapyPet.csproj -c Release
git diff --check
```

The first tests exercise actual production serialization and URL parsing with temporary
files and pure inputs, never real Edge profiles or user preferences.

| Area | Test evidence | Verified 2026-10-29 |
| --- | --- | --- |
| Default preferences | Missing test settings file returns default pose, 220 px, auto-sleep on, Gemini selected | Passed |
| Settings persistence | Round-trip pose, size, sleep, provider, and position; computed pose not serialized | Passed |
| Legacy validation | Invalid pose falls back; size below 140/above 360 clamps | Passed |
| ChatGPT URL | Personal/project conversation restore paths, query stripping, malformed/unsafe URL rejection | Passed |
| Gemini URL | Existing /app/ conversation URL normalization and invalid host/scheme/path rejection | Passed |

**Observed result:** 18 passed, 0 failed, 0 skipped. `dotnet build` reported
0 warnings and 0 errors in Release. These results cover serializer/URL rules,
not browser behavior or runtime switching.

## Manual Windows acceptance: NOT YET VERIFIED at this revision

Run these on a supported interactive Windows desktop. Record revision, Windows
version, Edge version, DPI, monitor count, warm/cold state, operation boundary,
sample size and observed result. Do not delete or replace real profiles.
A manual check is not passed until directly observed.

| Scenario | Existing expected behavior / difference | Status |
| --- | --- | --- |
| Launch and pet | One pet instance; transparency, drag/position, click, hover, animation | Not run |
| Menu inventory | Nine existing root commands; five poses; four size presets and custom | Not run |
| Menu accessibility | Arrow/Enter/Escape, disabled auto-sleep navigation, check state, focus, high-contrast | Not run |
| Provider selection | Menu selection persisted; pet click targets selected provider after restart | Not run |
| Gemini warm | Hide/show cached identified Gemini window, retaining position | Not run |
| ChatGPT warm | Hide/show cached ChatGPT; current code repositions on restore | Not run |
| Minimized windows | Observe legacy differences between menu/pet paths before intentionally fixing | Not run |
| Rapid clicks | Observe pending-toggle parity, overlap with provider switching and busy flag | Not run |
| Cold launch | Existing Edge app-mode profile reused, saved conversations restored | Not run |
| Bad/missing Edge | Capture failure behavior without modifying working installation | Not run |
| Auto-sleep | Automatic only, 45-second inactivity with both windows hidden | Not run |
| Shutdown | Pet and trackers exit; current code does not reveal its hidden app windows | Not run |

## Latency measurement protocol (pending)

Do not combine menu opening, UI dispatch, cached show/hide, WMI discovery,
cold process launch, and visible/foreground presentation into one result.
Use multiple trials for each boundary; record median, tail/max, sample count,
system conditions and source of timestamps. OS focus restrictions can affect
visible presentation independently from ShowWindow requests.

Historical context: an earlier external 25-sample menu harness reported
16.08 ms median / 32.57 ms max at 120 DPI. It was not rerun here and is
not a provider-switching benchmark.

## Gate status

Gate 1 remains **partial** until manual menu/keyboard/browser/placement checks
and repeatable latency measurements are completed. The suite provides a safe
baseline for subsequent coordinator extraction but is not a substitute for
concurrency, stale-HWND or visual Windows checks.
