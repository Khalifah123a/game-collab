using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Secret Hermit NPC tucked away in difficult high-altitude parkour niches.
/// Rewards the courier for their traversal skill with secret lore and bonus DataBits.
/// </summary>
public class HiddenHermitNPC : NPC_Base
{
    private string RewardId => "hermit:reward:" + worldIndex + ":" + gameObject.name;
    private bool _rewardClaimed;

    private void Reset()
    {
        npcId = nameof(HiddenHermitNPC);
        displayName = "Glitch Hermit";
        speakerName = "DATA HERMIT";
        prompt = "Bicara [Petapa Rahasia]";
        visorColor = new Color(0.7f, 0.3f, 1f);
        bodyColor = new Color(0.3f, 0.2f, 0.45f);
        lines = new[]
        {
            "Hehe... Kamu berhasil memanjat sampai ke sini, kurir?",
            "Jarang sekali ada paket yang melintasi jalur udara setinggi ini.",
            "Sebagai apresiasi kelincahanmu, ambillah cipher rahasia ini!"
        };
    }

    protected override void Start()
    {
        base.Start();
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            var s = Resources.Load<Sprite>("GeneratedArt/npc_hermit");
            if (s != null) { sr.sprite = s; sr.color = Color.white; }
        }

        var gm = GameManager.Instance;
        _rewardClaimed = gm != null && gm.ledger.IsCollected(RewardId);
    }

    public override string GetPrompt()
    {
        return _rewardClaimed ? "Petapa Rahasia [Sudah Diklaim]" : (PlayerInRange ? prompt : displayName);
    }

    public override IReadOnlyList<string> GetChoices()
    {
        if (_rewardClaimed)
            return new[] { "Teruslah melompat tinggi, kurir!" };
        return new[] { "Terima Hadiah Petapa (+200 Bits)", "Tanya rahasia rute" };
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index == 0 && !_rewardClaimed)
        {
            _rewardClaimed = true;
            var gm = GameManager.Ensure();
            gm.ledger.Collect(RewardId, 200);
            gm.dataBitsCollectedThisRun = gm.ledger.Bits;
            OptionalQuestService.ReportCollect(worldIndex, "npc:hermit", RewardId);

            DialogUI.Ensure().Show(speakerName, new[] { "Hadiah 200 DataBits telah ditambahkan ke saldo tokomu!" }, null, null, this);
        }
        else if (index == 1)
        {
            DialogUI.Ensure().Show(speakerName, new[] { "Gunakan bouncer plasma dan wall-jump untuk memangkas waktu traversal!" }, null, null, this);
        }
    }
}
