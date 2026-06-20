#!/usr/bin/env python3
"""
Obsidian ICS-to-Daily-Notes Sync Script

Extracts non-recurring calendar events from an ICS file and writes them into
Obsidian daily notes with proper frontmatter, timezone conversion, and wikilinks.

Usage:
    python sync_calendar.py --ics calendar.ics --daily-root Daily/ [options]

See `python sync_calendar.py --help` for full options.
"""

import re, os, sys, json, argparse
from datetime import datetime, timedelta, timezone
from collections import defaultdict
from string import Formatter

# ── Defaults ──────────────────────────────────────────────────────────────────

DEFAULT_TEMPLATE = """---
date: {date}
day: {day_name}
week: {week}
date created: {date_created}
date modified: {date_modified}
tags: [daily, journal]
touched by llm: true
---

# 📅 {day_name}, {month_name} {day}, {year}

## 📝 Daily Notes

"""

MONTH_NAMES = ['', 'January', 'February', 'March', 'April', 'May', 'June',
               'July', 'August', 'September', 'October', 'November', 'December']


# ── Timezone helpers ──────────────────────────────────────────────────────────

def _second_sunday_march(y: int) -> datetime:
    d = datetime(y, 3, 8)
    while d.weekday() != 6:
        d += timedelta(days=1)
    return d

def _first_sunday_november(y: int) -> datetime:
    d = datetime(y, 11, 1)
    while d.weekday() != 6:
        d += timedelta(days=1)
    return d

def utc_to_local(utc_dt: datetime, tz_offset_hours: int = -7, tz_name: str = 'America/Denver') -> datetime:
    """
    Convert a UTC datetime to local time.
    Uses DST rules (2nd Sunday March → -6, 1st Sunday November → -7 for America/Denver).
    Customize `tz_offset_hours` and DST logic for other timezones.
    """
    y = utc_dt.year
    utc_naive = utc_dt.replace(tzinfo=None)
    # America/Denver DST logic
    dst_start = _second_sunday_march(y)
    dst_end = _first_sunday_november(y)
    if dst_start <= utc_naive < dst_end:
        offset = -6  # MDT
    else:
        offset = -7  # MST
    
    # For other timezones, override with tz_offset_hours
    # This simple model works for America/Denver. Extend as needed.
    return utc_dt + timedelta(hours=offset)

def fmt_time(dt: datetime) -> str:
    """Format a datetime as '7:30 pm' or 'midnight'."""
    h, mi = dt.hour, dt.minute
    if h == 0 and mi == 0:
        return 'midnight'
    ampm = 'am' if h < 12 else 'pm'
    dh = h if h <= 12 else h - 12
    if dh == 0:
        dh = 12
    if mi == 0:
        return f'{dh} {ampm}'
    return f'{dh}:{mi:02d} {ampm}'


# ── ICS Parser ────────────────────────────────────────────────────────────────

