# Pixoo 64 — Automation Examples

Generalized patterns. Replace placeholder entity IDs with your own.

> **Legend:** `[wake]` = wakes display, `[inline]` = no wake/sleep, `[⚠]` = risk warning

---

## 1. Bordered Notification with Header + Body [wake]

Border rectangle, centered header, centered body. Ask user for duration.

```yaml
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: rectangle
            color: white
            filled: false
            position: [0, 0]
            size: [64, 64]
          - type: text
            content: "Header"
            font: gicko
            color: yellow
            align: center
            position: [32, 6]
          - type: text
            content: "Body message"
            font: eleven_pix
            color: white
            align: center
            position: [32, 36]
      duration: 15  # Ask user for seconds
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 15  # Must match duration

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

---

## 2. Standalone Inline Notification [inline]

Does not wake or sleep — just overrides rotation for `duration` seconds.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: text
        content: "Alert"
        font: gicko
        color: [255, 200, 0]
        align: center
        position: [32, 24]
      - type: text
        content: "Message text"
        font: pico_8
        color: white
        align: center
        position: [32, 44]
  duration: 10
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 3. Multi-line Sensor Value [inline]

Use `\n` for line breaks. Warn if text exceeds font width.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: text
        content: |-
          Temperature
          {{ states('sensor.temperature') }}°
        font: eleven_pix
        color: [0, 200, 255]
        align: center
        position: [32, 20]
  duration: 8
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 4. Full-screen Image [wake]

64x64 image centered. Warn if image source exceeds 64 px.

```yaml
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: image
            align: center
            image_path: /config/www/images/icon_64x64.png
            position: [0, 0]
      duration: 20
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 20

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

---

## 5. Image + Overlay Text [wake]

Image as background with text overlaid. Warn if text color matches
image's dominant color area beneath it.

```yaml
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: image
            align: center
            image_path: /config/www/images/background_64x64.png
            position: [0, 0]
          - type: text
            content: "Overlay"
            font: gicko
            color: white
            align: center
            position: [32, 52]
      duration: 15
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 15

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

---

## 6. Status Indicators with Colored Rectangles [wake]

Small filled rectangles as binary status indicators, with labels.

```yaml
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: rectangle
            color: white
            filled: false
            position: [0, 0]
            size: [64, 64]
          - type: rectangle
            color: "{{ [0,255,0] if is_state('binary_sensor.example_1', 'on') else [255,0,0] }}"
            filled: true
            position: [6, 6]
            size: [8, 8]
          - type: text
            content: "Status A"
            font: five_pix
            color: [200, 200, 200]
            position: [20, 7]
          - type: rectangle
            color: "{{ [0,255,0] if is_state('binary_sensor.example_2', 'on') else [255,0,0] }}"
            filled: true
            position: [6, 22]
            size: [8, 8]
          - type: text
            content: "Status B"
            font: five_pix
            color: [200, 200, 200]
            position: [20, 23]
          - type: rectangle
            color: "{{ [0,255,0] if is_state('binary_sensor.example_3', 'on') else [255,0,0] }}"
            filled: true
            position: [6, 38]
            size: [8, 8]
          - type: text
            content: "Status C"
            font: five_pix
            color: [200, 200, 200]
            position: [20, 39]
      duration: 30
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 30

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

---

## 7. Clock Face [inline]

Set a built-in Divoom clock face by ID.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: clock
    id: 182           # Replace with desired clock ID
  duration: 30
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 8. Channel Display [inline]

Switch to a Divoom custom channel.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: channel
    id: 0             # 0, 1, or 2
  duration: 30
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 9. Animated GIF [wake]

Play a 64x64 GIF from URL. Warn if URL source is unreliable.
Warn that complex GIFs may crash the display (reduce brightness).

```yaml
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: gif
        gif_url: https://example.com/animation_64x64.gif
      duration: 20
    target:
      entity_id: sensor.divoom_pixoo_64_display_current_page

  - delay:
      seconds: 20

  - service: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
```

