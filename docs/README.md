# Capies documentation

This is the entry point for developing Capies consistently.

Documentation baseline: 2026-10-29 (Asia/Singapore), runtime source commit
`8d98105`. The architecture below is an accepted direction with implementation
pending. This documentation change does not implement the refactor.

## Read in this order

| Document | Purpose |
| --- | --- |
| [Current state](current-state.md) | What source actually implements; known gaps and evidence limits |
| [Architecture](architecture.md) | Target responsibilities, dependencies, state, and lifecycle |
| [Engineering patterns](engineering-patterns.md) | Concrete rules for implementation and extension |
| [Validation](validation.md) | Existing commands, required future checks, and evidence reporting |
| [Gate 1 characterization](characterization-2026-10-29.md) | Local test results and outstanding manual Windows scenarios |
| [Roadmap](roadmap.md) | Incremental migration with completion criteria |
| [ADR 0001](decisions/0001-managed-app-architecture.md) | Why the managed-app architecture was chosen |
| [Agent instructions](../AGENTS.md) | Repository rules for coding assistants |
| [Development skill](../.agents/skills/capies-development/SKILL.md) | Repeatable workflow for changes |
| [PR template](../.github/pull_request_template.md) | Review and verification checklist |

## Status vocabulary

- **Implemented:** located in current source. Behavior verification is reported
  separately with its scope, command/scenario, result, and revision.
- **Proposed:** intended design, API concept, or future test; not a current feature.
- **Unknown / unverified:** insufficient source or runtime evidence.
- **Historical evidence:** a dated result; not a guarantee for later revisions.

Current source wins when determining what exists. The architecture guides new work.
Resolve discrepancies explicitly; neither an old chat nor this document substitutes
for reading the code.

## Keeping these documents useful

Keep current-state factual, architecture normative, and roadmap status evidence-based.
Link to source files and name symbols; line numbers change too easily. When files
move, update their links in the same change. Record important tradeoffs in a new
ADR linked to the previous decision. Avoid duplicating business rules across docs.

A feature is not complete because its checkbox was updated. Completion requires the
source and checks named by the corresponding roadmap gate.

These documents reduce unsupported assumptions; they do not automatically enforce
project boundaries or prove runtime behavior. Automated architecture checks become
possible once the planned projects and test suites actually exist.

## Software library increment

See [software library implementation and evidence](software-library.md). The software manager, icon toolbar, tray collapse, catalog, and serialized coordinator now exist. The complete architecture migration and remaining acceptance gates are still partial.
