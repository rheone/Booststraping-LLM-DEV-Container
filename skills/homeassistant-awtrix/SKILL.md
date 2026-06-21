---
name: homeassistant-awtrix
description: >-
  Control AWTRIX 3 custom firmware on Ulanzi TC001 Smart Pixel Clocks via Home
  Assistant. Sends notifications, manages custom apps, adjusts display settings,
  plays RTTTL sounds, and draws graphics on the 8x32 LED matrix. Provides a
  3-tier detection fallback: awtrix.* service calls (MiguelAngelLV/ha-awtrix) →
  per-device services (10der) → raw MQTT publish. Use when the user mentions
  AWTRIX, Ulanzi, pixel clock, matrix display, TC001, LED matrix, notification
  to clock, or wants to display alerts, sensor data, timers, or visual feedback
  on a pixel display.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Home Assistant AWTRIX 3 Skill

Control AWTRIX 3 firmware on Ulanzi TC001 (or DIY) pixel matrix clocks.

> **Load [REFERENCE.md](REFERENCE.md) for parameter tables and [EXAMPLES.md](EXAMPLES.md) for patterns.**
>
> **Icons are LaMetric IDs** — browse the gallery at https://developer.lametric.com/icons or use the AWTRIX web interface **Icons** tab. Popular IDs with descriptions are in REFERENCE.md §15.

## Detection & Fallback Chain

Determine which integration is available — try Tier 1 first, fall back down:

| Tier | Component | Detection | Calling Pattern |
|------|-----------|-----------|-----------------|
| **1** | [MiguelAngelLV/ha-awtrix](https://github.com/MiguelAngelLV/ha-awtrix) | Service `awtrix.notification` exists via `hass.services` | `awtrix.notification` with `device` selector |
| **2** | [10der/...awtrix_notification](https://github.com/10der/homeassistant-custom_components-awtrix) | Dir `custom_components/awtrix_notification/` exists | `notify.{device}` or `awtrix.{device}_*` |
| **3** | Raw MQTT | MQTT configured on device | `mqtt.publish` to `{prefix}/notify` |

**If none found:** Suggest HACS install of `MiguelAngelLV/ha-awtrix`.

### MQTT Prefix Discovery (Tier 3)

Find the device's MQTT topic root (e.g. `awtrix_bedroom`):
1. Scan entity registry for entity with `original_name == "Device topic"` → read its state
2. Alternatively: Scan device registry for manufacturer `"Blueforcer"`, find any entity whose `entity_id` contains `"device_topic"`

## Quick Start

```yaml
# Tier 1 — simplest notification (preferred)
service: awtrix.notification
data:
  device: 9790d9c0deadbeef     # From device selector
  text: "Garage door open!"
  icon: "33655"                 # LaMetric icon ID
  duration: 10
  sound: beep

# Tier 2 — via notify platform
service: notify.bedroom_awtrix
data:
  message: "Garage door open!"
  data:
    icon: "33655"
    sound: beep

# Tier 3 — raw MQTT
service: mqtt.publish
data:
  topic: "awtrix_bedroom/notify"
  payload: '{"text":"Garage door open!","icon":"33655","duration":10,"sound":"beep"}'
  retain: false
```

## Automation Workflow

1. **Detect tier** — scan `hass.services` for `awtrix.notification`, check `custom_components/` directory
2. **Discover devices** — query entity registry → filter `manufacturer == "Blueforcer"` → get device IDs
3. **Suggest HACS** if nothing found: `MiguelAngelLV/ha-awtrix` via `hacs://repository/MiguelAngelLV/ha-awtrix`
4. **Write action** at highest available tier
5. **Add conditions** — gate non-critical notifications on `do_not_disturb == false` if exists

## Notification vs Custom App

| Aspect | Notification | Custom App |
|--------|-------------|------------|
| Lifecycle | One-shot, auto-dismissed after `duration` | Persistent, rotates in app loop |
| `hold` | Yes — stays until dismissed | No |
| `sound`/`rtttl` | Yes | No |
| `lifetime` | No | Auto-remove if not updated in N seconds |
| `save` | No | Save to flash (survives reboot) |
| Use case | Alerts, timers, brief status | Dashboards, now-playing, data displays |

## DND Convention

For this repo, gate non-critical notifications:
```yaml
condition:
  - condition: state
    entity_id: input_boolean.do_not_disturb
    state: "off"
```
