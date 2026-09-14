public static class DreamremainsLevelData
{
    public struct PlatformSpec
    {
        public string Id;
        public float X;
        public float Y;
        public float W;
        public string Type;
        public bool Raise;
        public float MoveX;
        public float MoveX2; // Optional horizontal destination, zero retains legacy vertical movement.
        public float MoveY1;
        public float MoveY2;
    }

    public struct AbyssSpec
    {
        public string Id;
        public float X1;
        public float X2;
    }

    public struct SpikeSpec
    {
        public string Id;
        public float X;
        public float Y;
        public float W;
    }

    public struct PointSpec
    {
        public string Id;
        public string Kind;
        public float X;
        public float Y;
        public int Slot;
        public float PatrolX1;
        public float PatrolX2;
    }

    public struct FoldSpec
    {
        public string EnterId;
        public string ExitId;
    }

    public struct RectSpec
    {
        public string Id;
        public float X;
        public float Y;
        public float W;
        public float H;
    }

    public struct RoomSpec
    {
        public string RtId;
        public int ZoneIndex;
        public float X1;
        public float X2;
    }

    public struct SceneSpec
    {
        public PlatformSpec[] Platforms;
        public AbyssSpec[] Abysses;
        public SpikeSpec[] Spikes;
        public PointSpec[] Points;
        public FoldSpec[] Folds;
        public RectSpec[] AirWalks;
        public RoomSpec[] Rooms;
        public int HiddenSlot;
        public string[] HiddenIds;
        public int HiddenSlot2;
        public string[] HiddenIds2;
    }

    public static readonly SceneSpec SceneA = new SceneSpec
    {
        Platforms = new[]
        {
            P("PL-A01", 0.3f, 5.7f, 3.9f, "normal"),
            P("PL-A02", 4.2f, 5.0f, 1.5f, "normal"),
            P("PL-A03", 6.0f, 2.0f, 3.7f, "normal"),
            P("PL-A04", 6.0f, 6.0f, 3.2f, "normal"),
            P("PL-A04b", 9.2f, 5.1f, 1.5f, "normal"),
            P("PL-A05", 10.7f, 4.2f, 2.2f, "normal"),
            P("PL-A06", 12.7f, 4.8f, 1.8f, "normal"),
            M("MP-A01", 15.1f, 4.1f, 1.5f, 15.85f, 2.2f, 5.1f),
            R("PL-A07", 17.1f, 1.8f, 3.4f),
            P("PL-A08", 17.1f, 5.9f, 3.5f, "normal"),
            P("PL-A09", 21.0f, 3.5f, 2.4f, "normal"),
            P("PL-A10", 22.0f, 1.2f, 2.2f, "normal"),
            P("PL-A11", 24.3f, 4.8f, 2.2f, "normal"),
            P("PL-A12", 26.7f, 5.3f, 2.0f, "normal"),
            P("PL-A13", 28.8f, 4.5f, 1.6f, "normal"),
            P("FP-A01a", 12.3f, 5.5f, 0.9f, "fading"),
            P("FP-A01b", 13.5f, 5.1f, 0.9f, "fading"),
            P("FP-A01c", 14.7f, 5.5f, 0.9f, "fading"),
            P("TH-A01", 12.7f, 0.8f, 1.8f, "hidden"),
            P("TH-A02", 15.0f, 0.8f, 1.6f, "hidden"),
            P("TH-A03", 22.1f, 6.5f, 1.7f, "hidden"),
            P("TH-A04", 25.0f, 6.5f, 1.6f, "hidden")
        },
        Abysses = new[]
        {
            new AbyssSpec { Id = "H-A01", X1 = 5.1f, X2 = 8.8f },
            new AbyssSpec { Id = "H-A02", X1 = 13.5f, X2 = 18.0f },
            new AbyssSpec { Id = "H-A03", X1 = 22.6f, X2 = 25.7f }
        },
        Spikes = new[]
        {
            new SpikeSpec { Id = "SG-A01", X = 7.2f, Y = 1.85f, W = 1.2f },
            new SpikeSpec { Id = "SG-A02", X = 15.2f, Y = 5.65f, W = 2.0f },
            new SpikeSpec { Id = "SG-A03", X = 23.2f, Y = 5.65f, W = 1.3f }
        },
        Points = new[]
        {
            N("SP-A01", "spawn", 0.9f, 5.35f),
            N("T1", "tarot", 11.3f, 3.85f, 0),
            N("CP-A01", "cp", 13.7f, 4.5f),
            N("T2", "tarot", 21.9f, 3.15f, 1),
            N("CP-A02", "cp", 27.5f, 5.0f),
            N("TRANS-A-B", "trans", 29.7f, 4.1f),
            E("EN-A01", 3.0f, 5.35f, 1.8f, 3.8f),
            E("EN-A02", 8.0f, 5.65f, 6.5f, 9.0f),
            E("EN-A03", 18.3f, 1.45f, 17.5f, 19.8f),
            E("EN-A04", 19.2f, 5.55f, 17.8f, 20.0f),
            E("EN-A05", 25.0f, 4.45f, 24.5f, 25.9f),
            N("IT-A01", "item", 14.7f, 0.45f),
            N("IT-A02", "item", 25.2f, 6.15f),
            N("TRG-A01", "trg", 12.1f, 3.95f),
            N("TRG-A02", "trg", 22.5f, 3.55f),
            N("D1", "door", 9.7f, 1.45f),
            N("D2", "door", 20.7f, 5.45f),
            N("FE-A03", "fold", 12.6f, 2.6f),
            N("FA-A03", "fold", 19.1f, 3.0f),
            N("FE-A04", "fold", 19.8f, 3.8f),
            N("FA-A04", "fold", 21.5f, 3.1f),
            N("FE-A05", "fold", 26.1f, 4.0f),
            N("FA-A05", "fold", 29.1f, 4.0f)
        },
        Folds = new[]
        {
            new FoldSpec { EnterId = "FE-A03", ExitId = "FA-A03" },
            new FoldSpec { EnterId = "FE-A04", ExitId = "FA-A04" },
            new FoldSpec { EnterId = "FE-A05", ExitId = "FA-A05" }
        },
        AirWalks = new[]
        {
            new RectSpec { Id = "AW-A01", X = 12.3f, Y = 1.0f, W = 3.8f, H = 1.5f }
        },
        Rooms = new[]
        {
            new RoomSpec { RtId = "RT-01", ZoneIndex = 0, X1 = 5.7f, X2 = 12.3f },
            new RoomSpec { RtId = "RT-02", ZoneIndex = 1, X1 = 12.3f, X2 = 19.3f },
            new RoomSpec { RtId = "RT-03", ZoneIndex = 2, X1 = 19.3f, X2 = 25.9f },
            new RoomSpec { RtId = "RT-04", ZoneIndex = 3, X1 = 25.9f, X2 = 30f }
        },
        HiddenSlot = 0,
        HiddenIds = new[] { "TH-A01", "TH-A02", "IT-A01" },
        HiddenSlot2 = 1,
        HiddenIds2 = new[] { "TH-A03", "TH-A04", "IT-A02" }
    };

    public static readonly SceneSpec SceneB = new SceneSpec
    {
        Platforms = new[]
        {
            P("PL-B01", 0.3f, 5.7f, 3.3f, "normal"),
            P("PL-B02", 4.0f, 4.9f, 1.6f, "normal"),
            P("PL-B03", 6.1f, 4.1f, 1.8f, "normal"),
            P("PL-B04", 8.5f, 3.1f, 1.5f, "normal"),
            P("PL-B04b", 10.0f, 2.55f, 1.1f, "normal"),
            P("PL-B05", 11.1f, 2.0f, 2.0f, "normal"),
            P("PL-B06", 6.5f, 6.0f, 4.8f, "normal"),
            M("MP-B01", 12.8f, 5.3f, 1.6f, 13.6f, 2.6f, 5.8f),
            P("PL-B08", 14.6f, 3.3f, 3.0f, "normal"),
            P("PL-B09", 17.2f, 5.1f, 1.8f, "normal"),
            P("PL-B10", 19.5f, 4.2f, 2.2f, "normal"),
            M("MP-B02", 22.0f, 3.2f, 1.7f, 22.85f, 2.0f, 5.0f),
            P("PL-B12", 24.1f, 2.2f, 2.2f, "normal"),
            P("PL-B13", 26.5f, 5.0f, 1.4f, "normal"),
            P("PL-B13b", 27.9f, 4.15f, 1.2f, "normal"),
            P("PL-B13c", 29.1f, 3.3f, 1.2f, "normal"),
            P("PL-B13d", 30.3f, 2.45f, 1.2f, "normal"),
            R("PL-B14", 31.5f, 1.5f, 1.6f),
            P("FP-B01a", 13.8f, 4.8f, 0.9f, "fading"),
            P("FP-B01b", 15.1f, 4.3f, 0.9f, "fading"),
            P("FP-B01c", 16.4f, 4.8f, 0.9f, "fading"),
            P("FP-B02a", 24.4f, 3.9f, 0.9f, "fading"),
            P("FP-B02b", 25.7f, 3.4f, 0.9f, "fading"),
            P("FP-B02c", 27.0f, 3.9f, 0.9f, "fading"),
            P("TH-B01", 13.8f, 0.8f, 1.7f, "hidden"),
            P("TH-B02", 16.0f, 0.8f, 1.7f, "hidden"),
            P("TH-B03", 23.2f, 6.4f, 2.0f, "hidden"),
            P("TH-B04", 25.7f, 6.4f, 1.7f, "hidden")
        },
        Abysses = new[]
        {
            new AbyssSpec { Id = "H-B01", X1 = 3.8f, X2 = 7.8f },
            new AbyssSpec { Id = "H-B02", X1 = 12.2f, X2 = 17.0f },
            new AbyssSpec { Id = "H-B03", X1 = 20.3f, X2 = 24.0f },
            new AbyssSpec { Id = "H-B04", X1 = 27.0f, X2 = 29.0f }
        },
        Spikes = new[]
        {
            new SpikeSpec { Id = "SG-B01", X = 8.9f, Y = 5.65f, W = 1.4f },
            new SpikeSpec { Id = "SG-B02", X = 20.7f, Y = 5.65f, W = 1.4f },
            new SpikeSpec { Id = "SG-B03", X = 27.4f, Y = 5.65f, W = 1.0f }
        },
        Points = new[]
        {
            N("IN-B01", "spawn", 0.9f, 5.35f),
            N("CP-B01", "cp", 4.8f, 4.55f),
            N("T3", "tarot", 11.8f, 1.65f, 2),
            N("CP-B02", "cp", 18.4f, 4.75f),
            N("T4", "tarot", 22.8f, 2.85f, 3),
            N("CP-B03", "cp", 27.2f, 4.65f),
            N("END", "end", 32.2f, 1.1f),
            E("EN-B01", 3.0f, 5.35f, 1.8f, 3.3f),
            E("EN-B02", 7.8f, 5.65f, 6.9f, 10.0f),
            E("EN-B03", 15.3f, 2.95f, 14.8f, 16.8f),
            E("EN-B04", 21.2f, 3.85f, 19.8f, 21.5f),
            E("EN-B05", 24.9f, 1.85f, 24.2f, 25.8f),
            // 离开 CP-B03 复活点，落在既有 PL-B13b；不改任何平台点位。
            E("EN-B06", 28.5f, 3.8f, 28.2f, 28.8f),
            N("IT-B01", "item", 15.9f, 0.45f),
            N("IT-B02", "item", 26.2f, 6.05f),
            N("TRG-B01", "trg", 12.9f, 3.05f),
            N("TRG-B02", "trg", 23.4f, 2.7f),
            N("D3", "door", 20.3f, 5.5f),
            N("FE-B02", "fold", 5.3f, 3.6f),
            N("FA-B02", "fold", 11.5f, 1.65f),
            N("FE-B03", "fold", 12.2f, 5.9f),
            N("FA-B03", "fold", 18.1f, 4.75f),
            N("FE-B04", "fold", 19.0f, 3.5f),
            N("FA-B04", "fold", 22.4f, 2.85f),
            N("FE-B05", "fold", 25.3f, 4.5f),
            N("FA-B05", "fold", 27.5f, 4.65f)
        },
        Folds = new[]
        {
            new FoldSpec { EnterId = "FE-B02", ExitId = "FA-B02" },
            new FoldSpec { EnterId = "FE-B03", ExitId = "FA-B03" },
            new FoldSpec { EnterId = "FE-B04", ExitId = "FA-B04" },
            new FoldSpec { EnterId = "FE-B05", ExitId = "FA-B05" }
        },
        AirWalks = new[]
        {
            new RectSpec { Id = "AW-B01", X = 13.7f, Y = 1.1f, W = 3.8f, H = 1.7f }
        },
        Rooms = new[]
        {
            new RoomSpec { RtId = "RT-05", ZoneIndex = 4, X1 = 0.35f, X2 = 5.2f },
            new RoomSpec { RtId = "RT-06", ZoneIndex = 5, X1 = 5.2f, X2 = 12f },
            new RoomSpec { RtId = "RT-07", ZoneIndex = 6, X1 = 12f, X2 = 18.8f },
            new RoomSpec { RtId = "RT-08", ZoneIndex = 7, X1 = 18.8f, X2 = 25.2f },
            new RoomSpec { RtId = "RT-09", ZoneIndex = 8, X1 = 25.2f, X2 = 34f }
        },
        HiddenSlot = 2,
        HiddenIds = new[] { "TH-B01", "TH-B02", "IT-B01" },
        HiddenSlot2 = 3,
        HiddenIds2 = new[] { "TH-B03", "TH-B04", "IT-B02" }
    };

    static DreamremainsLevelData()
    {
        SceneA = DreamremainsExpandedLayout.Expand(SceneA, true);
        SceneB = DreamremainsExpandedLayout.Expand(SceneB, false);
    }

    public static int ZoneAt(float htmlX, bool sceneA)
    {
        var rooms = sceneA ? SceneA.Rooms : SceneB.Rooms;
        for (int i=0;i<rooms.Length;i++)
            if(htmlX>=rooms[i].X1 && htmlX<rooms[i].X2) return rooms[i].ZoneIndex;
        return sceneA ? -1 : 4;
    }

    private static int CompactZoneAt(float htmlX, bool sceneA)
    {
        if (sceneA)
        {
            if (htmlX < 6f) return -1;
            if (htmlX < 12.5f) return 0;
            if (htmlX < 19.5f) return 1;
            if (htmlX < 26f) return 2;
            return 3;
        }

        if (htmlX < 5.2f) return 4;
        if (htmlX < 12f) return 5;
        if (htmlX < 18.8f) return 6;
        if (htmlX < 25.2f) return 7;
        return 8;
    }

    private static PlatformSpec P(string id, float x, float y, float w, string type)
    {
        return new PlatformSpec { Id = id, X = x, Y = y, W = w, Type = type };
    }

    private static PlatformSpec R(string id, float x, float y, float w)
    {
        return new PlatformSpec { Id = id, X = x, Y = y, W = w, Type = "normal", Raise = true };
    }

    private static PlatformSpec M(string id, float x, float y, float w, float mx, float y1, float y2)
    {
        return new PlatformSpec { Id = id, X = x, Y = y, W = w, Type = "moving", MoveX = mx, MoveY1 = y1, MoveY2 = y2 };
    }

    private static PointSpec N(string id, string kind, float x, float y, int slot = 0)
    {
        return new PointSpec { Id = id, Kind = kind, X = x, Y = y, Slot = slot };
    }

    private static PointSpec E(string id, float x, float y, float p1, float p2)
    {
        return new PointSpec { Id = id, Kind = "enemy", X = x, Y = y, PatrolX1 = p1, PatrolX2 = p2 };
    }
}
