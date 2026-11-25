using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public static class Stage2DialogSmoke
{
    public static async Task<string> Main()
    {
        var asm = typeof(Enemy_Patrol).Assembly;
        var npcType = asm.GetType("NPC_Base");
        if (npcType == null) throw new Exception("FAIL: NPC_Base is not implemented");
        var dialogType = asm.GetType("DialogUI");
        if (dialogType == null) throw new Exception("FAIL: DialogUI is not implemented");
        if (asm.GetType("NPCDialogChoice") == null) throw new Exception("FAIL: NPCDialogChoice is not implemented");

        Application.runInBackground = true;
        Time.timeScale = 1;
        var report = new List<string>();
        var created = new List<GameObject>();
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception("FAIL: " + label); report.Add("PASS: " + label); };
        Func<string, System.Reflection.PropertyInfo> prop = n => npcType.GetProperty(n);
        var gm = GameManager.Ensure();
        try
        {
            gm.StartRun(0); await Task.Delay(350);
            var player = PlayerHealth.Instance.GetComponent<PlayerController>();
            var health = PlayerHealth.Instance;
            var body = player.GetComponent<Rigidbody2D>();
            var combat = player.GetComponent<PlayerCombat>();
            var input = InputManager.Instance;
            input.enabled = false;
            input.moveInput = Vector2.zero;
            health.drainRate = 0;
            player.enabled = false;
            player.FacingDir = 1;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            Vector2 origin = new Vector2(4000, 10);
            body.position = origin; body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            var npcGo = new GameObject("Dialog smoke NPC");
            created.Add(npcGo);
            npcGo.transform.position = origin + new Vector2(6, 0);
            npcGo.AddComponent<CircleCollider2D>().radius = 0.9f;
            var npc = npcGo.AddComponent(npcType);
            npcType.GetField("speakerName").SetValue(npc, "OPERATOR");
            npcType.GetField("lines").SetValue(npc, new[] { "Packet courier, reporting.", "System stable for now." });
            var picks = new List<int>();
            npcType.GetField("choices").SetValue(npc, BuildChoices(asm, picks));
            Physics2D.SyncTransforms();
            await Task.Delay(150);

            check(!(bool)prop("playerInRange").GetValue(npc), "an NPC six units away reports no player in range");
            check(!(bool)prop("promptVisible").GetValue(npc), "no interact hint shows while the player is away");
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null), "proximity alone never opens the dialog");

            npcGo.transform.position = origin + new Vector2(0.8f, 0);
            Physics2D.SyncTransforms();
            await Task.Delay(220);

            check((bool)prop("playerInRange").GetValue(npc), "collider overlap puts the NPC in interaction range");
            check((bool)prop("promptVisible").GetValue(npc), "proximity prompt is visible in range");
            var hint = npcGo.GetComponentInChildren<TextMesh>(true);
            check(hint != null && hint.text.Contains("ENTER"), "prompt advertises the interact key");
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null), "standing in range still does not open the dialog");

            input.interactPressed = true;
            await Task.Delay(120);

            check((bool)dialogType.GetProperty("IsOpen").GetValue(null), "interacting opens the dialog");
            check(player.isFrozen, "opening the dialog locks player control");
            check(!input.interactPressed, "opening the dialog swallows the interact press");
            check(!(bool)prop("promptVisible").GetValue(npc), "the prompt hides while the dialog is open");

            var dialog = dialogType.GetProperty("Instance").GetValue(null);
            check(dialog != null, "dialog exposes a live singleton instance while open");
            check((string)Read(dialog, "SpeakerText") == "OPERATOR", "dialog shows the configured speaker name");
            var bodyText = (string)Read(dialog, "BodyText");
            check(bodyText.Contains("Packet courier, reporting.") && bodyText.Contains("System stable for now."), "dialog renders every configured line");
            check(Read(dialog, "ChoiceCount").Equals(2), "dialog lists both configured choices");
            check(((string)Call(dialog, "ChoiceLabel", 0)).StartsWith("1") && ((string)Call(dialog, "ChoiceLabel", 1)).StartsWith("2"), "choices are numbered for keyboard selection");

            input.attackPressed = true;
            await Task.Delay(120);
            check(!combat.IsAttacking && player.isFrozen, "attack input cannot start a strike while the dialog is open");

            input.interactPressed = true;
            await Task.Delay(120);
            check(Read(dialog, "ChoiceCount").Equals(2) && (string)Read(dialog, "SpeakerText") == "OPERATOR", "interacting again mid-dialog does not stack a second panel");
            check(!input.interactPressed, "a repeated interact press is swallowed instead of reopening");

            Call(dialog, "SelectChoice", 1);
            Call(dialog, "CommitSelected");
            await Task.Delay(120);
            check(picks.Count == 1 && picks[0] == 1, "keyboard selection plus submit fires the chosen callback only");
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null), "committing a choice closes the dialog");
            check(!player.isFrozen, "closing the dialog releases the player");

            input.interactPressed = true;
            await Task.Delay(140);
            check((bool)dialogType.GetProperty("IsOpen").GetValue(null) && player.isFrozen, "the NPC can start a second conversation after the first closes");

            Call(dialog, "ClickChoice", 0);
            await Task.Delay(120);
            check(picks.Count == 2 && picks[1] == 0, "clicking a choice button fires that choice callback");
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null) && !player.isFrozen, "clicking a choice closes the dialog and unfreezes the player");

            input.attackPressed = true;
            await Task.Delay(120);
            check(combat.IsAttacking, "melee works again once the dialog is closed");
            combat.CancelAttack();

            npcType.GetField("choices").SetValue(npc, EmptyChoices(asm));
            input.interactPressed = true;
            await Task.Delay(140);
            check((bool)dialogType.GetProperty("IsOpen").GetValue(null) && Read(dialog, "ChoiceCount").Equals(0), "a dialogue without choices opens with no buttons");
            Call(dialog, "CommitSelected");
            await Task.Delay(120);
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null) && !player.isFrozen, "submit dismisses a choice-less dialogue and unfreezes the player");

            npcGo.transform.position = origin + new Vector2(6, 0);
            Physics2D.SyncTransforms();
            await Task.Delay(220);
            check(!(bool)prop("playerInRange").GetValue(npc) && !(bool)prop("promptVisible").GetValue(npc), "leaving range clears the prompt again");
            check(!(bool)dialogType.GetProperty("IsOpen").GetValue(null), "leaving range never opens the dialog");

            return string.Join("\n", report);
        }
        finally
        {
            Time.timeScale = 1;
            foreach (var go in created) if (go) UnityEngine.Object.Destroy(go);
            gm.ReturnToMenu();
        }
    }

    private static object Read(object target, string member)
    {
        if (target == null) throw new Exception("FAIL: reflection read of '" + member + "' on a null instance");
        return target.GetType().GetProperty(member).GetValue(target);
    }

    private static object Call(object target, string method, params object[] args)
    {
        if (target == null) throw new Exception("FAIL: reflection call of '" + method + "' on a null instance");
        return target.GetType().GetMethod(method).Invoke(target, args);
    }

    private static object BuildChoices(Assembly asm, List<int> picks)
    {
        var choiceType = asm.GetType("NPCDialogChoice");
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(choiceType));
        foreach (var label in new[] { "TAKE JOB", "NOT YET" })
        {
            var entry = Activator.CreateInstance(choiceType);
            choiceType.GetField("label").SetValue(entry, label);
            var evt = choiceType.GetField("onChoose").GetValue(entry) as UnityEvent;
            if (evt == null)
            {
                evt = new UnityEvent();
                choiceType.GetField("onChoose").SetValue(entry, evt);
            }
            int index = list.Count;
            evt.AddListener(() => picks.Add(index));
            list.Add(entry);
        }
        return list;
    }

    private static object EmptyChoices(Assembly asm)
    {
        var choiceType = asm.GetType("NPCDialogChoice");
        return Activator.CreateInstance(typeof(List<>).MakeGenericType(choiceType));
    }
}