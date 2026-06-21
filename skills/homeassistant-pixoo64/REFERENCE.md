# Pixoo 64 Reference

## 1. Services

All services are **entity services** — target the Pixoo sensor entity.

### 1a. `divoom_pixoo.show_message`

Display a page on the Pixoo. Temporarily overrides normal page rotation;
returns to it after `duration` seconds.

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `page_data` | object | **Yes** | -- | Page definition with `page_type` (see SS2) |
| `duration` | number (1-9999) | No | scan_interval from config | Seconds to override normal rotation |

**Target:** `sensor.divoom_pixoo_64_display_current_page`

### 1b. `divoom_pixoo.play_buzzer`

Play the built-in buzzer. **Warning: may damage the device. Use at own risk.**

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `buzz_cycle_time_millis` | number (1-9999) | No | 500 | Active time per cycle (ms) |
| `idle_cycle_time_millis` | number (1-9999) | No | 500 | Idle time per cycle (ms) |
| `total_time` | number (1-9999) | No | 3000 | Total duration (ms) |

### 1c. `divoom_pixoo.restart`

Restart the Pixoo device. Has a brief delay — be patient.

### 1d. `divoom_pixoo.update_page`

Re-render and re-send the currently configured page. **Warning:**
spamming this may crash the device.

---

## 2. Page Types

All pages are defined under `page_data:` in `show_message` or in the
integration's config flow (`pages_data`).

### 2a. `components` / `custom` (Custom Canvas)

Build your own page from text, image, rectangle, and templatable components.
Most flexible page type. Both `"components"` and `"custom"` are accepted.

```yaml
page_type: components
components:
  - type: text
    # ... (see 3a)
  - type: image
    # ... (see 3b)
  - type: rectangle
    # ... (see 3c)
  - type: templatable
    # ... (see 3d)
```

Optional top-level field:

| Field | Type | Notes |
|---|---|---|
| `variables` | object | Define Jinja2 vars shared across components (config only, not in service call) |

### 2b. `clock`

Display a built-in Divoom clock face.

