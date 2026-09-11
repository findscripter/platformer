using UnityEngine;

/// <summary>
/// 按镜头一屏（约 14 单位）排景，而不是把山、莲、前景叶各自随机撒点。
/// A 屏结构对齐参考图：左雾丘、中莲与光柱、右前景叶。
/// B 屏结构对齐参考图：白坡托住平台、水线芦苇、右上角月。
/// </summary>
public partial class ProceduralLevelGenerator
{
    private const float ScreenWidth = 14f;

    private void ComposeDesignedScenery(Transform parent)
    {
        if (platforms.Count == 0)
            return;

        int screen = 0;
        for (float x0 = RegionMinX; x0 < RegionMaxX - 1f; x0 += ScreenWidth)
        {
            if (isSceneA)
                ComposeScreenA(parent, x0, screen);
            else
                ComposeScreenB(parent, x0, screen);
            screen++;
        }

        PlacePlatformSupports(parent);
    }

    private void ComposeScreenA(Transform parent, float x0, int screen)
    {
        int motif = screen % 3;
        string tag = "S" + screen + "-";

        PlaceProp(parent, tag + "HillL", "S01/15.png", 1.75f, x0 + 1.55f, 0.88f, 0.78f,
            GameLayers.MidgroundSorting, -21, false);
        PlaceProp(parent, tag + "HillM", "S01/19.png", 1.08f, x0 + 3.55f, 0.54f, 0.82f,
            GameLayers.MidgroundSorting, -20, false);

        if (motif == 0)
        {
            PlaceProp(parent, tag + "HillR", "S01/15.png", 1.35f, x0 + 6.2f, 0.68f, 0.55f,
                GameLayers.MidgroundSorting, -21, true);
            PlaceProp(parent, tag + "Lotus", "S01/17.png", 1.18f, x0 + 7.9f, 0.74f, 0.7f,
                GameLayers.MidgroundSorting, -6, false);
            PlaceProp(parent, tag + "Fly", "S01/23.png", 0.5f, x0 + 9.45f, 1.82f, 0.88f,
                GameLayers.MidgroundSorting, -3, false);
            PlaceProp(parent, tag + "Leaf", "S01/21.png", 4.55f, x0 + 10.9f, 2.28f, 0.88f,
                GameLayers.ForegroundSorting, 4, false);
        }
        else if (motif == 1)
        {
            PlaceProp(parent, tag + "LotusA", "S01/17.png", 1.05f, x0 + 5.4f, 0.62f, 0.68f,
                GameLayers.MidgroundSorting, -6, false);
            PlaceProp(parent, tag + "LotusB", "S01/17.png", 0.92f, x0 + 8.6f, 0.55f, 0.6f,
                GameLayers.MidgroundSorting, -6, true);
            PlaceProp(parent, tag + "Fly", "S01/23.png", 0.46f, x0 + 6.3f, 1.55f, 0.8f,
                GameLayers.MidgroundSorting, -3, true);
            PlaceProp(parent, tag + "HillFar", "S01/19.png", 0.95f, x0 + 11.4f, 0.48f, 0.5f,
                GameLayers.MidgroundSorting, -22, false);
        }
        else
        {
            PlaceProp(parent, tag + "LeafL", "S01/21.png", 3.4f, x0 + 1.1f, 1.7f, 0.55f,
                GameLayers.MidgroundSorting, -19, true);
            PlaceProp(parent, tag + "Lotus", "S01/17.png", 1.12f, x0 + 7.2f, 0.7f, 0.66f,
                GameLayers.MidgroundSorting, -6, false);
            PlaceProp(parent, tag + "HillR", "S01/15.png", 1.55f, x0 + 11.8f, 0.78f, 0.62f,
                GameLayers.MidgroundSorting, -21, false);
        }
    }

