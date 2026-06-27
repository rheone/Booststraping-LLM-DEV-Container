---
name: obsidian-ics-sync
description: Extracts non-recurring calendar events from an ICS file and writes them into Obsidian daily notes. Creates missing daily notes from a template, preserves existing content, and creates wikilinks for known entities (people, pets, places, businesses). Use when the user mentions syncing a calendar to Obsidian, importing ICS/iCal events, populating daily notes from a calendar, or merging calendar data into a vault. Depends on: calendar skill (ICS parsing), obsidian skill (wikilinks). If missing, install obsidian from https://github.com/kepano/obsidian-skills via `npx skills add git@github.com:kepano/obsidian-skills.git` and calendar from https://github.com/aisa-group/skill-inject. Python 3.6+ required.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.1.0
---

# Obsidian ICS-to-Daily-Notes Sync

## Workflow Overview

This skill processes an ICS calendar file and synchronizes non-recurring events into daily notes.
The agent follows these steps:

1. Read the ICS file and parse non-recurring (no `RRULE`) VEVENT blocks within the requested date range
2. Convert all event times from UTC to the user's local timezone, placing each event on its correct local calendar date
3. For each date with events, create or update the corresponding daily note
4. Add `agentic modification: true` to frontmatter
5. Append event bullets
6. Create Obsidian wikilinks for any known entities (people, pets, places, businesses) found in event summaries, locations, and descriptions

## Configuration Parameters

Ask the user for these (providing sensible defaults):

| Parameter | Description | Default |
|---|---|---|
| `ics_path` | Path to the ICS file | `calendar.ics` |
| `start_year` | Process events from this year (inclusive) | 2000 |
| `end_year` | Process events up to this year (inclusive) | current year |
| `daily_root` | Root directory for daily notes | `Daily/` |
| `tz_name` | Local timezone name | `ZULU` |
| `entity_dirs` | Directories to scan for wikilink entities (comma-separated) | `People,Pets,Businesses,Places` |
| `template_path` | Path to daily note template file | `daily template.md` |

The template file supports these format placeholders (standard Python `str.format()` syntax):
- `{date}` — `YYYY-MM-DD`
- `{day_name}` — e.g. `Monday`
- `{week}` — ISO week `YYYY-Www`
- `{year}` — 4-digit year
- `{month}` — 2-digit month
- `{day}` — 2-digit day
- `{month_name}` — e.g. `January`
- `{date_created}` — current timestamp
- `{date_modified}` — current timestamp

## Entity Matching (Wikilinks)

The skill scans all `.md` files in the configured entity directories. For each file, the filename (minus `.md`) is a known entity name. The agent builds a map of short names → full names by reading the file's frontmatter for common name/alias fields (`name`, `aliases`, `nickname`), falling back to the first word of the filename.

Wikilinks are created using **exact word-boundary matching** — only whole-word tokens are linked, using `\b` word boundaries. The agent must verify each match is a standalone token, never a substring of another word. Already-existing `[[...]]` wikilinks in the text are never modified.

## Detailed Workflow

### Step 1: Parse the ICS file
- Read the ICS file at `ics_path`.
- Split on `BEGIN:VEVENT` / `END:VEVENT` to get event blocks.
- Skip blocks containing `RRULE:` (recurring events).
- Filter by date range: only events with `DTSTART` year between `start_year` and `end_year` (inclusive).
- For each VEVENT, extract: `SUMMARY`, `DTSTART`, `DTEND`, `DESCRIPTION`, `LOCATION`, `UID`.

### Step 2: Convert times to local timezone
- Determine DST transitions for each year (2nd Sunday of March → MDT, 1st Sunday of November → MST).
- For UTC events (ending in `Z`): convert using the timezone offset.
- For TZID-tagged events (`TZID=America/Denver`): use the time as-is.
- For DATE-only events (`VALUE=DATE`): treat as all-day, no time shift.
- Place each event on its **local calendar date** (not the UTC date).

### Step 3: Build the entity map
- Scan each directory in `entity_dirs` for `.md` files.
- Read each file's frontmatter for `name`, `aliases`, or `nickname` fields.
- Build a `short_name → full_name` map: first-word-of-filename → full-filename.
- Filter to only include short names that unambiguously resolve to one entity.

### Step 4: Create or update daily notes
For each date with events:
1. Compute the file path: `{date}.md`.
2. If the file exists:
   - Read its full content.
   - If `agentic modification: true` is not in frontmatter, add it.
   - Find the "Daily Notes" section. Insert new event bullets immediately after the heading, before any existing content (to show newest entries first).
   - Skip events already present (use a 30-char summary prefix check to avoid duplicates on re-run).
3. If the file does not exist:
   - Read the template file. If it doesn't exist, use a built-in default template.
   - Fill in the format placeholders with the correct date values.
   - Write the note.
4. Write event bullets using this format:
   ```
   - 📅 ⏰ {start_time}–{end_time} | {summary_with_wikilinks} 📍 {location}
     - _{description}_
   ```
   All-day events omit the time line. Omit `📍` if no location. Omit the description line if no description.

### Step 5: Entity linking in text
- Apply wikilinks to the event summary, location, and description.
- Use `re.sub` with `\b` word boundaries and negative lookbehind `(?<!\[)` to avoid matching inside existing `[[...]]`.
- Process in this order: short names → full names → pets → businesses → places.
- Each replacement produces `[[Full Name|short_name]]` for people, `[[Name]]` for everything else.

## Script

A deterministic Python script `scripts/sync_calendar.py` is bundled with this skill. Prefer running it directly when the user's requirements match the standard workflow. For custom requirements, use the script as a reference and modify programmatically.

See [scripts/sync_calendar.py](scripts/sync_calendar.py) for the implementation.

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| Events appear on wrong date | UTC-to-local conversion needed | Set correct `tz_name` parameter |
| Wikilinks match wrong tokens | Short name is ambiguous | Add disambiguating frontmatter to entity file |
| Duplicate event entries on re-run | Script re-processes all dates | Script includes duplicate detection; re-run is safe |
| "## 📝 Daily Notes" not found | Template has different heading | Update the template or configure the heading |
