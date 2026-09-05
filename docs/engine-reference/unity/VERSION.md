# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6000.5.0f1 |
| **Project Pinned** | 2026-08-17 |
| **LLM Knowledge Cutoff** | May 2025 |
| **Risk Level** | LOW — Unity 6000.x is within LLM training data |
| **Last Docs Verified** | 2026-08-17 |

## Post-Cutoff Version Timeline

| Version | Release (approx) | Within Training? |
|---------|-----------------|-----------------|
| Unity 6000.0.x (Unity 6) | Oct 2024 | ✅ Yes |
| Unity 6000.5.0f1 | 2025 | ✅ Yes |

## Key Unity 6 APIs in This Project

These APIs are confirmed current for Unity 6000.5.0f1 based on the existing codebase:

### Object Finding (replaced legacy FindObjectOfType)
```csharp
// Use these — correct for Unity 6
Object.FindFirstObjectByType<T>();
Object.FindObjectsByType<T>(FindObjectsInactive.Include);
// Do NOT use FindObjectsSortMode parameter — obsolete
```

### TextMeshPro
```csharp
// Use this — correct for Unity 6
text.textWrappingMode = TextWrappingModes.NoWrap;
text.textWrappingMode = TextWrappingModes.Normal;
// Do NOT use enableWordWrapping — obsolete
```

### ShaderLab Headers
```hlsl
// Single-word identifier only — correct
[Header(VerticalFade)]
// Spaces or quotes in Header() — causes parse errors
```

## Active Packages (from manifest.json)

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.render-pipelines.universal` | 17.5.0 | URP rendering |
| `com.unity.inputsystem` | 1.19.0 | New Input System |
| `com.unity.cinemachine` | 3.1.6 | Camera system |
| `com.unity.ugui` | 2.5.0 | UI + TextMeshPro |
| `com.unity.feature.2d` | 2.0.2 | 2D tools (Tilemap, Sprite Shape) |
| `com.unity.test-framework` | 1.7.0 | NUnit testing |
| `com.unity.ai.inference` | 2.6.1 | Sentis AI inference |

## Notes

This engine version is within the LLM's training data. The existing CLAUDE.md
already captures the key Unity 6 API requirements for this project. Run
`/setup-engine refresh` at any time to update this reference.
