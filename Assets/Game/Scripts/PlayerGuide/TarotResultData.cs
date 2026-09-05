using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局塔罗运行状态。字段对齐「关卡实现规则」工作表：
/// drawnCardIds / isReversed / cardEffectsUnlocked / tarotNodesActivated /
/// hiddenPathsUnlocked / activationSequence / foldExitCharge。
/// 开局 22 抽 4，正逆位独立随机；T1–T4 才激活对应槽位。
/// </summary>
[System.Serializable]
public class TarotResultData
{
    public const int SlotCount = 4;
    public const int ZoneCount = 9;

    [SerializeField] private TarotCardData[] drawnCards = new TarotCardData[SlotCount];
    [SerializeField] private bool[] isReversed = new bool[SlotCount];
    [SerializeField] private bool[] cardEffectsUnlocked = new bool[SlotCount];
    [SerializeField] private bool[] tarotNodesActivated = new bool[SlotCount];
    [SerializeField] private bool[] hiddenPathsUnlocked = new bool[SlotCount];
    [SerializeField] private int[] activationSequence = { -1, -1, -1, -1 };
    [SerializeField] private int foldExitCharge;
    [SerializeField] private int nextActivationOrder;
    [SerializeField] private bool[] zoneResolved = new bool[ZoneCount];
    [SerializeField] private int[] zoneEffectMask = new int[ZoneCount];

    public IReadOnlyList<TarotCardData> DrawnCards => drawnCards;
    public int FoldExitCharge => foldExitCharge;

    public TarotCardData MainCard => GetCard(0);

    public TarotResultData()
    {
    }

    public void BeginRun(TarotCardData[] cards, bool[] reversed)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            drawnCards[i] = cards != null && i < cards.Length ? cards[i] : null;
            isReversed[i] = reversed != null && i < reversed.Length && reversed[i];
            cardEffectsUnlocked[i] = false;
            tarotNodesActivated[i] = false;
            hiddenPathsUnlocked[i] = false;
            activationSequence[i] = -1;
        }

        foldExitCharge = 0;
        nextActivationOrder = 0;
        for (int z = 0; z < ZoneCount; z++)
        {
            zoneResolved[z] = false;
            zoneEffectMask[z] = 0;
        }
    }

    public bool IsComplete()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (drawnCards[i] == null)
                return false;
        }

        return true;
    }

    public TarotCardData GetCard(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
            return null;
        return drawnCards[slot];
    }

    public bool IsReversed(int slot)
    {
        return slot >= 0 && slot < SlotCount && isReversed[slot];
    }

    public TarotEffectId GetSlotEffect(int slot)
    {
        return TarotCatalog.GetEffect(GetCard(slot), IsReversed(slot));
    }

    public bool IsNodeActivated(int slot)
    {
        return slot >= 0 && slot < SlotCount && tarotNodesActivated[slot];
    }

    public bool IsHiddenPathUnlocked(int slot)
    {
        return slot >= 0 && slot < SlotCount && hiddenPathsUnlocked[slot];
    }

    public bool AllNodesActivated
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!tarotNodesActivated[i])
                    return false;
            }

            return true;
        }
    }

    public bool HasActiveEffect(TarotEffectId id)
    {
        if (id == TarotEffectId.E00)
            return false;

        TarotEffectChannel channel = TarotEffectChannels.Of(id);
        if (!TarotEffectChannels.Stacks(channel) &&
            (channel == TarotEffectChannel.AerialCombat || channel == TarotEffectChannel.PlatformState))
        {
            return GetResolvedEffect(channel) == id;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (cardEffectsUnlocked[i] && GetSlotEffect(i) == id)
                return true;
        }

        return false;
    }

    public TarotEffectId GetResolvedEffect(TarotEffectChannel channel)
    {
        TarotEffectId resolved = TarotEffectId.E00;
        int bestOrder = -1;
        for (int i = 0; i < SlotCount; i++)
        {
            if (!cardEffectsUnlocked[i])
                continue;

            TarotEffectId effect = GetSlotEffect(i);
            if (TarotEffectChannels.Of(effect) != channel)
                continue;

            int order = activationSequence[i];
            if (order < 0)
                order = 0;

            if (order >= bestOrder)
            {
                bestOrder = order;
                resolved = effect;
            }
        }

        return resolved;
    }

    public bool ActivateNode(int slot)
    {
        if (slot < 0 || slot >= SlotCount || drawnCards[slot] == null)
            return false;
        if (tarotNodesActivated[slot])
            return false;

        tarotNodesActivated[slot] = true;
        cardEffectsUnlocked[slot] = true;
        hiddenPathsUnlocked[slot] = true;
        activationSequence[slot] = nextActivationOrder++;

        TarotEffectId effect = GetSlotEffect(slot);
        if (effect == TarotEffectId.E09)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                cardEffectsUnlocked[i] = true;
                if (activationSequence[i] < 0)
                    activationSequence[i] = activationSequence[slot];
            }
        }

        if (effect == TarotEffectId.E06)
            foldExitCharge++;

        return true;
    }

    public bool TryConsumeFoldCharge()
    {
        if (foldExitCharge <= 0)
            return false;

        foldExitCharge--;
        return true;
    }

    public bool IsZoneResolved(int zone)
    {
        return zone >= 0 && zone < ZoneCount && zoneResolved[zone];
    }

    public void SnapshotZone(int zone)
    {
        if (zone < 0 || zone >= ZoneCount || zoneResolved[zone])
            return;

        zoneResolved[zone] = true;
        int mask = 0;
        for (int id = 1; id <= 18; id++)
        {
            if (HasActiveEffect((TarotEffectId)id))
                mask |= 1 << id;
        }

        zoneEffectMask[zone] = mask;
    }

    public bool ZoneHasEffect(int zone, TarotEffectId id)
    {
        if (id == TarotEffectId.E00 || zone < 0 || zone >= ZoneCount || !zoneResolved[zone])
            return false;

        return (zoneEffectMask[zone] & (1 << (int)id)) != 0;
    }

    public string FormatSlot(int slot)
    {
        TarotCardData card = GetCard(slot);
        if (card == null)
            return "-";

        string name = TarotCatalog.GetDisplayName(card);
        string orient = IsReversed(slot) ? "逆" : "正";
        return name + "（" + orient + "）" + GetSlotEffect(slot);
    }

    public string SummarizeDrawn()
    {
        var parts = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
            parts.Add(FormatSlot(i));
        return string.Join("、", parts);
    }

    public string MainCardId => GetCard(0) != null ? GetCard(0).CardId : string.Empty;

    public List<string> GetEmotionCardIds()
    {
        var ids = new List<string>(3);
        for (int i = 1; i < SlotCount; i++)
        {
            if (drawnCards[i] != null)
                ids.Add(drawnCards[i].CardId);
        }

        return ids;
    }
}
