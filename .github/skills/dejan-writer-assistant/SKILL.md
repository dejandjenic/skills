---
name: dejan-writer-assistant
description: "Use when writing or updating book chapters in a repository that contains multiple books. Handles chapter creation from user guidance, language detection, and ongoing character/trivia maintenance. Trigger phrases: write chapter, draft chapter, continue book, update character sheet."
argument-hint: "Provide book directory, chapter number, chapter title, and chapter guidance."
user-invocable: false
disable-model-invocation: false
---

# Writer Assistant

Workflow for writing fiction or nonfiction chapters in a repository that stores multiple books.

## Use When
- The repository contains multiple books, each in its own directory.
- The user asks to write or revise a chapter.
- Character consistency must be maintained across chapters.

## Do Not Use
- Pure coding tasks unrelated to writing.
- Requests that do not specify any chapter objective.

## Inputs
- Book directory path (required)
- Chapter number (required)
- Chapter title (required)
- Chapter guidance from user: plot beats, themes, tone, target length, constraints (required)
- Optional style references from earlier chapters

## Repository Conventions
- Treat each book as a separate project rooted in its own directory.
- Store chapters under that book directory using a consistent naming scheme, for example `chapter-01-the-beginning.md`.
- Maintain one or more character tracking documents per book, for example:
  - `CHARACTERS.md`
  - `CHARACTER_TRIVIA.md`
  - `LORE.md`
- If these tracking files do not exist, create them before or immediately after writing the first chapter.

## Workflow
1. Validate that the target book directory exists.
2. Determine book language:
   - Infer from existing chapter content, book metadata, or notes when confidence is high.
   - If language cannot be inferred with high confidence, ask the user to choose language before drafting.
3. Parse chapter request and confirm required fields: number, title, and guidance.
4. Draft or update the chapter in the selected book directory using the requested guidance.
5. Ensure chapter heading includes both chapter number and title.
6. Update character tracking documents after chapter changes:
   - Add newly introduced characters.
   - Update known attributes: personality, likes, dislikes, birthdays, relationships, motivations, and notable trivia.
   - Record chapter references for each important character change.
7. If any character detail is unclear, conflicting, or ambiguous, stop and ask the user for clarification before finalizing those entries.
8. Return a summary of chapter work plus all character/trivia updates.

## Output Format
1. Book and chapter targeted
2. Language used and how it was determined
3. Chapter changes made
4. Character and trivia updates
5. Clarifications needed (if any)
6. Suggested next chapter actions

## Critical Rules
1. Never mix content between books. Operate only inside the selected book directory.
2. Never write a chapter without chapter number, chapter title, and user guidance.
3. If book language is uncertain, ask the user before writing.
4. Character tracking maintenance is mandatory after every chapter write or rewrite.
5. If character information is unclear, ask for clarification instead of guessing.
6. Preserve continuity with prior chapters unless user explicitly requests a retcon.

## Quality Bar
- Chapter aligns with user guidance and established story context.
- Voice and language remain consistent with the target book.
- Character data stays current and internally consistent.
- Ambiguities are surfaced clearly to the user with direct follow-up questions.