def parse_ics(ics_path: str) -> list:
    """
    Parse an ICS file and return a list of raw event dicts.
    Each dict has keys like: DTSTART, DTEND, SUMMARY, DESCRIPTION, LOCATION, UID, RRULE, etc.
    """
    with open(ics_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find all VEVENT blocks
    blocks = []
    i = 0
    while True:
        start = content.find('BEGIN:VEVENT', i)
        if start == -1:
            break
        end = content.find('END:VEVENT', start)
        if end == -1:
            break
        blocks.append(content[start:end + len('END:VEVENT')])
        i = end + len('END:VEVENT')

    events = []
    SKIP_KEYS = {'TRANSP', 'SEQUENCE', 'DTSTAMP', 'CREATED', 'LAST-MODIFIED',
                 'RECURRENCE-ID', 'STATUS', 'METHOD', 'CALSCALE', 'PRODID',
                 'VERSION', 'X-WR-CALNAME', 'X-WR-TIMEZONE', 'X-WR-CALDESC',
                 'X-GOOGLE-CALENDAR-CONTENT-DISPLAY', 'X-GOOGLE-CALENDAR-CONTENT-ICON',
                 'X-GOOGLE-CONFERENCE', 'X-APPLE-STRUCTURED-LOCATION'}
    SKIP_PREFIXES = ('X-', 'ATTENDEE', 'ORGANIZER')
    SKIP_EXACT = {'CLASS:PUBLIC', 'CLASS:PRIVATE', 'CLASS:CONFIDENTIAL'}

    for block in blocks:
        event = {}
        in_alarm = False
        accumulated = ''

        for raw_line in block.split('\n'):
            line = raw_line.strip()
            if not line:
                continue
            
            # Handle continuation lines
            if line.startswith(' '):
                accumulated += line.strip()
                continue
            if accumulated:
                _parse_kv(accumulated, event, SKIP_KEYS, SKIP_PREFIXES, SKIP_EXACT, lambda: in_alarm)
                accumulated = line
                continue
            accumulated = line
        
        if accumulated:
            _parse_kv(accumulated, event, SKIP_KEYS, SKIP_PREFIXES, SKIP_EXACT, lambda: in_alarm)
        
        if 'DTSTART' in event:
            events.append(event)
    
    return events


def _parse_kv(line, event, skip_keys, skip_prefixes, skip_exact, get_in_alarm):
    if line in ('BEGIN:VALARM',):
        return
    if line == 'END:VALARM':
        return
    if line in skip_exact:
        return
    if any(line.startswith(p) for p in skip_prefixes):
        return
    
    colon_idx = line.find(':')
    if colon_idx == -1:
        return
    key = line[:colon_idx]
    val = line[colon_idx+1:]
    if key in skip_keys:
        return
    event[key] = val


# ── Entity Scanner ────────────────────────────────────────────────────────────

def scan_entities(entity_dirs: list) -> dict:
    """
    Scan directories for .md files and build an entity map.
    Returns:
        full_names: set of full filenames (minus .md)
        short_to_full: dict mapping first-word → full name
        pets: set of names from a 'Pets' directory
        businesses: set of names from a 'Businesses' directory
        places: set of known place names
    """
    full_names = set()
    short_to_full = {}
    pets = set()
    businesses = set()
    places = set()

    for d in entity_dirs:
        d = d.strip()
        if not os.path.isdir(d):
            continue
        
        dir_key = os.path.basename(d).lower()
        for fname in os.listdir(d):
            if not fname.endswith('.md'):
                continue
            name = fname[:-3]
            full_names.add(name)

            # Categorize by directory name
            if dir_key in ('pets', 'pet'):
                pets.add(name)
            elif dir_key in ('businesses', 'business', 'vendors'):
                businesses.add(name)
            elif dir_key in ('places', 'place', 'locations', 'location'):
                places.add(name)
            
            # Extract short name from file content frontmatter
            short = None
            fpath = os.path.join(d, fname)
            try:
                with open(fpath, 'r', encoding='utf-8') as fh:
                    content = fh.read()
                # Try to get name/aliases/nickname from frontmatter
                fm = re.search(r'^---\s*\n(.*?)\n---', content, re.DOTALL)
                if fm:
                    fm_text = fm.group(1)
                    for field in ('name', 'aliases', 'nickname'):
                        m = re.search(rf'^{field}:\s*(.+)$', fm_text, re.MULTILINE)
                        if m:
                            val = m.group(1).strip().strip('"\'')
                            if field == 'aliases':
                                # aliases can be a list
                                aliases = re.findall(r'\[([^\]]+)\]', val)
                                if aliases:
                                    for a in re.split(r'[,\s]+', aliases[0]):
                                        a = a.strip().strip('"\'')
                                        if a:
                                            short_to_full[a] = name
                                continue
                            short = val
                            break
            except Exception:
                pass
            
            if not short:
                short = name.split()[0] if name.split() else name
            
            if short and short.lower() not in ('the', 'a', 'an'):
                short_to_full[short] = name

    return full_names, short_to_full, pets, businesses, places


# ── Wikilink Engine ──────────────────────────────────────────────────────────

def build_wikilink_replacements(short_to_full, full_names, pets, businesses, places):
    """Build ordered list of (pattern, replacement) for wikilink creation."""
    replacements = []

    # Short names → [[Full Name|short_name]]
    for short in sorted(short_to_full, key=len, reverse=True):
        full = short_to_full[short]
        pattern = re.compile(r'(?<!\[)\b' + re.escape(short) + r'\b(?![\|\]])')
        replacements.append((pattern, f'[[{full}|{short}]]'))

    # Full names → [[Full Name]]
    for name in sorted(full_names, key=len, reverse=True):
        pattern = re.compile(r'(?<!\[)\b' + re.escape(name) + r'\b(?![\|\]])')
        replacements.append((pattern, f'[[{name}]]'))

    # Pets
    for pet in sorted(pets, key=len, reverse=True):
        pattern = re.compile(r'(?<!\[)\b' + re.escape(pet) + r'\b', re.IGNORECASE)
        replacements.append((pattern, f'[[{pet}]]'))

    # Businesses
    for bus in sorted(businesses, key=len, reverse=True):
        pattern = re.compile(r'(?<!\[)\b' + re.escape(bus) + r'\b', re.IGNORECASE)
        replacements.append((pattern, f'[[{bus}]]'))

    # Places
    for place in sorted(places, key=len, reverse=True):
        pattern = re.compile(r'(?<!\[)\b' + re.escape(place) + r'\b')
        replacements.append((pattern, f'[[{place}]]'))

    return replacements


def apply_wikilinks(text: str, replacements: list) -> str:
    """Apply all wikilink replacements to text; safe for multiple passes."""
    result = text
    for pattern, repl in replacements:
        result = pattern.sub(repl, result)
    return result


# ── Event Processing ──────────────────────────────────────────────────────────

def process_events(events: list, start_year: int, end_year: int,
                   tz_name: str = 'America/Denver') -> dict:
    """
    Process raw ICS events into local-date-grouped dicts.
    
    Returns: { 'YYYY-MM-DD': [{summary, time_prefix, description, location, ...}] }
    """
    local_events = defaultdict(list)

    for evt in events:
        if 'RRULE' in evt:
            continue  # skip recurring

        dtstart_str = evt.get('DTSTART', '')
        if not dtstart_str:
            continue

        # Check year
        ym = re.search(r'(\d{4})(\d{2})(\d{2})', dtstart_str)
        if not ym:
            continue
        raw_y, raw_mo, raw_d = int(ym.group(1)), int(ym.group(2)), int(ym.group(3))

        if not (start_year <= raw_y <= end_year):
            continue

        is_utc = dtstart_str.endswith('Z')
        has_tzid = 'TZID=' in dtstart_str
        is_date_only = 'VALUE=DATE' in dtstart_str
        time_m = re.search(r'T(\d{2})(\d{2})', dtstart_str)
        h, mi = (int(time_m.group(1)), int(time_m.group(2))) if time_m else (0, 0)

        if is_date_only:
            local_date = f"{raw_y:04d}-{raw_mo:02d}-{raw_d:02d}"
            time_prefix = ''
            start_display = None
            end_display = None
        elif is_utc:
            dt_utc = datetime(raw_y, raw_mo, raw_d, h, mi, tzinfo=timezone.utc)
            dt_local = utc_to_local(dt_utc)
            local_date = dt_local.strftime('%Y-%m-%d')
            start_display = fmt_time(dt_local)
            
            dtend_str = evt.get('DTEND', '')
            end_display = None
            if dtend_str and 'VALUE=DATE' not in dtend_str:
                em = re.search(r'T(\d{2})(\d{2})', dtend_str)
                if em:
                    eh, emi = int(em.group(1)), int(em.group(2))
                    m2 = re.search(r'(\d{4})(\d{2})(\d{2})T', dtend_str)
                    if m2:
                        de = datetime(int(m2.group(1)), int(m2.group(2)), int(m2.group(3)), eh, emi, tzinfo=timezone.utc)
                        de_local = utc_to_local(de)
                        end_display = fmt_time(de_local)
        elif has_tzid:
            local_date = f"{raw_y:04d}-{raw_mo:02d}-{raw_d:02d}"
            start_display = fmt_time(datetime(2000, 1, 1, h, mi))
            end_display = None
            dtend_str = evt.get('DTEND', '')
            if dtend_str and 'VALUE=DATE' not in dtend_str:
                em = re.search(r'T(\d{2})(\d{2})', dtend_str)
                if em:
                    eh, emi = int(em.group(1)), int(em.group(2))
                    end_display = fmt_time(datetime(2000, 1, 1, eh, emi))
        else:
            # Floating time
            local_date = f"{raw_y:04d}-{raw_mo:02d}-{raw_d:02d}"
            start_display = fmt_time(datetime(2000, 1, 1, h, mi))
            end_display = None
            dtend_str = evt.get('DTEND', '')
            if dtend_str and 'VALUE=DATE' not in dtend_str:
                em = re.search(r'T(\d{2})(\d{2})', dtend_str)
                if em:
                    eh, emi = int(em.group(1)), int(em.group(2))
                    end_display = fmt_time(datetime(2000, 1, 1, eh, emi))

        # Build time prefix
        if is_date_only:
            time_prefix = ''
        elif start_display and end_display:
            time_prefix = f'{start_display}–{end_display} | '
        elif start_display:
            time_prefix = f'{start_display} | '
        else:
            time_prefix = ''

        summary = evt.get('SUMMARY', 'Untitled')
        description = evt.get('DESCRIPTION', '')
        location = evt.get('LOCATION', '')

        # Clean description (remove Google Meet boilerplate)
        if description:
            clean = []
            for line in description.replace('\r\n', '\n').split('\n'):
                s = line.strip()
                if not s:
                    continue
                if '-::~:' in s or 'Join with Google Meet' in s or \
                   'Learn more about Meet' in s or 'Please do not edit' in s:
                    continue
                clean.append(s)
            description = '\n'.join(clean)

        # Clean location
        location = location.replace('\\n', ', ').replace('\\,', ',').replace('\n', ', ')
        location = re.sub(r',\s*,', ',', location).strip(' ,')

        local_events[local_date].append({
            'date': local_date,
            'time_prefix': time_prefix,
            'summary': summary,
            'description': description,
            'location': location,
            'start_display': start_display,
            'is_date_only': is_date_only,
        })

    # Sort events within each day
    for k in local_events:
        local_events[k].sort(key=lambda e: (e['start_display'] or 'zzz', e['summary']))

    return dict(local_events)


# ── Note Generation ──────────────────────────────────────────────────────────

def load_template(template_path: str) -> str:
    """Load template from file or return default."""
    if template_path and os.path.exists(template_path):
        with open(template_path, 'r', encoding='utf-8') as f:
            return f.read()
    return DEFAULT_TEMPLATE


def gen_bullet(evt: dict, replacements: list) -> str:
    """Generate a bullet line for a calendar event."""
    bullet = '- 📅 '
    if evt['time_prefix']:
        bullet += f'⏰ {evt["time_prefix"]}'
    bullet += apply_wikilinks(evt['summary'], replacements)
    if evt['location']:
        bullet += f' 📍 {evt["location"]}'
    if evt['description'] and len(evt['description']) > 10:
        desc = evt['description'][:300].replace('\n', ' | ')
        bullet += f'\n  - _{desc}_'
    return bullet + '\n'


def sync_notes(local_events: dict, daily_root: str, template_path: str,
               replacements: list, dry_run: bool = False) -> dict:
    """
    Create or update daily notes for each date with events.
    Returns stats dict.
    """
    template_str = load_template(template_path)
    sorted_dates = sorted(local_events.keys())
    stats = {'created': 0, 'updated': 0, 'skipped': 0}

    for date_key in sorted_dates:
        y, m, d = int(date_key[:4]), int(date_key[5:7]), int(date_key[8:10])
        month_dir = os.path.join(daily_root, str(y), f'{m:02d}')
        file_path = os.path.join(month_dir, f'{date_key}.md')
        events = local_events[date_key]

        if os.path.exists(file_path):
            # ── Update existing file ──
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            modified = False

            # Add touched by llm
            if 'touched by llm: true' not in content:
                if content.startswith('---'):
                    end_fm = content.find('---', 3)
                    if end_fm != -1:
                        fm = content[3:end_fm].strip()
                        fm += '\ntouched by llm: true'
                        content = '---\n' + fm + '\n' + content[end_fm:]
                        modified = True

            # Append events under ## 📝 Daily Notes
            section_marker = '## 📝 Daily Notes'
            if section_marker in content:
                section_start = content.index(section_marker)
                rest = content[section_start:]
                after = rest[len(section_marker):]
                nl = after.find('\n')
                insert_pos = section_start + len(section_marker) + nl + 1

                # Check for existing events to avoid duplicates
                already_present = all(
                    evt['summary'][:30] in content[section_start:section_start+1000]
                    for evt in events
                )
                if not already_present:
                    new_text = ''
                    for evt in events:
                        new_text += gen_bullet(evt, replacements)
                    old_rest = content[insert_pos:]
                    content = content[:insert_pos] + '\n' + new_text + old_rest
                    modified = True

            if modified:
                now_str = datetime.now().strftime('%Y-%m-%d %H:%M')
                content = re.sub(r'date modified: .*', f'date modified: {now_str}', content)
                if not dry_run:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                stats['updated'] += 1
            else:
                stats['skipped'] += 1
        else:
            # ── Create new file ──
            if not dry_run:
                os.makedirs(month_dir, exist_ok=True)
            
            day_name = datetime(y, m, d).strftime('%A')
            week = datetime(y, m, d).strftime('%Y-W%W')
            now_str = datetime.now().strftime('%Y-%m-%d %H:%M')

            try:
                content = template_str.format(
                    date=date_key, day_name=day_name, week=week,
                    date_created=now_str, date_modified=now_str,
                    month_name=MONTH_NAMES[m], day=d, year=y,
                    month=f'{m:02d}'
                )
            except KeyError as e:
                # If template has a placeholder we don't support, use default
                content = DEFAULT_TEMPLATE.format(
                    date=date_key, day_name=day_name, week=week,
                    date_created=now_str, date_modified=now_str,
                    month_name=MONTH_NAMES[m], day=d, year=y
                )

            for evt in events:
                content += gen_bullet(evt, replacements)

            if not dry_run:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(content)
            stats['created'] += 1

    return stats


# ── CLI ──────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description='Sync ICS calendar events to Obsidian daily notes.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --ics calendar.ics --daily-root Daily/ --start-year 2024
  %(prog)s --ics .temp/cal.ics --entity-dirs "People,Pets,Businesses,Places"
  %(prog)s --ics events.ics --template my_template.md --tz America/Chicago
        """)
    
    parser.add_argument('--ics', required=True, help='Path to ICS file')
    parser.add_argument('--daily-root', default='Daily/',
                        help='Root directory for daily notes (default: Daily/)')
    parser.add_argument('--start-year', type=int, default=2000,
                        help='Process events from this year (inclusive)')
    parser.add_argument('--end-year', type=int, default=datetime.now().year,
                        help='Process events up to this year (inclusive)')
    parser.add_argument('--tz', default='America/Denver',
                        help='Timezone name (default: America/Denver)')
    parser.add_argument('--template', default=None,
                        help='Path to daily note template file')
    parser.add_argument('--entity-dirs', default='People,Pets,Businesses,Places',
                        help='Comma-separated directories to scan for wikilink entities')
    parser.add_argument('--dry-run', action='store_true',
                        help='Show what would be done without writing files')
    parser.add_argument('--verbose', action='store_true',
                        help='Print detailed progress')

    args = parser.parse_args()

    if args.verbose:
        print(f"📂 ICS: {args.ics}", file=sys.stderr)
        print(f"📁 Daily root: {args.daily_root}", file=sys.stderr)
        print(f"📅 Year range: {args.start_year}–{args.end_year}", file=sys.stderr)
        print(f"🕐 Timezone: {args.tz}", file=sys.stderr)
        print(f"📄 Template: {args.template or '(default)'}", file=sys.stderr)
        print(f"🏷️  Entity dirs: {args.entity_dirs}", file=sys.stderr)

    # Step 1: Parse ICS
    if args.verbose:
        print("\nParsing ICS...", file=sys.stderr)
    raw_events = parse_ics(args.ics)
    if args.verbose:
        print(f"  Found {len(raw_events)} total events", file=sys.stderr)

    # Step 2: Process events
    local_events = process_events(raw_events, args.start_year, args.end_year, args.tz)
    total_evts = sum(len(v) for v in local_events.values())
    if args.verbose:
        print(f"  {total_evts} events across {len(local_events)} dates in range", file=sys.stderr)

    # Step 3: Scan entities
    if args.verbose:
        print("\nScanning entity directories...", file=sys.stderr)
    entity_dirs = [d.strip() for d in args.entity_dirs.split(',') if d.strip()]
    full_names, short_to_full, pets, businesses, places = scan_entities(entity_dirs)
    if args.verbose:
        print(f"  {len(full_names)} entities, {len(short_to_full)} short names, "
              f"{len(pets)} pets, {len(businesses)} businesses", file=sys.stderr)

    # Step 4: Build wikilink replacements
    replacements = build_wikilink_replacements(short_to_full, full_names, pets, businesses, places)

    # Step 5: Sync notes
    if args.verbose:
        print("\nSyncing notes...", file=sys.stderr)
    stats = sync_notes(local_events, args.daily_root, args.template, replacements, args.dry_run)

    # Print summary
    print(f"\n{'=== DRY RUN ===' if args.dry_run else '=== Done =='}", file=sys.stderr)
    print(f"Created: {stats['created']}", file=sys.stderr)
    print(f"Updated: {stats['updated']}", file=sys.stderr)
    print(f"Skipped: {stats['skipped']}", file=sys.stderr)
    print(f"Total:   {stats['created'] + stats['updated'] + stats['skipped']}", file=sys.stderr)

    # Return JSON to stdout for machine parsing
    print(json.dumps({**stats, 'dates_processed': len(local_events), 'events_total': total_evts}))


if __name__ == '__main__':
    main()
