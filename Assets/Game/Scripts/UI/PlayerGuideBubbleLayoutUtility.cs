using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerGuideBubbleSizeType
{
    Medium,
    Small
}

public readonly struct PlayerGuideBubblePlacement
{
    public PlayerGuideBubblePlacement(PlayerGuideBubbleSizeType sizeType, Vector2 anchoredPosition)
    {
        SizeType = sizeType;
        AnchoredPosition = anchoredPosition;
    }

    public PlayerGuideBubbleSizeType SizeType { get; }
    public Vector2 AnchoredPosition { get; }
}

public static class PlayerGuideBubbleLayoutUtility
{
    private readonly struct Circle
    {
        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector2 Center { get; }
        public float Radius { get; }
    }

    public static bool TryGenerateLayout(
        PlayerGuideBubbleLayoutConfig config,
        Vector2 mainBubblePosition,
        Vector2 layoutHalfSize,
        out List<PlayerGuideBubblePlacement> placements,
        out string errorMessage)
    {
        placements = new List<PlayerGuideBubblePlacement>();
        errorMessage = null;

        if (config == null)
        {
            errorMessage = "Bubble layout config is missing.";
            return false;
        }

        if (config.TotalOtherBubbleCount <= 0)
            return true;

        if (!TryValidateOrbitRange(
                config.mediumMinOrbitRadius,
                config.mediumMaxOrbitRadius,
                "medium",
                out errorMessage))
            return false;

        if (!TryValidateOrbitRange(
                config.smallMinOrbitRadius,
                config.smallMaxOrbitRadius,
                "small",
                out errorMessage))
            return false;

        var rng = config.randomSeed == 0
            ? new System.Random()
            : new System.Random(config.randomSeed);

        var mainRadius = GetRadius(config.mainBubbleSize);
        var mainCircle = new Circle(mainBubblePosition, mainRadius);
        var placedCircles = new List<Circle>(config.TotalOtherBubbleCount);

        if (!TryPlaceBubblesOfType(
                config,
                mainCircle,
                placedCircles,
                layoutHalfSize,
                PlayerGuideBubbleSizeType.Medium,
                config.mediumCount,
                rng,
                placements,
                out errorMessage))
            return false;

        if (!TryPlaceBubblesOfType(
                config,
                mainCircle,
                placedCircles,
                layoutHalfSize,
                PlayerGuideBubbleSizeType.Small,
                config.smallCount,
                rng,
                placements,
                out errorMessage))
            return false;

        return true;
    }