---

## 10. Progress Bar [inline]

Progress bar with header, percentage, and footer.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: progress_bar
    header: "TASK"
    progress: "{{ states('sensor.progress_sensor') }}"
    footer: "{{ now().strftime('%H:%M') }}"
    progress_bar_color: [0, 200, 100]
    bg_color: [30, 30, 80]
    header_font_color: [255, 200, 0]
  duration: 15
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 11. Solar / PV Dashboard [inline]

Pre-designed solar display with battery icon and grid flow.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: pv
    power: "{{ states('sensor.solar_power') }}"
    storage: "{{ states('sensor.battery_soc') }}"
    discharge: "{{ states('sensor.battery_power') }}"
    powerhousetotal: "{{ states('sensor.house_power') }}"
    vomNetz: "{{ states('sensor.grid_power') }}"
    time: "{{ now().strftime('%H:%M') }}"
  duration: 20
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 12. Fuel Price Display [inline]

Pre-designed fuel station pricing with three fuel types.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: fuel
    title: "Station Name"
    name1: "Fuel A"
    price1: "{{ states('sensor.fuel_price_a') }}"
    name2: "Fuel B"
    price2: "{{ states('sensor.fuel_price_b') }}"
    name3: "Fuel C"
    price3: "{{ states('sensor.fuel_price_c') }}"
    status: "{{ 'Open' if is_state('binary_sensor.station_open', 'on') else 'Closed' }}"
    font_color: white
    bg_color: [255, 230, 0]
    title_color: black
  duration: 20
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 13. Visualizer [inline]

Music visualizer page (code accepts both `visualizer` and `visualiser`).

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: visualizer
    id: 0             # Visualizer position index
  duration: 30
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 14. Buzzer Alert [inline] [⚠]

**Warning: may damage device. Use at own risk.**
Ask user before using this service.

```yaml
service: divoom_pixoo.play_buzzer
data:
  buzz_cycle_time_millis: 500
  idle_cycle_time_millis: 500
  total_time: 3000
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 15. Safety Cutoff Automation

Turns off the Pixoo display if left on accidentally for too long.

```yaml
alias: "[pixoo] Auto-off safety"
triggers:
  - trigger: state
    entity_id: light.divoom_pixoo_64_display_light
    to: "on"
    for:
      minutes: 5
actions:
  - action: light.turn_off
    target:
      entity_id: light.divoom_pixoo_64_display_light
mode: single
```

---

## 16. Reusable Notification Script

Template-based script that accepts customizable fields. Same pattern
as `pixoo_64_notification` but generalized.

```yaml
pixoo_notification:
  alias: Pixoo Notification
  mode: queued
  fields:
    image_path:
      description: Optional 64x64 image path
    header_text:
      description: Header text (top)
    body_text:
      description: Body text (middle)
    footer_text:
      description: Footer text (bottom)
    timeout:
      description: Display duration (seconds)
      default: 15
  sequence:
    - service: light.turn_on
      target:
        entity_id: light.divoom_pixoo_64_display_light

    - service: divoom_pixoo.show_message
      data:
        page_data:
          page_type: components
          components: >-
            {% set c = [] %}
            {% if image_path is defined %}
              {% set c = c + [{
                "type": "image",
                "align": "center",
                "image_path": image_path,
                "position": [0, 0]
              }] %}
            {% endif %}
            {% if header_text is defined %}
              {% set c = c + [{
                "type": "text",
                "content": header_text,
                "font": "gicko",
                "color": "yellow",
                "align": "center",
                "position": [32, 4]
              }] %}
            {% endif %}
            {% if body_text is defined %}
              {% set c = c + [{
                "type": "text",
                "content": body_text,
                "font": "eleven_pix",
                "color": "white",
                "align": "center",
                "position": [32, 28]
              }] %}
            {% endif %}
            {% if footer_text is defined %}
              {% set c = c + [{
                "type": "text",
                "content": footer_text,
                "font": "pico_8",
                "color": [150, 150, 150],
                "align": "center",
                "position": [32, 50]
              }] %}
            {% endif %}
            {{ c }}
        duration: "{{ timeout }}"
      target:
        entity_id: sensor.divoom_pixoo_64_display_current_page

    - delay:
        seconds: "{{ timeout | int }}"

    - service: light.turn_off
      target:
        entity_id: light.divoom_pixoo_64_display_light
