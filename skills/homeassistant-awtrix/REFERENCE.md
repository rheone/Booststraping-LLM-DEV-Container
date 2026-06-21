# AWTRIX 3 Reference

## 1. Service Comparison (3 Tiers)

| Feature | Tier 1 (MiguelAngelLV) | Tier 2 (10der) | Tier 3 (MQTT) |
|---------|------------------------|----------------|----------------|
| Send notification | `awtrix.notification` | `notify.{device}` | `{prefix}/notify` |
| Dismiss notification | `awtrix.dismiss` | Empty message to `notify` | `{prefix}/notify/dismiss` |
| Create/update custom app | `awtrix.custom_app` | `awtrix.{device}_push_app_data` | `{prefix}/custom/{name}` |
| Delete custom app | `awtrix.delete_custom_app` | Push with empty `data` | `{prefix}/custom/{name}` (empty) |
| Change settings | `awtrix.settings` | `awtrix.{device}_settings` | `{prefix}/settings` |
| Switch to app | `awtrix.switch_app` | `awtrix.{device}_switch_app` | `{prefix}/switch` |
| Deep sleep | `awtrix.deep_sleep` | — | `{prefix}/sleep` |
| Play RTTTL | Via `rtttl` field in notification | `awtrix.{device}_rtttl` | `{prefix}/rtttl` |
| Play sound | Via `sound` field in notification | `awtrix.{device}_sound` | `{prefix}/sound` |
| Mood light | — | — | `{prefix}/moodlight` |
| Power on/off | — | — | `{prefix}/power` |
| Indicators (x3) | — | — | `{prefix}/indicator{1,2,3}` |

---

## 2. Tier 1 — MiguelAngelLV/ha-awtrix Services

All services require `device` (device selector — hex ID of Blueforcer device).

### `awtrix.notification`

Publishes to `{prefix}/notify`. **All fields optional except `device`.**

**Text:**
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `text` | string or array | — | Message or `[{"t":"...","c":"hex"}]` fragments |
| `textCase` | select: 0-2 | 0 | 0=global, 1=uppercase, 2=as-sent |
| `color` | color_rgb | — | Text color |
| `rainbow` | boolean | false | Per-character RGB cycling |
| `gradient` | `["#c1","#c2"]` | — | Two-color text gradient |
| `center` | boolean | true | Center short text |
| `topText` | boolean | false | Draw at top row |
| `textOffset` | number (≥0) | 0 | X scroll offset |
| `blinkText` | number (≥0) | — | Blink interval in ms |
| `fadeText` | number (≥0) | — | Fade interval in ms |
| `noScroll` | boolean | false | Suppress scrolling |
| `scrollSpeed` | number (1-200) | 100 | Scroll speed % |

**Icon:**
| Field | Type | Notes |
|-------|------|-------|
| `icon` | text | LaMetric ID, filename, or Base64 8×8 JPG |
| `pushIcon` | select: 0-2 | 0=static, 1=moves+disappears, 2=moves+reappears |

**Sound:**
| Field | Type | Notes |
|-------|------|-------|
| `sound` | text | RTTTL filename (no ext) or DFplayer 4-digit MP3 |
| `rtttl` | text | Inline RTTTL string |
| `loopSound` | boolean | Loop for duration |

**Graphs & Progress:**
| Field | Type | Notes |
|-------|------|-------|
| `bar` | array of ints | Bar chart (max 16 values, 11 with icon) |
| `line` | array of ints | Line chart (max 16 values, 11 with icon) |
| `autoscale` | boolean | Auto-scale charts |
| `progress` | number (0-100) | Progress bar value |
| `progressC` | color_rgb | Fill color |
| `progressBC` | color_rgb | Background color |
| `draw` | text/array | Drawing instructions (YAML array or JSON string, see §9) |

**Background & Effects:**
| Field | Type | Notes |
|-------|------|-------|
| `background` | color_rgb | Background color |
| `effect` | text | Effect name (see §7 for list) |
| `effectSettings` | object | `{"speed":3,"palette":"Rainbow","blend":true}` |
| `overlay` | text | Overlay effect (see §8) |

