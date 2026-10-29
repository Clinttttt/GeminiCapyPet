# ADR 0001: Shared orchestration for managed apps

Date: 2026-10-29 (Asia/Singapore)
Status: **accepted architectural direction; implementation pending**
Source baseline: 8d98105

## Context

Capies currently coordinates two browser apps from PetWindow. Their controllers
duplicate discovery and Win32 operations. Earlier latency fixes established the
importance of cached access and avoiding cross-provider discovery. Adding more
software through more UI branches would multiply these dependencies.

The user requested a clean, extensible OOP architecture and repository guidance
that distinguishes verified facts from proposed work.

## Decision

Keep one running Windows desktop application. Move toward Core, Windows, and Desktop
project boundaries with a shared AppCoordinator, registered app definitions,
capability-aware adapters, reusable window services, and optional session restorers.

Use composition and constructor injection. Keep runtime sessions separate from
persistent definitions. Serialize state transitions with desired-state and generation
tracking while slow acquisition runs outside the transition loop.

All UI entry points use the same commands. Top-right software-window placement is
a shared policy; a future app panel is a separate UI concern.

Use an incremental migration with characterization first, thin wrappers second,
then shared infrastructure and a third desktop program before app-management UI.

## Alternatives considered

- More provider-specific methods in PetWindow: initially small edits, but grows
  pairwise dependencies and duplicates behavior/performance fixes.
- A large inherited base controller: centralizes code but couples unrelated
  browser-session, launch, and placement responsibilities.
- Third-party plugin loading now: adds compatibility, packaging, and lifecycle
  obligations before the internal contracts have been proven.
- A full UI/runtime rewrite first: makes behavior regressions harder to isolate.

## Consequences

The initial refactor adds a few contracts and tests before new user-visible features.
Ordinary supported apps can later be configuration-driven; unusual programs may need
specialized adapters. Capability differences remain explicit.

The architecture does not guarantee Windows foreground permission or universal
window compatibility. Performance requires measurement and a cache/discovery policy,
not merely new folders.

The split is a target, not a claim about current files. Preserve executable identity,
settings, profiles, and deployment paths unless an intentional migration changes them.

## Follow-up

Use [architecture](../architecture.md) for boundaries and
[roadmap](../roadmap.md) for gates. When changing this decision materially, add a
superseding ADR and link it here rather than silently rewriting the historical rationale.
