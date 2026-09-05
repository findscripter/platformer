# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6000.5.0f1
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP) 17.5.0
- **Physics**: Unity Physics2D (2D platformer)

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Windows primary)
- **Input Methods**: Keyboard/Mouse, Gamepad
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: Partial (via Unity Input System)
- **Touch Support**: None
- **Platform Notes**: 2D platformer — all UI should support both keyboard navigation and gamepad d-pad. No hover-only interactions.

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`)
- **Public fields/properties**: PascalCase (e.g., `MoveSpeed`)
- **Private fields**: _camelCase (e.g., `_moveSpeed`)
- **Methods**: PascalCase (e.g., `TakeDamage()`)
- **Events/Signals**: PascalCase verb phrase (e.g., `GameEvents.DialogueStarted`)
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `MainMenu.unity`, `PlayerPrefab.prefab`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE

## Performance Budgets

- **Target Framerate**: 60 fps
- **Frame Budget**: 16.6ms
- **Draw Calls**: ≤ 100 (2D URP)
- **Memory Ceiling**: 512MB

## Testing

- **Framework**: Unity Test Framework (NUnit) — `com.unity.test-framework` 1.7.0
- **Minimum Coverage**: Core gameplay systems (state machine, save/load, event bus)
- **Required Tests**: State machine transitions, SaveSystem serialize/deserialize, EventCenter subscribe/publish

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- `Object.FindObjectsSortMode` — obsolete in Unity 6; use `FindFirstObjectByType<T>()` or `FindObjectsByType<T>(FindObjectsInactive.Include)`
- `text.enableWordWrapping` — obsolete in Unity 6 TextMeshPro; use `text.textWrappingMode`
- `[Header("text with spaces")]` in ShaderLab — use single-word identifiers only
- `[Tooltip]` in shader Properties blocks — C# only

## Allowed Libraries / Addons

<!-- Only add when actively integrating — do not add speculatively -->
- `com.unity.render-pipelines.universal` 17.5.0 — URP rendering
- `com.unity.inputsystem` 1.19.0 — New Input System
- `com.unity.cinemachine` 3.1.6 — camera
- `com.unity.ugui` 2.5.0 — UI + TextMeshPro
- `com.unity.feature.2d` 2.0.2 — 2D toolset (Tilemap, Sprite Shape, etc.)
- `com.unity.test-framework` 1.7.0 — NUnit testing
- `com.unity.ai.inference` 2.6.1 — Sentis AI inference

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP/HDRP materials)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS, UGUI Canvas, runtime UI)
- **Additional Specialists**: unity-dots-specialist (ECS, Jobs system, Burst compiler), unity-addressables-specialist (asset loading, memory management, content catalogs)
- **Routing Notes**: Invoke primary for architecture and general C# code review. Invoke shader specialist for rendering and visual effects. Invoke UI specialist for all interface implementation. Invoke Addressables specialist for asset management systems.

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