```

**Usage in automation:**

```yaml
actions:
  - service: script.pixoo_notification
    data:
      header_text: "Alert"
      body_text: "Motion detected"
      timeout: 20
```

---

## 17. Conditional Content via Template [inline]

Change display content based on entity state.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: text
        content: >-
          {% if is_state('binary_sensor.example', 'on') %}
            Active
          {% else %}
            Inactive
          {% endif %}
        font: gicko
        color: "{{ [0,255,0] if is_state('binary_sensor.example', 'on') else [255,0,0] }}"
        align: center
        position: [32, 24]
      - type: text
        content: "{{ states('sensor.example_value') }}"
        font: eleven_pix
        color: white
        align: center
        position: [32, 44]
  duration: 10
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 18. Dynamic Image via Template [inline]

Swap image based on sensor state. Warn if images exceed 64x64.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: image
        align: center
        image_path: >-
          {% if is_state('binary_sensor.example', 'on') %}
            /config/www/images/on_64x64.png
          {% else %}
            /config/www/images/off_64x64.png
          {% endif %}
        position: [0, 0]
      - type: text
        content: "{{ states('sensor.example_value') }}"
        font: pico_8
        color: white
        align: center
        position: [32, 56]
  duration: 12
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 19. Variables in Config (config-only)

Use `variables` in the config flow `pages_data` to reuse template
expressions. Not available in service calls.

```yaml
page_type: components
variables:
  value: "{{ states('sensor.example_sensor') }}"
  color: "{{ [0,255,0] if states('sensor.example_sensor')|int > 50 else [255,0,0] }}"
components:
  - type: text
    content: "{{ value }}"
    color: "{{ color }}"
    font: gicko
    align: center
    position: [32, 24]
  - type: text
    content: "Units"
    font: pico_8
    color: [150, 150, 150]
    align: center
    position: [32, 44]
```

---

## 20. Restart Device [⚠]

Restart the Pixoo. Has a brief delay. Use sparingly.

```yaml
service: divoom_pixoo.restart
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```

---

## 21. DND-Gated Notification [wake]

Gate non-critical notifications so they only fire when DND is off.

```yaml
alias: "[pixoo] Gated notification"
triggers:
  - trigger: state
    entity_id: binary_sensor.example
    to: "on"
conditions:
  - condition: state
    entity_id: input_boolean.do_not_disturb
    state: "off"
actions:
  - service: light.turn_on
    target:
      entity_id: light.divoom_pixoo_64_display_light

  - service: divoom_pixoo.show_message
    data:
      page_data:
        page_type: components
        components:
          - type: rectangle
            color: white
            filled: false
            position: [0, 0]
            size: [64, 64]
          - type: text
            content: "Event"
            font: gicko
            color: yellow
            align: center
            position: [32, 16]
          - type: text
            content: "Detected"
            font: eleven_pix
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
mode: single
```

---

## 22. Templatable Component (Advanced) [inline]

Generate components dynamically with a Jinja2 template.
Useful for data-driven displays where the number of elements
is unknown at config time.

```yaml
service: divoom_pixoo.show_message
data:
  page_data:
    page_type: components
    components:
      - type: templatable
        template: >-
          {% set items = ["A", "B", "C"] %}
          {% set c = [] %}
          {% for item in items %}
            {% set c = c + [{
              "type": "text",
              "content": item,
              "font": "pico_8",
              "color": "white",
              "position": [32, loop.index0 * 20 + 4]
            }] %}
          {% endfor %}
          {{ c }}
  duration: 10
target:
  entity_id: sensor.divoom_pixoo_64_display_current_page
```