**Behavior:**
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `duration` | number (≥1) | 5 | Display seconds |
| `repeat` | number (≥-1) | -1 | Scroll repeats (-1=infinite) |
| `hold` | boolean | false | Hold until dismissed |
| `stack` | boolean | true | Stack (false=replace immediately) |
| `wakeup` | boolean | false | Wake matrix from off (`true` = wake) |
| `clients` | array of strings | — | Forward to other AWTRIX devices |

### `awtrix.dismiss`

```yaml
service: awtrix.dismiss
data:
  device: 9790d9c0deadbeef
```

### `awtrix.custom_app`

Same text/icon/graph/effect/behavior params as notification, plus:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `app` | text | **Yes** | App name (no spaces) |
| `save` | boolean | No | Save to flash (avoid for frequent updates) |
| `lifetime` | number (≥0) | No | Auto-remove after N seconds without update |
| `lifetimeMode` | select: 0-1 | No | 0=delete, 1=red border (stale marker) |
| `pos` | number (≥0) | No | Position in app loop (first push only) |

### `awtrix.delete_custom_app`

```yaml
service: awtrix.delete_custom_app
data:
  device: 9790d9c0deadbeef
  app: my_app_name
```

### `awtrix.settings`

Publishes to `{prefix}/settings`. All fields optional.

**Core:**
| Field | Type | Range | Notes |
|-------|------|-------|-------|
| `ATIME` | number | ≥1 | App display duration (sec) |
| `TEFF` | select | 0-10 | Transition effect (see §10) |
| `TSPEED` | number | ≥1 | Transition speed (ms) |
| `TCOL` | color_rgb | — | Global text color |
| `TMODE` | select | 0-6 | Time app style (see §11) |
| `WD` | boolean | — | Show weekday |
| `BRI` | number | 0-255 | Matrix brightness |
| `ABRI` | boolean | — | Auto brightness |
| `ATRANS` | boolean | — | Auto app transition |
| `TFORMAT` | text | — | Time format string |
| `DFORMAT` | text | — | Date format string |
| `SOM` | boolean | — | Start week Monday |
| `CEL` | boolean | — | Celsius |
| `BLOCKN` | boolean | — | Block physical nav buttons |
| `UPPERCASE` | boolean | — | Force uppercase |
| `SSPEED` | number | ≥1 | Scroll speed % |
| `TIM`/`DAT`/`HUM`/`TEMP`/`BAT` | boolean | — | Enable built-in apps (needs reboot) |
| `MATP` | boolean | — | Enable matrix (like power) |
| `VOL` | number | 0-30 | Buzzer volume |
| `OVERLAY` | select | See §8 | Global overlay |

**Color fields:** `TIME_COL`, `DATE_COL`, `TEMP_COL`, `HUM_COL`, `BAT_COL`, `CHCOL`, `CBCOL`, `CTCOL`, `WDCA`, `WDCI`, `CCORRECTION` (RGB array), `CTEMP` (RGB array).

### `awtrix.switch_app`

```yaml
service: awtrix.switch_app
data:
  device: 9790d9c0deadbeef
  name: "Time"   # Built-in: Time, Date, Temperature, Humidity, Battery
```

### `awtrix.deep_sleep`

```yaml
service: awtrix.deep_sleep
data:
  device: 9790d9c0deadbeef
  sleep: 3600   # seconds. Wake only via physical middle button.
```

---

## 3. Tier 2 — 10der Services

Dynamically named per device: `awtrix.{normalized_name}_{service}` or `notify.{normalized_name}`.  
Name = `device.name_by_user or device.name`, spaces → underscores.

### Notify Platform

```yaml
service: notify.bedroom_awtrix
data:
  message: "Alert text"
  data:
    icon: "33655"
    sound: beep
    hold: true
```

Empty message → dismisses held notification. Icon URL starting with `http://` auto-downloads to Base64.

### `awtrix.{device}_push_app_data`

```yaml
service: awtrix.bedroom_awtrix_push_app_data
data:
  name: my_app
  data:
    text: "Hello"
    icon: "87"
    rainbow: true
    duration: 5
    lifetime: 900
```

Empty `data: {}` → deletes the custom app.

### `awtrix.{device}_settings`

```yaml
service: awtrix.bedroom_awtrix_settings
data:
  BRI: 100
  TMODE: 5
```

### `awtrix.{device}_switch_app`

