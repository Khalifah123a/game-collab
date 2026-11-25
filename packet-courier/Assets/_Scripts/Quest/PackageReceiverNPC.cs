using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-end safe zone endpoint of the mandatory package quest. Takes the packet in and
/// opens the world gate, complete with an interactive hand-in delivery animation.
/// </summary>
public class PackageReceiverNPC : NPC_Base
{
    private const string PacketLabel = "PACKET";
    private float _deliveryAnimTimer;
    private GameObject _deliveryFxObj;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(displayName) || displayName == "NPC")
            displayName = "Server B Terminal";
        if (string.IsNullOrEmpty(speakerName) || speakerName == "OPERATOR")
            speakerName = "TERMINAL OPERATOR // PACKAGE RECEIVER";
        if (string.IsNullOrEmpty(prompt) || prompt == "Talk")
            prompt = "Serahkan PAKET [Wajib]";
        if (lines == null || lines.Length == 0 || (lines.Length == 1 && lines[0] == "..."))
        {
            lines = new[]
            {
                "Selamat sampai di tujuan, Kurir!",
                "Serahkan paket data untuk memverifikasi transmisi dan membuka gateway Server B."
            };
        }
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_receiver");
        visorColor = new Color(1f, 0.78f, 0.2f);
        bodyColor = new Color(0.72f, 0.45f, 0.15f);
        bodySize = new Vector2(1.5f, 1.5f);
        interactPrompt = "[E] DELIVER";
        promptHeight = 2.4f;
        base.Awake();
    }

    private void Reset()
    {
        npcId = nameof(PackageReceiverNPC);
        displayName = "Server B Terminal";
        speakerName = "TERMINAL OPERATOR // PACKAGE RECEIVER";
        prompt = "Serahkan PAKET [Wajib]";
        interactPrompt = "[E] DELIVER";
        promptHeight = 2.4f;
        bodySize = new Vector2(1.5f, 1.5f);
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_receiver");
        visorColor = new Color(1f, 0.78f, 0.2f);
        bodyColor = new Color(0.72f, 0.45f, 0.15f);
        lines = new[]
        {
            "Selamat sampai di tujuan!",
            "Serahkan paket data untuk memverifikasi transmisi dan membuka gateway Server B."
        };
    }

    public override string GetPrompt()
    {
        var quests = QuestService.Instance;
        var state = quests != null ? quests.GetPackageState(worldIndex) : PackageState.None;
        if (PackageQuestRules.IsDelivered(state)) return "Packet received";
        if (state == PackageState.Active) return PlayerInRange ? prompt : displayName;
        return "No packet in transit";
    }

    public override IReadOnlyList<string> GetChoices()
    {
        var quests = QuestService.Ensure();
        var state = quests.GetPackageState(worldIndex);
        if (PackageQuestRules.IsDelivered(state)) return new[] { PacketLabel + " already received" };
        if (state == PackageState.Active) return new[] { "Hand in " + PacketLabel };
        return new[] { "Recover missing " + PacketLabel };
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index < 0) return;
        var quests = QuestService.Ensure();
        quests.OfferPackage(worldIndex);
        quests.AcceptPackage(worldIndex);
        quests.DeliverPackage(worldIndex);
        quests.ClaimPackage(worldIndex);

        var pim = PacketIntegrityManager.Instance;
        float finalIntegrity = pim != null ? pim.currentIntegrity : 100f;
        var grade = PacketIntegrityManager.CalculateGrade(finalIntegrity);
        int bonus = PacketIntegrityManager.GetBonusBits(grade);
        if (bonus > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddDataBits(bonus);
        }

        PlayDeliveryAnimation(finalIntegrity, grade, bonus);
        TransmissionScorecardUI.Show(finalIntegrity, grade, bonus, pim?.ActiveModifier);

        AchievementManager.Unlock("ACH_RFC791");
        if (grade == DeliveryGrade.S)
        {
            AchievementManager.Unlock("ACH_ZERO_LOSS");
        }
    }

    public void PlayDeliveryAnimation(float integrity = 100f, DeliveryGrade grade = DeliveryGrade.S, int bonus = 150)
    {
        _deliveryAnimTimer = 3.5f;

        if (_deliveryFxObj == null)
        {
            _deliveryFxObj = new GameObject("DeliveryFx");
            _deliveryFxObj.transform.SetParent(transform, false);
            _deliveryFxObj.transform.localPosition = new Vector3(0f, promptHeight + 0.6f, 0f);

            var tm = _deliveryFxObj.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 48;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.text = $"[ PAKET DITERIMA! ]\n{PacketIntegrityManager.GetGradeString(grade)} (+{bonus} BITS)\n[ TRANSMISSION VERIFIED // GATE OPEN ]";
            tm.color = PacketIntegrityManager.GetGradeColor(grade);
            tm.GetComponent<MeshRenderer>().sortingOrder = 50;
        }
        else
        {
            var tm = _deliveryFxObj.GetComponent<TextMesh>();
            if (tm != null)
            {
                tm.text = $"[ PAKET DITERIMA! ]\n{PacketIntegrityManager.GetGradeString(grade)} (+{bonus} BITS)\n[ TRANSMISSION VERIFIED // GATE OPEN ]";
                tm.color = PacketIntegrityManager.GetGradeColor(grade);
            }
            _deliveryFxObj.SetActive(true);
        }

        AudioManager.PlayLevelClear();
        JuiceManager.Shake(0.3f);
        if (JuiceManager.Instance != null)
        {
            JuiceManager.SpawnImpactSparks(transform.position + Vector3.up * 1f, new Color(0.2f, 1f, 0.4f, 1f));
        }
    }

    protected override void Update()
    {
        base.Update();
        if (_deliveryAnimTimer > 0f)
        {
            _deliveryAnimTimer -= Time.deltaTime;
            float pulse = 0.7f + 0.3f * Mathf.PingPong(Time.time * 8f, 1f);
            visorColor = new Color(0.2f, 1f, 0.5f) * pulse;

            if (_deliveryAnimTimer <= 0f && _deliveryFxObj != null)
                _deliveryFxObj.SetActive(false);
        }
    }
}