| Field | Required | Notes |
|---|---|---|
| `id` | **Yes** | Clock face ID. Browse via Divoom app or check [CLOCKS list](https://github.com/gickowtf/pixoo-homeassistant/blob/main/READMES/CLOCKS.md). All fields support templates. |

```yaml
page_type: clock
id: 182
```

### 2c. `channel`

Display a Divoom custom channel.

| Field | Required | Notes |
|---|---|---|
| `id` | **Yes** | 0 = Channel 1, 1 = Channel 2, 2 = Channel 3 |

```yaml
page_type: channel
id: 0
```

### 2d. `visualizer`

Music visualizer page. Also accepted as `"visualiser"` (British spelling).

| Field | Required | Notes |
|---|---|---|
| `id` | **Yes** | Visualizer position (0-based, top-left to bottom-right in app) |

```yaml
page_type: visualizer
id: 2
```

### 2e. `gif`

Animated GIF from URL.

| Field | Required | Notes |
|---|---|---|
| `gif_url` | **Yes** | URL ending in `.gif`. GIF must be 64x64 px for best results. Warn if URL does not resolve or if dimensions are unknown. |

```yaml
page_type: gif
gif_url: https://example.com/animation.gif
```

### 2f. `pv` (Solar / Photovoltaic)

Pre-designed solar dashboard page.

| Field | Required | Notes |
|---|---|---|
| `power` | Yes | Current power (W) |
| `storage` | Yes | Battery level (%) |
| `discharge` | Yes | Discharge rate (W) |
| `powerhousetotal` | Yes | House consumption (W) |
| `vomNetz` | Yes | Grid import/export (W) |
| `time` | Yes | Time string (`{{ now().strftime('%H:%M') }}`) |

All fields support templates. Icons change at battery thresholds:
80/60/40/20/0%.

```yaml
page_type: pv
power: "{{ states('sensor.solar_production') }}"
storage: "{{ states('sensor.battery_level') }}"
```

### 2g. `fuel` (Fuel Prices)

Pre-designed fuel price display.

| Field | Required | Notes |
|---|---|---|
| `title` | Yes | Station name |
| `name1` | Yes | Fuel type 1 (e.g. Diesel) |
| `price1` | Yes | Price 1 |
| `name2` | Yes | Fuel type 2 |
| `price2` | Yes | Price 2 |
| `name3` | Yes | Fuel type 3 |
| `price3` | Yes | Price 3 |
| `status` | Yes | Extra info (e.g. open/closed) |
| `font_color` | No | white |
| `bg_color` | No | [255, 230, 0] (yellow) |
| `price_color` | No | white |
| `title_color` | No | black |
| `stripe_color` | No | font_color |
| `title_offset` | No | 2 (centering offset) |

All fields except colors support templates.

### 2h. `progress_bar`

Pre-designed progress bar with percentage and time.

| Field | Required | Default | Notes |
|---|---|---|---|
| `header` | Yes | -- | Top label |
| `progress` | Yes | -- | 0-100 integer |
| `footer` | Yes | -- | Bottom label |
| `bg_color` | No | blue | Background color |
| `header_offset` | No | 2 | Header centering offset |
| `header_font_color` | No | white | |
| `progress_bar_color` | No | red | Fill color |
| `progress_text_color` | No | white | Percentage text color |
| `time_color` | No | gray | |
| `time_end` | No | "" | End time string (uses CLOCK font, digits `[0-9:]` only) |
| `time_end_color` | No | light_grey | End time color |
| `footer_offset` | No | 2 | Footer centering offset |
| `footer_font_color` | No | white | |

```yaml
page_type: progress_bar
header: "PROCESS"
progress: "{{ states('sensor.progress_sensor') }}"
footer: "{{ now().strftime('%H:%M') }}"
```

---

## 3. Component Types (for `components` page)

### 3a. Text

| Field | Required | Default | Notes |
|---|---|---|---|
| `content` | **Yes** | -- | Text (templatable). Rendered **UPPERCASE**. Use `\n` for line break. |
| `position` | **Yes** | -- | `[x, y]` — center text at x=32, y=Y |
| `font` | No | `pico_8` | See FONTS below |
| `color` | No | `white` | `"white"` (preset) or `[R, G, B]` array |
| `align` | No | `left` | `left`, `center`, `right` |

**Warn** if content length exceeds font's per-line capacity.
**Warn** if color matches the background/border beneath it.

### 3b. Image

| Field | Required | Notes |
|---|---|---|
| `position` | **Yes** | `[x, y]` — center at [0, 0] |
| `image_path` | One of 3 | Local file path (e.g. `/config/www/img/icon.png`) |
| `image_url` | One of 3 | Remote URL |
| `image_data` | One of 3 | Base64-encoded image data |
| `height` | No | If set, longest side = this value (proportional) |
| `width` | No | If set, longest side = this value (proportional) |
| `resample_mode` | No | `box` — also `nearest`, `bilinear`, `hamming`, `bicubic`, `lanczos` |

**Warn** if image dimensions exceed 64 px in both width and height
after scaling (it will be auto-scaled but may lose detail).

### 3c. Rectangle

| Field | Required | Default | Notes |
|---|---|---|---|
| `position` | **Yes** | -- | `[x, y]` top-left corner |
| `size` | **Yes** | -- | `[width, height]` |
| `color` | No | white | Preset or `[R, G, B]` |
| `filled` | No | false | Fill or outline |

### 3d. Templatable

| Field | Required | Notes |
|---|---|---|
| `template` | **Yes** | Jinja2 template that returns a list of component dicts |

Renders a template whose output must be a JSON array of component
definitions. These are inserted in place of the templatable entry.

---

## 4. FONTS

All text is rendered **UPPERCASE**. Unknown characters show as `?`.

| Font | Approx. chars per line (64 px) | Best for |
|---|---|---|
| `pico_8` | ~12 | Body text, sensor values |
| `gicko` | ~8 | Short labels, headers |
| `five_pix` | ~16 | Dense data, small status |
| `eleven_pix` | ~7 | Large headlines, emphasis |
| `clock` | ~6 | Numeric time only (digits `[0-9:]`) |
| `pix24` | ~4 | Very large numbers |

To wrap long text: split content across multiple text components at
different Y positions, inserting `\n` every N chars.

---

## 5. COLORS

Named presets available (CSS4 color names). Replace any `[R, G, B]`
with a preset string:

`white`, `black`, `red`, `green`, `blue`, `yellow`, `gold`,
`goldenrod`, `darkgoldenrod`, `orange`, `purple`, `pink`, `gray`, `silver`, `navy`, `teal`, `aqua`, `lime`, `olive`,
`maroon`, `fuchsia`, `cyan`, `magenta`, `violet`, `indigo`,
`coral`, `crimson`, `salmon`, `tomato`, `turquoise`, `plum`

Or use RGB array: `[0, 200, 255]`

---

## 6. XY POSITIONING

Canvas: `X = 0..63` (left to right), `Y = 0..63` (top to bottom).

```
(0,0)                                       (63,0)
  +-------------------------------------------+
  |                                             |
  |      32,0  <-- horizontal center            |
  |                                             |
  |           center = [32, Y]                  |
  |                                             |
  |  0,32  <-- vertical center                  |
  |                                             |
  +-------------------------------------------+
(0,63)                                      (63,63)
```

- Centered text: `position: [32, Y]`, `align: center`
- Centered image: `position: [0, 0]`, `align: center`
- Top-left anchored: `position: [0, 0]`, `align: left`

---

## 7. TEMPLATES

All `content`, `color`, `position`, `size`, and page-type value fields
support Home Assistant Jinja2 templates (`{{ ... }}`).

Common patterns:

```yaml
# Entity state
content: "{{ states('sensor.temperature') }}"

# Conditional color
color: "{{ [255,0,0] if is_state('binary_sensor.door', 'on') else [0,255,0] }}"

# Conditional image path
image_path: "{{ '/config/www/img/open.png' if is_state('binary_sensor.door', 'on') else '/config/www/img/closed.png' }}"

# Multi-line with \n
content: |-
  Line one
  {{ states('sensor.value') }}
```

**Newline support:** Use `|-` after `content:` for multi-line strings.

On the `duration` field, templates are evaluated only when the page
is displayed (not at config-write time).

---

## 8. VARIABLES (components pages only)

Define reusable template expressions in the page config:

```yaml
page_type: components
variables:
  power: "{{ states('sensor.solar_power') }}"
  storage: "{{ states('sensor.battery_level') }}"
components:
  - type: text
    content: "{{ power }}W"
    position: [32, 8]
  - type: text
    content: "{{ storage }}%"
    position: [32, 32]
```

Variables are resolved at render time. Not supported in service calls
(use HA action-level variables instead).

---

## 9. LIGHT ENTITY

| Property | Range | Notes |
|---|---|---|
| `is_on` | true/false | Screen power state |
| `brightness` | 0-255 | Maps to device 0-100% |
| `color_mode` | brightness | No RGB control |

Brightness 255 = 100%, 128 = ~50%, etc.

---

## 10. LIMITATIONS & QUIRKS

- **Buzzer may damage device** — use sparingly if at all
- **Update-page spam crashes** — avoid rapid-fire `update_page` calls
- **Text is always UPPERCASE** — lowercase input will be uppercased
- **All services are entity-scoped** — must target the specific sensor entity
- **Polling-based** — no push/subscribe; state refreshes on HA update cycle
- **Display crash with animated GIFs** — may freeze with complex GIFs; reduce brightness to ~90%
- **Single sensor entity** — no per-page or per-metric sensor entities
- **Cloud API for discovery** — auto-discovery calls `app.divoom-gz.com`; may fail on isolated networks
