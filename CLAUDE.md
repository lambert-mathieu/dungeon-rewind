# CLAUDE.md

## Unity Coding Standards

Always use modern Unity approaches when writing or editing C# scripts in this project:

- Use the new Input System (`UnityEngine.InputSystem`) instead of the legacy `Input` class.
- Prefer `CompareTag()` over `==` for tag checks.
- Cache component references (`GetComponent`) instead of calling them repeatedly, e.g. in `Update`.
- Use `TryGetComponent` instead of `GetComponent` + null check.
- Avoid `FindObjectOfType`/`Find*` at runtime; prefer serialized references, dependency injection, or singletons/events.
- Use `[SerializeField] private` fields instead of public fields for Inspector-exposed values.
- Use `async`/`await` or coroutines appropriately; avoid blocking calls.
- Prefer the Universal Render Pipeline (URP) conventions if the project uses URP.
- Use `UnityEvent`/C# events over polling where reasonable.
- Target current, non-obsolete Unity APIs - check for `[Obsolete]` attributes and avoid deprecated methods (e.g. `UnityEngine.Networking.UnityWebRequest` over `WWW`).
- Follow C# nullable-aware, modern syntax (pattern matching, expression-bodied members) where it improves clarity.

## General Coding Guidelines

- Use a consistent coding style across the entire codebase.
- Avoid breaking up long lines just to shorten them; prefer splitting logic into multiple well-named lines/statements for readability, or just keep the long line if that's clearer.
- Use correct naming conventions (e.g. `isButtonPressed` instead of `buttonPressed` for booleans).
- Do not write any comments in the code, ever. Prefer clear, well-named, self-explanatory code instead.
- Value implementing features the correct way: clean code, scalable, well-structured, with minimal code duplication.
- Use LF line endings (not CRLF), and 4 spaces for indentation where possible.
- Use Kernighan & Ritchie (K&R) style: opening brace on the same line.
- Always ask the user when unsure about a decision.
- Unless explicitly asked by the user, never run git commands that can discard or commit changes, such as `commit`, `checkout`, `restore`, `reset`, `stash`, `revert`, etc.
