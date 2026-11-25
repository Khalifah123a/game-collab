using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World shop endpoint. Offers "Hardware Tuning & Upgrades" / "Overclock Terminal" / "Batal";
/// buying is handled by ShopUI/OverclockUI. Housed inside a cyber kiosk booth.
/// </summary>
public class ShopNPC : NPC_Base
{
    private GameObject _boothGo;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(displayName) || displayName == "NPC")
            displayName = "Overclock & Tech Shop";
        if (string.IsNullOrEmpty(speakerName) || speakerName == "OPERATOR")
            speakerName = "CHIP TECHNICIAN // HARDWARE TECH SHOP";
        if (string.IsNullOrEmpty(prompt) || prompt == "Talk")
            prompt = "Buka Toko [Upgrade]";
        if (lines == null || lines.Length == 0 || (lines.Length == 1 && lines[0] == "..."))
        {
            lines = new[]
            {
                "Halo Kurir! Butuh modifikasi sasis, peningkatan Cyber Blade, Triple Jump, atau Booster Stamina?",
                "Pilih terminal di bawah untuk membuka Hardware Tech Shop atau Overclock Ability Chips!"
            };
        }
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_shop");
        visorColor = new Color(0.25f, 1f, 0.55f);
        bodyColor = new Color(0.18f, 0.55f, 0.32f);
        bodySize = new Vector2(1.5f, 1.2f);
        interactPrompt = "[E] SHOP";
        promptHeight = 3.1f;
        base.Awake();
        BuildShopBooth();
    }

    private void BuildShopBooth()
    {
        if (_boothGo != null) return;
        _boothGo = new GameObject("ShopBooth", typeof(SpriteRenderer));
        _boothGo.transform.SetParent(transform, false);
        _boothGo.transform.localPosition = Vector3.zero;
        var sr = _boothGo.GetComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/shop_kiosk");
        sr.sortingOrder = 10; // in front of NPC body so counter desk covers feet/legs, head visible in window!
    }

    private void Reset()
    {
        npcId = nameof(ShopNPC);
        displayName = "Overclock & Tech Shop";
        speakerName = "CHIP TECHNICIAN // HARDWARE TECH SHOP";
        prompt = "Buka Toko [Upgrade]";
        interactPrompt = "[E] SHOP";
        promptHeight = 3.1f;
        bodySize = new Vector2(1.5f, 1.2f);
        if (customSprite == null)
            customSprite = Resources.Load<Sprite>("GeneratedArt/npc_shop");
        visorColor = new Color(0.25f, 1f, 0.55f);
        bodyColor = new Color(0.18f, 0.55f, 0.32f);
        lines = new[] { "Halo kurir! Butuh modifikasi hardware, upgrade blaster, triple jump, atau recovery booster? Semua tersedia di Tech Shop!" };
    }

    public override string GetPrompt()
    {
        return PlayerInRange ? prompt : displayName;
    }

    public override IReadOnlyList<string> GetChoices()
    {
        return new[] { "Hardware Tuning & Modul Shop", "Overclock Terminal (Abilities)", "Batal / Tutup" };
    }

    protected override void OnChoiceChosen(int index)
    {
        if (index == 0)
        {
            ShopUI.Ensure().Open(this);
        }
        else if (index == 1)
        {
            OverclockUI.Ensure().Open(this);
        }
    }
}
