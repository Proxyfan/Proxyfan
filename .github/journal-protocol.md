# Journal protocol

Append-only epistemic memory for Proxyfan coding agents. The journal lives at
`JOURNAL.md` in the repository root and exists so that **non-obvious facts**
discovered during one session survive into the next without polluting
`docs/`, `docs/BACKLOG.md`, or the path-scoped instruction files.

> The journal is for things future agents would have to re-discover, not for a
> rolling progress log. If a fact belongs in `docs/ARCHITECTURE.md`,
> `docs/DESIGN.md`, or an ADR, put it there instead and skip the journal entry.

## When to write an entry

Write exactly one entry per Copilot session that did real work — at the end,
before handing back to the human. Skip the entry if the session only ran
read-only investigations and learned nothing new.

Trigger phrases the user may say: *"journal it"*, *"add a journal entry"*,
*"record this"*. Treat any of these as an explicit instruction to append.

## Entry format

Every entry has a single H3 header followed by exactly three bullets. Use the
literal placeholder `(none)` if a bullet would otherwise be empty.

```markdown
### YYYY-MM-DD HH:MM — [tag1,tag2] <short session title>
- **Learned:** <one concrete non-obvious fact, API quirk, or confirmed assumption>
- **Unclear:** <one open question or hypothesis worth testing next>
- **Harder:** <one source of friction, dead end, or regression you hit>
```

Tags are lowercase, hyphenated, comma-separated, immediately after the dash.
Every entry MUST carry at least one tag. **Reuse existing tags wherever
possible** — invent new ones only when nothing fits.

### Suggested tag vocabulary

These map to the Proxyfan module / system map. They are not exhaustive.

- **Bounded contexts:** `proxy`, `traffic`, `rules`, `scripting`,
  `certificates`, `session`, `configuration`.
- **Framework layer:** `networking`, `serialization`, `platform`.
- **Presentation layer:** `presentation`, `mvvm`, `avalonia`, `shell`,
  `inspector`, `tools-windows`.
- **Clients:** `cli`, `desktop`, `msix`.
- **Cross-cutting:** `analyzers`, `build`, `tests`, `e2e`, `ui-automation`,
  `ci`, `installer`, `release`, `performance`, `security`, `privacy`,
  `localization`, `docs`, `tooling`, `meta`.

`meta` is reserved for entries about the journal/protocol itself.

### Enumerate active tags

Before inventing a new tag, list the ones that already exist:

```powershell
(Select-String -Path JOURNAL.md -Pattern '\[[^\]]+\]' -AllMatches).Matches.Value |
    Sort-Object -Unique
```

## Reading: filter, never read end-to-end

The journal grows unboundedly. Loading it end-to-end into the context window
wastes turns and defeats the purpose of the area-tagged design.

1. **At session start**, surface only the tail in one go:

   ```powershell
   Get-Content JOURNAL.md -Tail 16
   ```

2. **Once the task scope is clear**, pull only entries that match the relevant
   tags (each entry is a header plus three bullets, so `-Context 0,3` captures
   it exactly):

   ```powershell
   Select-String -Path JOURNAL.md `
                 -Pattern '^### .*\[[^\]]*\b(proxy|networking)\b' `
                 -Context 0,3
   ```

3. **If a tag has no matches, state that explicitly** ("No prior `proxy`
   entries in JOURNAL.md") instead of falling back to unrelated entries.

## Bullet rules

- One sentence per bullet. Concrete, non-obvious, and falsifiable.
- No filler ("we made progress", "things went well", "this was hard").
- Cite the file and the analyzer/diagnostic ID where applicable
  (e.g. `IDE0370` surfaces in `src/Domain.Proxy/...`).
- Never paste request/response bodies, headers, secrets, credentials, file
  paths under `%LOCALAPPDATA%`, or anything that touches the user's traffic
  capture. The journal is committed to git.

## Size discipline

`.tools/Invoke-MarkdownGate.ps1` enforces a hard size cap on `JOURNAL.md`
(800 lines / 40 000 characters in the `journal` category). When the cap is
about to bite, the resolution is to **archive** — never edit — old entries:

1. Move the oldest entries to `docs/journal/YYYY.md` (one archive file per
   calendar year), preserving their text verbatim and chronological order.
2. Note the archive in a fresh journal entry tagged `[meta]`.

## Immutability

Never edit or delete past entries. Past entries record what the author
*believed at the time*, and rewriting them rewrites history. Only the
`# Project Journal` heading and this protocol document may be edited; even
typo fixes in a past entry are out of policy.