```yaml
service: awtrix.bedroom_awtrix_switch_app
data:
  name: Time
```

### `awtrix.{device}_rtttl`

```yaml
service: awtrix.bedroom_awtrix_rtttl
data:
  rtttl: "two_short:d=4,o=5,b=100:16e6,16e6"
```

### `awtrix.{device}_sound`

```yaml
service: awtrix.bedroom_awtrix_sound
data:
  sound: beep
```

---

## 4. MQTT Topic Reference (Tier 3)

Prefix discovery: Find entity with `original_name == "Device topic"` → its state is the prefix (e.g. `awtrix_bedroom`).

| Topic | Payload | Purpose |
|-------|---------|---------|
| `{prefix}/notify` | `{"text":"...","icon":"...",...}` | Send notification |
| `{prefix}/notify/dismiss` | `""` | Dismiss held notification |
| `{prefix}/custom/{name}` | `{...}` or `""` | Create/update/delete custom app |
| `{prefix}/settings` | `{"BRI":100,...}` | Change settings |
| `{prefix}/switch` | `{"name":"Time"}` | Switch to app |
| `{prefix}/sleep` | `{"sleep":3600}` | Deep sleep |
| `{prefix}/sound` | `{"sound":"beep"}` | Play sound file |
| `{prefix}/rtttl` | Raw RTTTL string | Play inline RTTTL |
| `{prefix}/power` | `{"power":true}` | Power on/off |
| `{prefix}/moodlight` | `{"brightness":170,"color":[R,G,B]}` or `{"kelvin":2300}` | Mood lighting |
| `{prefix}/indicator1` | `{"color":[255,0,0]}` | Indicator (upper-right) |
| `{prefix}/indicator2` | `{"color":[0,255,0]}` | Indicator (right side) |
| `{prefix}/indicator3` | `{"color":[0,0,255]}` | Indicator (lower-right) |
| `{prefix}/nextapp` | `""` | Next app in loop |
| `{prefix}/previousapp` | `""` | Previous app in loop |
| `{prefix}/reboot` | `""` | Reboot device |
| `{prefix}/doupdate` | JSON | Firmware update |
| `{prefix}/sendscreen` | `""` | Request screen capture |
| `{prefix}/screen` | (receives) | Screen data response |

---

## 5. Colors

Accepted everywhere (text, background, bar, progress, indicators, settings):

| Format | Example |
|--------|---------|
| RGB array | `[255, 0, 0]` |
| Hex string | `"#FF0000"` |
| Gradient | `["#FF0000", "#0000FF"]` |

---

## 6. Text Fragments (Multi-Color)

```yaml
text:
  - t: "Temp: "
    c: "FFFFFF"
  - t: "72°F"
    c: "00FF00"
```

---

## 7. Background Effects

| # | Name | Speed | Palette | Blend |
|---|------|-------|---------|-------|
| 1 | BrickBreaker | — | — | — |
| 2 | Checkerboard | 1 | Rainbow | yes |
| 3 | ColorWaves | 3 | Rainbow | yes |
| 4 | Fade | 1 | Rainbow | yes |
| 5 | Fireworks | 1 | Rainbow | yes |
| 6 | LookingEyes | — | — | — |
| 7 | Matrix | 8 | — | — |
| 8 | MovingLine | 1 | Rainbow | yes |
| 9 | Pacifica | 3 | Ocean | yes |
| 10 | PingPong | 8 | Rainbow | — |
| 11 | Plasma | 2 | Rainbow | yes |
| 12 | PlasmaCloud | 3 | Rainbow | yes |
| 13 | Radar | 1 | Rainbow | yes |
| 14 | Ripple | 3 | Rainbow | yes |
| 15 | Snake | 3 | Rainbow | — |
| 16 | SwirlIn | 4 | Rainbow | — |
| 17 | SwirlOut | 4 | Rainbow | — |
| 18 | TheaterChase | 3 | Rainbow | yes |
| 19 | TwinklingStars | 4 | Ocean | no |

**effectSettings:** `{"speed":3, "palette":"Rainbow", "blend":true}`  
Palettes: Cloud, Lava, Ocean, Forest, Stripe, Party, Heat, Rainbow

---

## 8. Overlay Effects

`"clear"`, `"snow"`, `"rain"`, `"drizzle"`, `"storm"`, `"thunder"`, `"frost"`

