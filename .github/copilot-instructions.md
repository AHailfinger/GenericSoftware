# GitHub Copilot Instructions for EnergyAutomate

## Session Rules
- All comments should be in English
- All documentations should be in English
- All direct discussions should be in English
- All code should be included trace logs
- All code responses must always include the full, compilable code for the relevant class, method, or file
- Never use placeholders like "rest of the code", "...", or similar abbreviations
- Never omit parts of the code for brevity. Always provide the complete implementation, so the code can be copied and used directly
- If a method or class is requested, always output the entire method or class, not just a fragment

## CRITICAL: No Unsolicited Additions or Removals
- **NEVER add services to Program.cs without explicit permission**
- **NEVER create new files without explicit permission**
- **NEVER delete files without explicit permission**
- **NEVER modify unrelated code without explicit permission**
- **NEVER suggest "improvements" that weren't requested**
- Only implement exactly what is requested, nothing more

## Workflow: Step-by-Step with Token Awareness
**IMPORTANT: Before implementing ANY change, ALWAYS:**
1. Analyze the task and estimate token cost
2. **Show the user the implementation plan** with estimated token usage
3. **Ask for permission** before proceeding
4. Only after approval: execute the changes
5. Report what was done and final token cost

**Token Budget Awareness:**
- Small edits (< 2 KB): ~500-2,000 tokens
- Medium changes (2-10 KB): ~2,000-10,000 tokens
- Large refactors (> 10 KB): ~10,000+ tokens
- Include analysis, search, and tool overhead in estimates

**When proposing changes:**
```
I need to change the following:
1. [File A] - [Description]
2. [File B] - [Description]
Estimated token usage: ~X,000 tokens
Should I continue? (yes/no)
```

## General Principles

### Research First, Code Never
- Check existing usage in the project FIRST
- Check official documentation/demos before writing code
- Never invent syntax - search for real examples
- When uncertain: ask or search, don't guess

### Transparency
- Say explicitly if unsure about something
- Don't pretend knowledge
- Admit when you need to verify

## Research Process for Components

For any Blazor/BlazorBootstrap component:
1. Search project for existing usage: `code_search`
2. Check official demos: https://demos.blazorbootstrap.com/
3. Get official docs with `get_web_pages` if needed
4. Extract from code patterns, not memory
5. Don't proceed until 100% sure

## File Organization

### .razor Files
- Import only necessary `@using` statements
- Follow existing import style
- Keep component structure simple
- Use proper spacing and indentation

### .razor.cs Files (Code-Behind)
- Follow existing code conventions in project
- Use existing properties/methods where possible
- Document public methods with comments (explain WHY)
- Follow SOLID principles
- Use dependency injection for services

### Testing
- Test through public APIs only
- Match existing test style (xUnit/NUnit/MSTest)
- One behavior per test
- Arrange-Act-Assert pattern

## When Uncertain

**DO THIS:**
1. Record observation with `record_observation`
2. Search for patterns in project with `code_search`
3. Get official documentation with `get_web_pages`
4. Show findings to user before proceeding
5. Ask for confirmation if needed
6. THEN implement

**DON'T DO THIS:**
- Guess at syntax ❌
- Invent component names ❌
- Use properties from memory ❌
- Skip research to save time ❌
- Assume documentation without verifying ❌

## Repository Instructions
- Repository instructions are located at the root under /.github (for example, .github/copilot-instructions.md).