    public static Vector2 GetBubbleSize(PlayerGuideBubbleLayoutConfig config, PlayerGuideBubbleSizeType sizeType)
    {
        return sizeType switch
        {
            PlayerGuideBubbleSizeType.Medium => config.mediumBubbleSize,
            PlayerGuideBubbleSizeType.Small => config.smallBubbleSize,
            _ => throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null)
        };
    }

    public static string GetBubbleNamePrefix(PlayerGuideBubbleLayoutConfig config, PlayerGuideBubbleSizeType sizeType)
    {
        return sizeType switch
        {
            PlayerGuideBubbleSizeType.Medium => config.mediumBubbleNamePrefix,
            PlayerGuideBubbleSizeType.Small => config.smallBubbleNamePrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null)
        };
    }

    private static bool TryPlaceBubblesOfType(
        PlayerGuideBubbleLayoutConfig config,
        Circle mainCircle,
        List<Circle> placedCircles,
        Vector2 layoutHalfSize,
        PlayerGuideBubbleSizeType sizeType,
        int count,
        System.Random rng,
        List<PlayerGuideBubblePlacement> placements,
        out string errorMessage)
    {
        errorMessage = null;

        for (var index = 0; index < count; index++)
        {
            var bubbleSize = GetBubbleSize(config, sizeType);
            if (!TryPlaceBubble(
                    config,
                    sizeType,
                    mainCircle,
                    placedCircles,
                    bubbleSize,
                    layoutHalfSize,
                    rng,
                    out var position))
            {
                errorMessage =
                    $"Failed to place all {sizeType} bubbles without overlap. " +
                    $"Placed {placements.Count}/{config.TotalOtherBubbleCount}. " +
                    "Try widening orbit radii, reducing bubble counts, or lowering bubblePadding.";
                return false;
            }

            placements.Add(new PlayerGuideBubblePlacement(sizeType, position));
        }

        return true;
    }

    private static bool TryPlaceBubble(
        PlayerGuideBubbleLayoutConfig config,
        PlayerGuideBubbleSizeType sizeType,
        Circle mainCircle,
        List<Circle> placedCircles,
        Vector2 bubbleSize,
        Vector2 layoutHalfSize,
        System.Random rng,
        out Vector2 position)
    {
        GetOrbitRange(config, sizeType, out var minOrbitRadius, out var maxOrbitRadius);

        var bubbleRadius = GetRadius(bubbleSize);
        var minDistanceFromMain = mainCircle.Radius + bubbleRadius + config.bubblePadding;
        var effectiveMinOrbit = Mathf.Max(minOrbitRadius, minDistanceFromMain);
        var effectiveMaxOrbit = Mathf.Max(effectiveMinOrbit, maxOrbitRadius);

        for (var attempt = 0; attempt < config.placementAttemptsPerBubble; attempt++)
        {
            var candidate = SampleEllipticalOrbitPosition(
                mainCircle.Center,
                effectiveMinOrbit,
                effectiveMaxOrbit,
                layoutHalfSize,
                rng);
            var candidateCircle = new Circle(candidate, bubbleRadius);

            if (CirclesOverlap(candidateCircle, mainCircle, config.bubblePadding))
                continue;

            var overlapsPlaced = false;
            for (var index = 0; index < placedCircles.Count; index++)
            {
                if (!CirclesOverlap(candidateCircle, placedCircles[index], config.bubblePadding))
                    continue;

                overlapsPlaced = true;
                break;
            }

            if (overlapsPlaced)
                continue;

            if (!IsInsideBounds(candidateCircle, parentHalfSize: layoutHalfSize, config.boundsPadding))
                continue;

            position = candidate;
            placedCircles.Add(candidateCircle);
            return true;
        }

        position = default;
        return false;
    }

    private static Vector2 SampleEllipticalOrbitPosition(
        Vector2 center,
        float minOrbitRadius,
        float maxOrbitRadius,
        Vector2 layoutHalfSize,
        System.Random rng)
    {
        var angle = (float)(rng.NextDouble() * Math.PI * 2d);
        var orbitBlend = (float)rng.NextDouble();
        var minAxes = GetEllipseSemiAxes(minOrbitRadius, layoutHalfSize);
        var maxAxes = GetEllipseSemiAxes(maxOrbitRadius, layoutHalfSize);
        var axes = Vector2.Lerp(minAxes, maxAxes, orbitBlend);
        return center + new Vector2(Mathf.Cos(angle) * axes.x, Mathf.Sin(angle) * axes.y);
    }

    private static Vector2 GetEllipseSemiAxes(float orbitRadius, Vector2 layoutHalfSize)
    {
        var dominantAxis = Mathf.Max(layoutHalfSize.x, layoutHalfSize.y);
        if (dominantAxis <= Mathf.Epsilon)
            return new Vector2(orbitRadius, orbitRadius);

        return orbitRadius * (layoutHalfSize / dominantAxis);
    }

    private static void GetOrbitRange(
        PlayerGuideBubbleLayoutConfig config,
        PlayerGuideBubbleSizeType sizeType,
        out float minOrbitRadius,
        out float maxOrbitRadius)
    {
        switch (sizeType)
        {
            case PlayerGuideBubbleSizeType.Medium:
                minOrbitRadius = config.mediumMinOrbitRadius;
                maxOrbitRadius = config.mediumMaxOrbitRadius;
                break;
            case PlayerGuideBubbleSizeType.Small:
                minOrbitRadius = config.smallMinOrbitRadius;
                maxOrbitRadius = config.smallMaxOrbitRadius;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null);
        }
    }

    private static bool TryValidateOrbitRange(
        float minOrbitRadius,
        float maxOrbitRadius,
        string label,
        out string errorMessage)
    {
        if (maxOrbitRadius < minOrbitRadius)
        {
            errorMessage = $"{label} max orbit radius must be greater than or equal to min orbit radius.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool IsInsideBounds(Circle circle, Vector2 parentHalfSize, float boundsPadding)
    {
        var maxX = Mathf.Max(0f, parentHalfSize.x - circle.Radius - boundsPadding);
        var maxY = Mathf.Max(0f, parentHalfSize.y - circle.Radius - boundsPadding);
        return Mathf.Abs(circle.Center.x) <= maxX && Mathf.Abs(circle.Center.y) <= maxY;
    }

    private static float GetRadius(Vector2 size)
    {
        return Mathf.Max(size.x, size.y) * 0.5f;
    }

    private static bool CirclesOverlap(Circle a, Circle b, float padding)
    {
        var requiredDistance = a.Radius + b.Radius + padding;
        return Vector2.SqrMagnitude(a.Center - b.Center) < requiredDistance * requiredDistance;
    }
}
