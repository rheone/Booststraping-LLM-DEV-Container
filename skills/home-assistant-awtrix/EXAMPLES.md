# AWTRIX 3 — Automation Examples

20 patterns. Each shows the preferred tier first, with fallbacks noted.

> **Tier legend:** 🟢 = MiguelAngelLV/ha-awtrix  🟡 = 10der  🔴 = Raw MQTT

---

## 1. Simple Text Notification

**Use case:** Brief alert — door opened, motion detected, device online

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Front door opened"
  icon: "33655"
  duration: 8
  sound: beep
```

---

## 2. Critical Hold Notification (Smoke/CO/Leak)

**Use case:** Safety alert — persists until explicitly dismissed. Wakes matrix if asleep.

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "SMOKE DETECTED!"
  icon: "763"
  hold: true
  sound: alarm
  loopSound: true
  wakeup: true
```

**Dismiss when alarm clears:**
```yaml
service: awtrix.dismiss
data:
  device: 9790d9c0deadbeef
```

---

## 3. Weather Alert (Rain/Frost/Storm)

**Use case:** Weather entity condition changes to rainy/extreme

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Rain expected today"
  icon: "2284"
  duration: 10
  color: [0, 120, 255]

# 🔴 Tier 3
service: mqtt.publish
data:
  topic: "awtrix_bedroom/notify"
  payload: '{"text":"Rain expected today","icon":"2284","duration":10,"color":[0,120,255]}'
```

---

## 4. Media Now Playing (Custom App)

**Use case:** Persistent app showing current track/artist. Updates on each state change. Auto-deletes when playback stops.

```yaml
# 🟢 Tier 1 — create/update app
service: awtrix.custom_app
data:
  device: 9790d9c0deadbeef
  app: now_playing
  text: '{{ state_attr("media_player.living_room_tv", "media_title") }}'
  icon: "1766"
  duration: 10
  lifetime: 600
  noScroll: false

# Delete when media stops
service: awtrix.delete_custom_app
data:
  device: 9790d9c0deadbeef
  app: now_playing
```

**Icon suggestions:** 1766 (music note), 445 (radio), 13954 (headphones)

---

## 5. 3D Printer/Device Complete

**Use case:** 3D printer finishes, soldering iron reaches temp, lights off, etc.

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Print complete!"
  icon: "522"
  duration: 12
  sound: beep
  wakeup: true
```

**Icon suggestions:** 522 (printer), 1704 (checkmark), 18360 (laundry/cycle done)

---

## 6. Vacuum State Change

**Use case:** Vacuum starts cleaning or returns to dock

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: >-
    {% if is_state("vacuum.roborock_q7_m5", "cleaning") %}
      Vacuum cleaning
    {% else %}
      Vacuum docked
    {% endif %}
  icon: "87"
  duration: 6
```

---

## 7. Escalating Door Alert (5-min timeout)

**Use case:** Notify immediately, then escalate with hold if still open after 5 minutes.

```yaml
# Step 1 — initial alert (trigger: binary_sensor.front_door → on)
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Front door opened"
  icon: "33655"
  duration: 8

# Step 2 — escalated alert (trigger: same sensor, for: 5 min)
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Front door open 5+ min!"
  icon: "33655"
  hold: true
  sound: beep
  loopSound: true

# Dismiss on door close
service: awtrix.dismiss
data:
  device: 9790d9c0deadbeef
```

---

## 8. Morning Briefing (Custom App + Work-Day)

**Use case:** Daily info on the clock — temperature + weather condition. Only on work days, respects DND.

```yaml
alias: "[awtrix] Morning briefing"
trigger:
  - platform: time
    at: "07:00:00"
condition:
  - condition: state
    entity_id: input_boolean.is_a_work_day
    state: "on"
  - condition: state
    entity_id: input_boolean.do_not_disturb
    state: "off"
action:
  - service: awtrix.custom_app
    data:
      device: 9790d9c0deadbeef
      app: morning_brief
      text: >-
        {{ states("sensor.outdoor_temperature") }}° 
        {{ state_attr("weather.forecast_home", "forecast")[0].condition }}
      icon: "6204"
      duration: 12
      lifetime: 3600
      center: true
