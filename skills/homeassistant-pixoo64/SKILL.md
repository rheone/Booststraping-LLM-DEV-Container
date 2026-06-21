---
name: homeassistant-pixoo64
description: >-
  Control Divoom Pixoo 64 displays via Home Assistant. Sends page data
  (components, clock, GIF, visualizer, fuel, PV, progress bar), manages
  display on/off and brightness, and handles buzzer/restart. Provides
  entity discovery (sensor + light), the wake-notify-sleep workflow,
  and inline push-notification patterns. Covers all 8 page types and
  4 entity services. Use when the user mentions Pixoo, Pixoo64, Divoom,
  pixel display, 64x64 display, or wants to show notifications, sensor
  data, images, GIFs, or visual feedback on a Divoom Pixoo.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Home Assistant Divoom Pixoo 64 Skill

Divoom Pixoo 64 integration — a 64x64 pixel LAN-connected display with
a sensor entity (current page) and a light entity (on/off + brightness).

> **Load [REFERENCE.md](REFERENCE.md) for full parameter tables and
> [EXAMPLES.md](EXAMPLES.md) for generalized patterns.**

## 64x64 Display Constraints

| Constraint | Guidance |
|---|---|
| **Canvas** | 64 x 64 pixels — every pixel counts |
| **Images** | Prefer images pre-scaled to <=64 px (use `height`/`width` params). Warn if source exceeds 64 px in both dimensions. |
| **Text centering** | Default `align: center` with `position: [32, Y]` |
| **Image centering** | Default `align: center` with `position: [0, 0]` |
| **Text color** | Warn if same color as background or border beneath it |
| **Line breaks** | Use `\n` in `content:` for multi-line. Warn when text likely exceeds font width (see REFERENCE.md FONTS). |
| **Long text** | If content exceeds the font's per-line capacity, suggest truncating or splitting across multiple text components rather than scrolling |

## Entity Discovery

Two entities per Pixoo device:

```
sensor.divoom_pixoo_64_display_current_page    # Target for show_message
light.divoom_pixoo_64_display_light             # On/off + brightness
```

Find them: **Settings -> Devices -> Divoom Pixoo 64** -> entities list.
Or from **Developer Tools -> States** (look for entity_id containing `pixoo`).

## Quick Start

### Wake -> Notify -> Sleep (brief notification)

```yaml
# Ask the user: "How many seconds should the notification display?"
# Set both duration and delay to that value.
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: text
            content: "Notification"
            font: gicko
            color: white
            align: center
            position: [32, 20]
          - type: text
            content: "Brief message"
            font: pico_8
            color: [0, 200, 255]
            align: center
            position: [32, 40]
      duration: 15
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 15

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

### Standalone Inline Notification

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: text
        content: "Sensor Value"
        font: eleven_pix
        color: yellow
        align: center
        position: [32, 32]
  duration: 10
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

## Core Workflows

### 1. Brief Notification (Wake -> Notify -> Sleep)

1. **Wake** <- `light.turn_on` on the Pixoo light entity
2. **Show** <- `divoom_pixoo.show_message` with `page_data` + `duration`
3. **Wait** <- `delay` matching the duration
4. **Sleep** <- `light.turn_off` on the Pixoo light entity

**Ask user:** how many seconds should the notification display?
Set both `duration` (service field) and `delay` to that value.

### 2. Persistent Page Update

Use when the display is already on and showing rotating pages.
The `show_message` service temporarily overrides the normal rotation
and returns to it after `duration` seconds.

### 3. Pre-designed Page Types

For `clock`, `channel`, `visualizer`, `gif`, `pv`, `fuel`,
`progress_bar` — see EXAMPLES.md for each pattern.

### 4. Safety Cutoff

Always pair non-timed notifications with an auto-off safety:

```yaml
alias: "[pixoo] Auto-off after 2 minutes"
triggers:
  - trigger: state
    entity_id: light.divoom_pixoo_64_display_light
    to: "on"
    for:
      minutes: 2
actions:
  - action: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
mode: single
```

### 5. DND Gating

Gate non-critical Pixoo notifications:

```yaml
condition:
  - condition: state
    entity_id: input_boolean.do_not_disturb
    state: "off"
```
