using System;
using UnityEngine;

/// <summary>
/// Network packet modifier types representing different protocol payloads and network constraints.
/// Connects gameplay mechanics directly to real-world computer networking concepts.
/// </summary>
public enum PacketModifierType
{
    StandardIPv4,   // Standard TCP/IPv4: 1500-byte MTU, balanced integrity and normal speed
    FragileUDP,      // Unencrypted UDP Datagram: +20% move speed, but double integrity loss on collision
    JumboFrame,      // 9000-byte Jumbo Frame: -12% jump height (payload weight), but +60% bonus Data Bits
    VolatileICMP     // High-priority ICMP Echo: slowly decays integrity (0.8%/s), grants +100 bonus Bits on S-Rank
}

/// <summary>
/// Represents network packet metadata and runtime modifications for the active delivery run.
/// </summary>
[System.Serializable]
public class PacketModifier
{
    public PacketModifierType type = PacketModifierType.StandardIPv4;
    public string protocolName = "IPv4 / TCP";
    public string payloadName = "STANDARD PAYLOAD [MTU 1500]";
    public string description = "Transmisi standar dengan proteksi error dasar.";
    public float speedMultiplier = 1.0f;
    public float jumpMultiplier = 1.0f;
    public float damagePenaltyMultiplier = 1.0f;
    public float integrityDrainPerSecond = 0.0f;
    public int bonusBitsReward = 0;

    public static PacketModifier Create(PacketModifierType type)
    {
        var mod = new PacketModifier { type = type };
        switch (type)
        {
            case PacketModifierType.FragileUDP:
                mod.protocolName = "UDP / UNRELIABLE STREAM";
                mod.payloadName = "FAST BITSTREAM [NO RETRANSMIT]";
                mod.description = "Kecepatan +20%, namun penalti tabrakan 2x lipat (tanpa handshake).";
                mod.speedMultiplier = 1.20f;
                mod.jumpMultiplier = 1.0f;
                mod.damagePenaltyMultiplier = 2.0f;
                mod.integrityDrainPerSecond = 0.0f;
                mod.bonusBitsReward = 60;
                break;

            case PacketModifierType.JumboFrame:
                mod.protocolName = "ETHERNET / JUMBO FRAME";
                mod.payloadName = "ARCHIVE PAYLOAD [MTU 9000]";
                mod.description = "Beban paket berat (-12% lompat), namun memberi +60% bonus Data Bits.";
                mod.speedMultiplier = 1.0f;
                mod.jumpMultiplier = 0.88f;
                mod.damagePenaltyMultiplier = 1.0f;
                mod.integrityDrainPerSecond = 0.0f;
                mod.bonusBitsReward = 120;
                break;

            case PacketModifierType.VolatileICMP:
                mod.protocolName = "ICMP / VOLATILE ECHO";
                mod.payloadName = "TIME-SENSITIVE PING [TTL CRITICAL]";
                mod.description = "Integritas memudar 0.8%/detik di luar Safe Zone. Hadiah +100 Bits jika S-Rank.";
                mod.speedMultiplier = 1.05f;
                mod.jumpMultiplier = 1.0f;
                mod.damagePenaltyMultiplier = 1.2f;
                mod.integrityDrainPerSecond = 0.8f;
                mod.bonusBitsReward = 100;
                break;

            case PacketModifierType.StandardIPv4:
            default:
                mod.protocolName = "IPv4 / TCP PROTOCOL";
                mod.payloadName = "STANDARD PAYLOAD [MTU 1500]";
                mod.description = "Transmisi standar dengan verifikasi error checksum seimbang.";
                mod.speedMultiplier = 1.0f;
                mod.jumpMultiplier = 1.0f;
                mod.damagePenaltyMultiplier = 1.0f;
                mod.integrityDrainPerSecond = 0.0f;
                mod.bonusBitsReward = 0;
                break;
        }
        return mod;
    }
}
