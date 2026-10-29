---
name: capies-development
description: Develop, refactor, extend, or review GeminiCapyPet using verified repository evidence, shared app orchestration, focused adapters, and behavior-preserving migration.
---

# Capies development workflow

This is a repository-local skill, not an installed global plugin. Its folder and
[AGENTS.md](../../../AGENTS.md) entry make it discoverable to compatible assistants;
other tools can follow it manually. Documentation does not enforce behavior by itself.

## When to use

Use for Capies app integrations, switching/window behavior, pet/tray/settings changes,
architecture refactoring, or consistency reviews. A documentation-only task can use
the evidence and documentation checks without running application workflows.

## Required context

Read [AGENTS.md](../../../AGENTS.md) and the
[documentation index](../../../docs/README.md). Follow the index to current-state,
architecture, patterns, roadmap, and validation. Read relevant source and call sites.

Explicit user instructions and higher-priority host instructions take precedence.
Honor existing task authorization. This skill imposes no extra permission step and
does not authorize unrelated deployment, messaging, or destructive actions.

## Workflow

1. Establish facts: inspect git status/HEAD, project layout, applicable instructions,
   and the relevant implementation. Preserve unrelated user edits.
2. Separate statements into implemented, proposed, and unknown. Reconcile stale
   documentation with source; never invent a type/API/test because it appears in a plan.
3. Choose the smallest relevant migration gate. State what behavior is preserved
   and which intentional change needs its own acceptance evidence.
4. Identify the owner: coordinator for policy, adapter for acquisition/identity,
   window service for native operations, UI for presentation, persistence for storage.
5. Implement through existing boundaries where possible. During extraction, wrappers
   may retain documented legacy behavior; do not falsely claim a shared service exists.
6. Keep UI state reads cheap. Avoid discovery/SQL/file work in rendering/getters;
   use explicit background work, bounded waits, validated cache entries, and stale-result checks.
7. Preserve current menu commands, keyboard/check behavior, profile locations,
   conversation restoration, and preferences unless the task explicitly changes them.
8. Run the checks appropriate to the change from
   [validation](../../../docs/validation.md). Add meaningful tests for new orchestration,
   migration, identity, or cancellation logic.
9. Update docs/status/ADRs to reflect the actual implementation. Check relative links
   and ensure future work remains labeled proposed.
10. Within authorized scope, finish commit/push/deployment and verify each outcome.
    Docs-only work does not require publishing or restarting the executable.

## App integration recipe

First determine whether an existing adapter really supports the program. Prefer a
definition over new code. If needed, add one focused adapter; keep switching policy
out of it. Register capabilities and explicit identity rules. Test cold launch,
reuse, hide, minimize/restore, cross-app switching, close/reopen, timeout, and restart
recovery. Never make a generic integration claim from one successful launch.

## Required completion report

Report changed behavior/files, checks actually run, unverified cases, and delivery
status. Cite source/revision and measured boundaries when asserting architecture,
performance, publish, restart, or push success. A proposed test list is not a passing
test suite. If blocked, state the concrete missing access/information and finish the
independent authorized work.

## Avoid

- Provider branches and pairwise hide calls in the pet/UI.
- Discovery hidden inside cheap-looking properties.
- New polling timers per unused app.
- Shared runtime state persisted as HWNDs/PIDs.
- Treating older logs or chat claims as proof of today's runtime.
- Speculative plugin frameworks or large all-at-once rewrites.
