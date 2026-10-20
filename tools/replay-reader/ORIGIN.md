# CasterDesk replay reader

Local-only experimental adapter based on julio208920/r6-dissect v1.7.0,
commit 6602cf2e2d72e645cf2b2a3a801fa953cf21903f:
https://github.com/julio208920/r6-dissect

Original project: https://github.com/redraskal/r6-dissect
MIT license and upstream copyright are retained in LICENSE.

CasterDesk changes: minimal command-line entry point, bounded runtime supervised
by the desktop app, and nullable initial/final cumulative scoreboard counters
in dissect/casterdesk.go, plus bounded header-only metadata reading for folder labels
in dissect/casterdesk_header.go. No upload or account integration is used.
Only parser sources are included; the fork's application frontend is not bundled.

Build: scripts/Build-ReplayReader.ps1 -Go <go.exe>. The executable is generated
under build-config and is included in the application's assets/replay directory.
Replay files used for local validation are private data and are not committed.