---

## 9. Drawing Instructions

Array of command objects. Color `cl` = `"#RRGGBB"` or `[R,G,B]`.

| Command | Array | Description |
|---------|-------|-------------|
| `dp` | `[x, y, cl]` | Draw pixel |
| `dl` | `[x0, y0, x1, y1, cl]` | Draw line |
| `dr` | `[x, y, w, h, cl]` | Rectangle outline |
| `df` | `[x, y, w, h, cl]` | Filled rectangle |
| `dc` | `[x, y, r, cl]` | Circle outline |
| `dfc` | `[x, y, r, cl]` | Filled circle |
| `dt` | `[x, y, t, cl]` | Draw text |
| `db` | `[x, y, w, h, [bmp]]` | RGB888 bitmap array |

```yaml
draw:
  - dc: [28, 4, 3, "#FF0000"]
  - dr: [20, 4, 4, 4, "#0000FF"]
  - dt: [0, 0, "Hi", "#00FF00"]
```

---

## 10. Transition Effects (TEFF)

| Code | Effect | Code | Effect |
|------|--------|------|--------|
| 0 | Random | 6 | Curtain |
| 1 | Slide | 7 | Ripple |
| 2 | Dim | 8 | Blink |
| 3 | Zoom | 9 | Reload |
| 4 | Rotate | 10 | Fade |
| 5 | Pixelate | | |

---

## 11. Time App Styles (TMODE)

| Mode | Description |
|------|-------------|
| 0 | Time + weekday bar (bottom) |
| 1 | Time + weekday bar (bottom) + calendar box |
| 2 | Time + weekday bar (top) + calendar box |
| 3 | Time + weekday bar (bottom) + calendar icon |
| 4 | Time + weekday bar (top) + calendar icon |
| 5 | Big time (large font, optional `bigtime.gif` 32×8) |
| 6 | Binary time (3 rows × 6 dots) |

---

## 12. Time Format Strings

| Format | Example |
|--------|---------|
| `%H:%M:%S` | 13:30:45 |
| `%l:%M:%S` | 1:30:45 |
| `%H:%M` | 13:30 |
| `%H %M` | 13.30 (blinking colon) |
| `%l:%M` | 1:30 |
| `%l %M` | 1:30 (blinking colon) |
| `%l:%M %p` | 1:30 PM |
| `%l %M %p` | 1:30 PM (blinking colon) |

---

## 13. Date Format Strings

For the `DFORMAT` setting:

| Format | Example | Description |
|--------|---------|-------------|
| `%d.%m.%y` | 16.04.22 | Day.Month.Year (short) |
| `%d.%m` | 16.04 | Day.Month |
| `%y-%m-%d` | 22-04-16 | Year-Month-Day |
| `%m-%d` | 04-16 | Month-Day |
| `%m/%d/%y` | 04/16/22 | Month/Day/Year |
| `%m/%d` | 04/16 | Month/Day |
| `%d/%m/%y` | 16/04/22 | Day/Month/Year |
| `%d/%m` | 16/04 | Day/Month |
| `%m-%d-%y` | 04-16-22 | Month-Day-Year |

---

## 14. Sound System

- **RTTTL file:** Place `.txt` with RTTTL content in `MELODIES/` folder → reference by filename (no ext)
- **Inline RTTTL:** Send raw RTTTL string e.g. `"two_short:d=4,o=5,b=100:16e6,16e6"`
- **DFplayer MP3:** Optional module. Files in `MP3/` folder on SD named `0001.mp3` → `"sound": "0001"`
- **Built-in buzzer:** Passive buzzer, monophonic RTTTL only

---

## 15. Icon System

AWTRIX 3 uses **LaMetric icon IDs** (numeric) to display icons on the matrix. All icons below are verified to exist on LaMetric's servers.

### Browsing & Downloading Icons

1. **Browser:** Go to https://developer.lametric.com/icons (JavaScript required) and browse by category (Indicators, Weather, Animals, etc.) — use the numeric `Icon ID` shown for each
2. **On-device:** Open your AWTRIX web interface → **Icons** tab → enter an ID → **Preview** → **Download** to save to the device's `ICONS/` folder
3. **Mobile app:** The official AWTRIX 3 app (iOS/Android) has a built-in icon browser with 1800+ icons and animations
4. **Direct image URL:** Icons are served from `https://developer.lametric.com/content/apps/icon_thumbs/{ID}` — the AWTRIX device fetches from this same source

