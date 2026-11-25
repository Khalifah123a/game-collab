using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-start safe zone endpoint of the mandatory package quest. Hands the packet over,
/// or puts the player back on track when a package was never collected.
/// </summary>
public class PackageGiverNPC : NPC_Base
{
    private const string PacketLabel = "PACKET";

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(displayName) || displayName == "NPC")
            displayName = "Courier Dispatch";
        if (string.IsNullOrEmpty(speakerName) || speakerName == "OPERATOR")
            speakerName = "DISPATCH OPERATOR // SERVER ALPHA";
        if (string.IsNullOrEmpty(prompt) || prompt == "Talk")
            prompt = "Ambil PAKET [Wajib]";
        if (lines == null || lines.Length == 0 || (lines.Length == 1 && lines[0] == "..."))
        {
            lines = new[]
            {
                "Halo Kurir! Bawa paket data terenkripsi ini ke Penerima di Server B di ujung kanan sektor.",
                "Ini misi WAJIB agar gerbang Server B terbuka menuju stage berikutnya!"
            };
        }
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_dispatch");
        visorColor = new Color(0.2f, 0.95f, 1f);
        bodyColor = new Color(0.22f, 0.42f, 0.8f);
        interactPrompt = "[E] QUEST";
        base.Awake();
    }

    private void Reset()
    {
        npcId = nameof(PackageGiverNPC);
        displayName = "Courier Dispatch";
        speakerName = "DISPATCH OPERATOR";
        prompt = "Ambil PAKET [Wajib]";
        interactPrompt = "[E] QUEST";
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_dispatch");
        visorColor = new Color(0.2f, 0.95f, 1f);
        bodyColor = new Color(0.22f, 0.42f, 0.8f);
        lines = new[]
        {
            "Halo Kurir! Bawa paket data kritis ini ke Server B di akhir rute.",
            "Ini tugas WAJIB agar pintu gateway server berikutnya terbuka!",
            "Waspada serangan drone dan laser di perjalanan!"
        };
    }

    public override string GetPrompt()
    {
        var quests = QuestService.Instance;
        var state = quests != null ? quests.GetPackageState(worldIndex) : PackageState.None;
        if (PackageQuestRules.IsDelivered(state)) return "Packet already delivered";
        if (state == PackageState.Active) return "Packet already in transit";
        return PlayerInRange ? prompt : displayName;
    }

    public override IReadOnlyList<string> GetChoices()
    {
        var quests = QuestService.Ensure();
        var state = quests.GetPackageState(worldIndex);
        if (PackageQuestRules.IsDelivered(state)) return new[] { "Nothing left to collect" };
        if (state == PackageState.Active) return new[] { PacketLabel + " already in transit" };
        return new[] { "Accept " + PacketLabel };
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index < 0) return;
        var quests = QuestService.Ensure();
        quests.OfferPackage(worldIndex);
        quests.AcceptPackage(worldIndex);
    }
}
