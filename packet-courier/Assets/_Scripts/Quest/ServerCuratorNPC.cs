using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gateway Curator / Station Inspector waiting at the final safe zone beside the Server B portal.
/// Monitors transmission security and greets the courier on successful parcel hand-in.
/// </summary>
public class ServerCuratorNPC : NPC_Base
{
    private void Reset()
    {
        npcId = nameof(ServerCuratorNPC);
        displayName = "Gateway Curator";
        speakerName = "CURATOR KIRA";
        prompt = "Bicara [Pengawas Gateway]";
        visorColor = new Color(1f, 0.85f, 0.25f);
        bodyColor = new Color(0.25f, 0.45f, 0.75f);
        lines = new[]
        {
            "Selamat datang di Safe Zone Akhir, Kurir!",
            "Saya bertugas menginspeksi lalu lintas data gateway Server B.",
            "Serahkan paketmu ke Terminal Penerima di sebelah agar firewall terbuka penuh!"
        };
    }

    protected override void Start()
    {
        base.Start();
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            var s = Resources.Load<Sprite>("GeneratedArt/npc_curator");
            if (s != null) { sr.sprite = s; sr.color = Color.white; }
        }
    }

    public override string GetPrompt()
    {
        var quests = QuestService.Instance;
        bool delivered = quests != null && quests.IsPackageDelivered(worldIndex);
        if (delivered) return "Gateway Aman // Siap Lolos!";
        return PlayerInRange ? prompt : displayName;
    }

    public override IReadOnlyList<string> GetChoices()
    {
        var quests = QuestService.Instance;
        bool delivered = quests != null && quests.IsPackageDelivered(worldIndex);
        if (delivered)
        {
            return new[]
            {
                "Terima kasih atas kerja kerasmu! Gateway Server B sudah aman untuk dilewati."
            };
        }
        return new[]
        {
            "Periksa Status Gateway",
            "Siap laksanakan, saya serahkan paketnya sekarang!"
        };
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index == 0)
        {
            var quests = QuestService.Instance;
            bool delivered = quests != null && quests.IsPackageDelivered(worldIndex);
            string msg = delivered
                ? "Status: GATEWAY ONLINE // VERIFIED"
                : "Status: GATEWAY TERKUNCI // MENUNGGU PAKET";
            DialogUI.Ensure().Show(speakerName, new[] { msg }, null, null, this);
        }
    }
}