mode: single
```

---

## 9. Pomodoro / Deep Work Timer

**Use case:** Focus session start/end notifications for productivity

```yaml
# Session start
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Focus — 25 min"
  icon: "21328"
  duration: 6

# Session end
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Break — 5 min"
  icon: "1101"
  sound: beep
  duration: 10
```

---

## 10. Energy/Power Dashboard (Custom App)

**Use case:** Live solar production + consumption. Updates every 5 min, auto-removed if stale.

```yaml
# 🟢 Tier 1
service: awtrix.custom_app
data:
  device: 9790d9c0deadbeef
  app: energy
  text: "{{ states('sensor.solar_production') }}W"
  icon: "24723"
  bar:
    - "{{ states('sensor.solar_production') | int / 100 }}"
  barBC: [30, 30, 30]
  duration: 10
  lifetime: 600
```

**Icon suggestions:** 24723 (sun/solar), 16320 (energy), 371 (power plug)

---

## 11. Laundry / Dryer / Dishwasher Done

**Use case:** Power-monitored appliance — notify when cycle finishes

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: "Laundry finished!"
  icon: "18360"
  duration: 12
  sound: beep
  wakeup: true
```

---

## 12. Play RTTTL Ringtone

**Use case:** Custom melody for special events (birthday, timer end)

```yaml
# 🟢 Tier 1 (via notification with no visible text)
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text: ""
  rtttl: "starwars:d=4,o=5,b=125:8d5,8d5,8d5,2g5,8d6,8c6,8b5,8a5,2g5"
  duration: 15

# 🟡 Tier 2
service: awtrix.bedroom_awtrix_rtttl
data:
  rtttl: "starwars:d=4,o=5,b=125:8d5,8d5,8d5,2g5,8d6,8c6,8b5,8a5,2g5"

# 🔴 Tier 3
service: mqtt.publish
data:
  topic: "awtrix_bedroom/rtttl"
  payload: "starwars:d=4,o=5,b=125:8d5,8d5,8d5,2g5,8d6,8c6,8b5,8a5,2g5"
```

---

## 13. Switch to Big Clock Mode (Nighttime)

**Use case:** Dim the display and show large time for night visibility

```yaml
service: awtrix.settings
data:
  device: 9790d9c0deadbeef
  TMODE: 5
  BRI: 30
```

**Restore daytime:**
```yaml
service: awtrix.settings
data:
  device: 9790d9c0deadbeef
  TMODE: 1
  BRI: 150
  ABRI: true
```

---

## 14. Day/Night Brightness Scene

**Use case:** Sun-triggered brightness adjustment

```yaml
# Sunset — dim
service: awtrix.settings
data:
  device: 9790d9c0deadbeef
  BRI: 20

# Sunrise — bright
service: awtrix.settings
data:
  device: 9790d9c0deadbeef
  BRI: 180
  ABRI: true
```

---

## 15. Colored Indicator for Sensor Status

**Use case:** Persistent status light — garage open = red blink, closed = green

```yaml
# Garage open → red blinking
service: mqtt.publish
data:
  topic: "awtrix_bedroom/indicator1"
  payload: '{"color":[255,0,0],"blink":1000}'

# Garage closed → green solid
service: mqtt.publish
data:
  topic: "awtrix_bedroom/indicator1"
  payload: '{"color":[0,255,0]}'

# Unknown → off
service: mqtt.publish
data:
  topic: "awtrix_bedroom/indicator1"
  payload: '{"color":"0"}'
```

---

## 16. Sunset Mood Lighting

**Use case:** Warm ambient glow on the matrix when sun sets

```yaml
service: mqtt.publish
data:
  topic: "awtrix_bedroom/moodlight"
  payload: '{"brightness":80,"kelvin":2200}'

# Turn off at sunrise
service: mqtt.publish
data:
  topic: "awtrix_bedroom/moodlight"
  payload: ""
```

---

## 17. Compound Notification (Multi-Color + Progress)

**Use case:** Rich notification with colored text fragments + progress bar

```yaml
# 🟢 Tier 1
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  text:
    - t: "Battery: "
      c: "FFFFFF"
    - t: "85%"
      c: "00FF00"
  icon: "1543"
  progress: 85
  progressC: "#00FF00"
  progressBC: "#333333"
  duration: 8
```

---

## 18. Multiple Sensor Custom Apps

**Use case:** Temperature + Humidity as rotating custom apps with lifetime management