### Setting an Icon

In any notification, custom app, or MQTT payload, set `icon` to the numeric ID as a string:
```yaml
icon: "87"       # vacuum cleaner
icon: "33655"    # door/garage
icon: "763"      # alarm
```

### Sources

| How | Details |
|-----|---------|
| **LaMetric ID** | Browse gallery → use numeric ID (e.g. `"763"`) |
| **Custom file** | Upload GIF (32×8, 8-bit, no transparency) or JPG (8×8) via web UI `ICONS/` folder → reference by filename (no ext) |
| **Inline Base64** | 8×8 RGB888 JPG encoded as Base64 string — useful for dynamic icons from automations |
| **Tier 2 URL auto-download** | (10der component) Icon URL starting with `http://` auto-downloads and Base64 encodes |

### Popular Verified Icon IDs

All IDs below return valid images from `https://developer.lametric.com/content/apps/icon_thumbs/{ID}`.

**Notifications & Alerts:**
| Icon | ID | Icon | ID |
|------|----|------|----|
| Alarm / Bell | 763 | Warning triangle | 636 |
| Info circle | 52176 | Checkmark / OK | 25998 |
| Heart | 1101 | Clock | 118 |
| Lock | 172 | Lightbulb | 300 |

**Weather & Environment:**
| Icon | ID | Icon | ID |
|------|----|------|----|
| Sun / Clear | 2282 | Cloudy | 2283 |
| Rain | 2284 | Snow | 2289 |
| Lightning | 630 | Fog | 17056 |
| Storm | 49299 | Moon | 2314 |
| Partly cloudy | 2286 | Wind | 15618 |

**Home & Devices:**
| Icon | ID | Icon | ID |
|------|----|------|----|
| Home | 96 | Door / Garage | 33655 |
| Vacuum | 87 | Printer / 3D Printer | 522 |
| Laundry / Wash | 18360 | Energy / Solar | 24723 |
| Power plug | 371 | Battery | 1543 |
| Thermometer | 2056 | Humidity / Droplet | 1760 |
| Music note | 1766 | Headphones | 13954 |
| Radio | 445 | Focus / Target | 21328 |
| Motion sensor | 400 | Camera | 901 |

**Misc:**
| Icon | ID | Icon | ID |
|------|----|------|----|
| Fire | 925 | Party | 17916 |
| Star | 2463 | Smiley | 117 |
| Trophy | 2358 | Gauge | 722 |

---

## 16. Mood Lighting

```yaml
service: mqtt.publish
data:
  topic: "awtrix_bedroom/moodlight"
  payload: '{"brightness":170, "color":[155,38,182]}'
```

Send empty payload to disable. Params: `brightness` (0–255), `kelvin`, `color`.

---

## 17. Indicators

Corner LEDs for persistent status. Three positions:

```yaml
# Indicator 1 (upper-right) — red blinking
service: mqtt.publish
data:
  topic: "awtrix_bedroom/indicator1"
  payload: '{"color":[255,0,0], "blink":500}'
```

Hide: `{"color":[0,0,0]}` or `{"color":"0"}` or empty payload.  
Extra fields: `blink` (ms), `fade` (ms).

---

## 18. Default Built-in App Names

`Time`, `Date`, `Temperature`, `Humidity`, `Battery`

---

## 19. Device Settings Keys (Complete)

`ATIME`, `TEFF`, `TSPEED`, `TCOL`, `TMODE`, `CHCOL`, `CBCOL`, `CTCOL`, `WD`, `WDCA`, `WDCI`, `BRI`, `ABRI`, `ATRANS`, `CCORRECTION`, `CTEMP`, `TFORMAT`, `DFORMAT`, `SOM`, `CEL`, `BLOCKN`, `UPPERCASE`, `TIME_COL`, `DATE_COL`, `TEMP_COL`, `HUM_COL`, `BAT_COL`, `SSPEED`, `TIM`, `DAT`, `HUM`, `TEMP`, `BAT`, `MATP`, `VOL`, `OVERLAY`