    private void ComposeScreenB(Transform parent, float x0, int screen)
    {
        int motif = screen % 3;
        string tag = "S" + screen + "-";

        PlaceProp(parent, tag + "ReedL", "S02/11.png", 0.82f, x0 + 2.1f, 0.4f, 0.72f,
            GameLayers.MidgroundSorting, -8, true);
        PlaceProp(parent, tag + "Grass", "S02/21.png", 1.12f, x0 + 6.0f, 0.5f, 0.8f,
            GameLayers.MidgroundSorting, -8, false);
        PlaceProp(parent, tag + "ReedR", "S02/11.png", 0.7f, x0 + 10.4f, 0.36f, 0.6f,
            GameLayers.MidgroundSorting, -8, false);

        if (motif == 0)
        {
            PlaceProp(parent, tag + "Slope", "S02/22.png", 4.55f, x0 + 6.4f, 3.05f, 0.95f,
                GameLayers.MidgroundSorting, -22, false);
            PlaceProp(parent, tag + "Moon", "S02/3.png", 0.58f, x0 + 12.15f, 7.05f, 0.9f,
                GameLayers.MidgroundSorting, -14, false);
            PlaceProp(parent, tag + "Lotus", "S01/17.png", 0.82f, x0 + 11.2f, 0.48f, 0.55f,
                GameLayers.MidgroundSorting, -6, false);
        }
        else if (motif == 1)
        {
            PlaceProp(parent, tag + "Slope", "S02/22.png", 3.8f, x0 + 8.2f, 2.55f, 0.88f,
                GameLayers.MidgroundSorting, -22, true);
            PlaceProp(parent, tag + "Lotus", "S01/17.png", 0.9f, x0 + 4.4f, 0.52f, 0.58f,
                GameLayers.MidgroundSorting, -6, false);
        }
        else
        {
            PlaceProp(parent, tag + "Slope", "S02/22.png", 4.2f, x0 + 4.6f, 2.85f, 0.92f,
                GameLayers.MidgroundSorting, -22, false);
            PlaceProp(parent, tag + "Moon", "S02/3.png", 0.5f, x0 + 2.6f, 6.85f, 0.78f,
                GameLayers.MidgroundSorting, -14, false);
            PlaceProp(parent, tag + "Ribbon", "S02/5.png", 0.8f, x0 + 11.5f, 6.4f, 0.35f,
                GameLayers.MidgroundSorting, -15, false);
        }
    }

    private void PlacePlatformSupports(Transform parent)
    {
        string beam = isSceneA ? "S01/8.png" : "S02/5.png";
        string pillar = "S01/9.png";

        for (int i = 0; i < platforms.Count; i++)
        {
            Platform p = platforms[i];
            if (p.width < 2.6f)
                continue;

            int beams = p.width >= 4.2f ? 2 : 1;
            for (int b = 0; b < beams; b++)
            {
                float t = beams == 1 ? 0f : (b == 0 ? -0.28f : 0.28f);
                float x = p.center.x + t * p.width;
                float height = Mathf.Clamp(p.top * 0.92f, 1.5f, 5.2f);
                PlaceProp(parent, "Beam-" + i + "-" + b, beam, height, x, height * 0.46f,
                    isSceneA ? 0.38f : 0.22f, GameLayers.MidgroundSorting, -17, false);
            }

            if (p.width >= 3.6f)
            {
                float height = Mathf.Clamp(p.top, 1.4f, 5.8f);
                PlaceProp(parent, "Pillar-" + i, pillar, height, p.center.x, height * 0.48f,
                    0.5f, GameLayers.MidgroundSorting, -18, false);
            }
        }
    }

    private static void PlaceProp(Transform parent, string name, string file, float height, float x, float y,
        float alpha, string sortingLayer, int order, bool flip)
    {
        SpriteRenderer sr = DreamremainsLevelBootstrap.AddArt(
            parent, name, "Assets/Game/Art/Scenes/" + file, height, new Vector2(x, y), sortingLayer, order);
        if (sr == null)
            return;
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        sr.flipX = flip;
    }
}
