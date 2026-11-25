using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Safe zone endpoint for optional side quests (bounties / item collections).
/// Provides side quest details and pays out bonus DataBits upon completion.
/// </summary>
public class SideQuestNPC : NPC_Base
{
    protected override void Awake()
    {
        if (string.IsNullOrEmpty(displayName) || displayName == "NPC")
            displayName = "Bounty Broker";
        if (string.IsNullOrEmpty(speakerName) || speakerName == "OPERATOR")
            speakerName = "NETRUNNER FIXER // KONTRAK OPSIONAL";
        if (string.IsNullOrEmpty(prompt) || prompt == "Talk")
            prompt = "Bicara [Misi Opsional]";
        if (lines == null || lines.Length == 0 || (lines.Length == 1 && lines[0] == "..."))
        {
            lines = new[]
            {
                "Punya waktu luang saat mengantar paket?",
                "Ada kontrak sampingan di sektor ini. Selesaikan untuk bonus saldo DataBits!"
            };
        }
        visorColor = new Color(0.92f, 0.28f, 1f);
        bodyColor = new Color(0.42f, 0.15f, 0.62f);
        base.Awake();
    }

    private void Reset()
    {
        npcId = nameof(SideQuestNPC);
        displayName = "Bounty Broker";
        speakerName = "NETRUNNER FIXER";
        prompt = "Bicara [Misi Opsional]";
        visorColor = new Color(0.92f, 0.28f, 1f);
        bodyColor = new Color(0.42f, 0.15f, 0.62f);
        lines = new[]
        {
            "Punya waktu luang saat mengantar paket?",
            "Ada kontrak sampingan dari jaringan gelap.",
            "Selesaikan untuk mendapatkan bonus saldo DataBits!"
        };
    }

    public override string GetPrompt()
    {
        int world = worldIndex >= 0 ? worldIndex : (GameManager.Instance != null ? GameManager.Instance.currentWorldIndex : 0);
        var quest = OptionalQuestService.Get(world, 0);
        if (quest != null && quest.CanClaim) return "KLAIM BONUS OPSIONAL!";
        return PlayerInRange ? prompt : displayName;
    }

    public override IReadOnlyList<string> GetChoices()
    {
        int world = worldIndex >= 0 ? worldIndex : (GameManager.Instance != null ? GameManager.Instance.currentWorldIndex : 0);
        var quest = OptionalQuestService.Get(world, 0);

        var list = new List<string>();
        if (quest == null)
        {
            list.Add("Tidak ada kontrak opsional di area ini");
        }
        else if (quest.CanClaim)
        {
            list.Add($"Klaim Bonus: +{quest.BonusBits} DataBits!");
        }
        else if (quest.Claimed)
        {
            list.Add($"Misi Selesai: +{quest.BonusBits} Bits sudah diterima");
        }
        else
        {
            list.Add($"Status: {quest.ProgressLabel}");
        }

        list.Add("Tutup Percakapan");
        return list;
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index == 0)
        {
            int world = worldIndex >= 0 ? worldIndex : (GameManager.Instance != null ? GameManager.Instance.currentWorldIndex : 0);
            var quest = OptionalQuestService.Get(world, 0);
            if (quest != null && quest.CanClaim)
            {
                int bits = OptionalQuestService.ClaimOptional(world, 0);
                if (bits > 0)
                {
                    lines = new[]
                    {
                        $"Kerja bagus kurir! Bonus +{bits} DataBits telah ditransfer ke dompetmu.",
                        "Lanjutkan tugas utamamu ke Server B!"
                    };
                }
            }
        }
    }
}