```yaml
# Temperature app
service: awtrix.custom_app
data:
  device: 9790d9c0deadbeef
  app: sensor_temp
  text: '{{ states("sensor.outdoor_temperature") }}°'
  icon: "2056"
  duration: 8
  lifetime: 900
  pos: 0

# Humidity app
service: awtrix.custom_app
data:
  device: 9790d9c0deadbeef
  app: sensor_humidity
  text: '{{ states("sensor.outdoor_humidity") }}%'
  icon: "1760"
  duration: 8
  lifetime: 900
  pos: 1
```

**Delete all:** Send empty payload to `{prefix}/custom/sensor_temp`.  
**Delete single:** Send empty payload to `{prefix}/custom/sensor_temp`.

---

## 19. Deep Sleep Overnight

**Use case:** Turn off matrix at bedtime to save power. Wake only via physical button.

```yaml
service: awtrix.deep_sleep
data:
  device: 9790d9c0deadbeef
  sleep: 28800    # 8 hours
```

**Tier 2/3 note:** Tier 2 has no deep sleep service. Fall back to MQTT:
```yaml
service: mqtt.publish
data:
  topic: "awtrix_bedroom/sleep"
  payload: '{"sleep":28800}'
```

---

## 20. Pixel Art / Drawing

**Use case:** Custom graphics — circles, rectangles, and text via draw instructions

```yaml
# 🟢 Tier 1
service: awtrix.custom_app
data:
  device: 9790d9c0deadbeef
  app: pixel_art
  draw:
    - dc: [16, 4, 4, "#FF4500"]
    - df: [24, 0, 8, 8, "#87CEEB"]
    - dt: [0, 4, "HI", "#00FF00"]
  duration: 15
  lifetime: 3600

# 🔴 Tier 3
service: mqtt.publish
data:
  topic: "awtrix_bedroom/custom/pixel_art"
  payload: >
    {"draw":[{"dc":[16,4,4,"#FF4500"]},{"df":[24,0,8,8,"#87CEEB"]},{"dt":[0,4,"HI","#00FF00"]}],
     "duration":15,"lifetime":3600}
```

---

## 21. Brief Notification (Wake → Notify → Auto-Return)

**Use case:** Matrix is powered off, briefly wake for a notification, then auto-return to prior state.

**Key insight:** AWTRIX has two "off" states:
- **Power off** (`power: false`) — can be woken via API with `wakeup: true`
- **Deep sleep** (`sleep` topic) — **cannot be woken via API** (physical button only)

For brief notifications, use the built-in `wakeup` mechanism. This handles wake, notify, and auto-return atomically — no separate power-off needed.

```yaml
# 🟢 Tier 1 — wakeup + duration handles everything
service: awtrix.notification
data:
  device: 9790d9c0deadbeef
  wakeup: true               # Wake matrix from power-off
  text: "Front door opened"
  icon: "33655"
  duration: "{{ 10 }}"       # Prompt user: how many seconds?
  sound: beep
```

After `duration` seconds, the notification ends and the matrix returns to its prior state.

**Explicit power control (Tier 3 / MQTT) — for full control:**
```yaml
action:
  - service: mqtt.publish
    data:
      topic: "awtrix_bedroom/power"
      payload: '{"power": true}'

  - delay:
      seconds: 1               # Brief pause for matrix init

  - service: mqtt.publish
    data:
      topic: "awtrix_bedroom/notify"
      payload: '{"text":"Brief notice","icon":"33655","duration":15,"sound":"beep"}'

  - delay:
      seconds: 15

  - service: mqtt.publish
    data:
      topic: "awtrix_bedroom/power"
      payload: '{"power": false}'
```

**Avoid deep sleep** for any pattern needing API wake. Use deep sleep only when the device stays off for long periods (e.g. overnight) where waking via physical middle-button press is acceptable.

---

## Icon Gallery

Icons are **LaMetric icon IDs** from https://developer.lametric.com/icons. Browse the gallery by category (Indicators & Notifications, Weather, Home, etc.) and use the numeric ID.

Preview any icon directly: `https://developer.lametric.com/content/apps/icon_thumbs/{ID}`

See REFERENCE.md §15 for a curated table of popular verified IDs organized by category (notifications, weather, home, devices, etc.).
