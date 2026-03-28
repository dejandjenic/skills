---
name: dejan-idea-to-implementation-spec
description: "Use when you have a feature idea but key details are missing. Turn ambiguous ideas into an implementation-ready spec with assumptions, architecture impact, risks, acceptance criteria, and phased tasks. Trigger phrases: scope feature, refine idea, write spec, discovery, fill missing details, implementation plan."
argument-hint: "Describe the feature idea, business goal, and known constraints"
user-invocable: false
disable-model-invocation: false
---

# Idea To Implementation Spec

Convert an early feature concept into a spec package a developer or coding agent can execute.

## Use When
- The user has a valid feature idea, but requirements are incomplete.
- Technical constraints and architecture impact are unclear.
- There is no implementation-ready scope, acceptance criteria, or rollout plan.

## Do Not Use
- Pure brainstorming where no delivery artifact is needed.
- Tiny one-file fixes where discovery overhead would be wasteful.

## Inputs
- Feature idea and expected user or business outcome
- Any known constraints: timeline, compliance, platform, dependencies
- Existing system context if available: repo modules, services, data stores

## Workflow
1. Restate the feature in one sentence and define success outcome.
2. Build a "known vs unknown" map.
3. Resolve unknowns using assumptions with confidence labels.
4. Propose at least two architecture options when uncertainty is high.
5. Select a recommended approach and explain trade-offs.
6. Produce an implementation-ready spec package using templates.
7. End with a short "start here" task list for execution.

## Required Output Sections
1. Problem and Success Definition
2. Scope
3. Non-Goals
4. Assumptions and Open Questions
5. Architecture and Design
6. Data and API Changes
7. Edge Cases and Failure Modes
8. Security and Privacy Considerations
9. Observability and Operations
10. Delivery Plan and Milestones
11. Acceptance Criteria
12. Risks and Mitigations
13. Execution Starter Tasks

## Rules For Missing Information
- Never block on unknowns.
- Mark assumptions explicitly as Assumption A1, A2, and so on.
- Assign confidence: High, Medium, Low.
- For low-confidence assumptions, add a validation task in milestone 0.
- Keep unknowns visible in an Open Questions table.

## Output Format
Use this sequence:
1. A one-paragraph executive summary.
2. A complete spec from [implementation spec template](./assets/implementation-spec-template.md).
3. A closed list of open questions from [open questions template](./assets/open-questions-template.md).
4. An actionable execution checklist from [execution checklist template](./assets/execution-checklist-template.md).

## Quality Bar
- Concrete and falsifiable acceptance criteria.
- Scope boundaries that prevent accidental overbuild.
- Architecture decisions include explicit trade-offs.
- Tasks are granular enough for immediate implementation.
- No section left as vague placeholders.

## Optional References
- [architecture decision mini guide](./references/architecture-decision-guide.md)
