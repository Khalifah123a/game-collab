/// <summary>
/// Pure domain logic for packet integrity, grades, penalties, and bonus payouts.
/// Kept free of UnityEngine dependencies so it can be verified headlessly.
/// </summary>
public enum DeliveryGrade
{
    S,
    A,
    B,
    C
}

public static class PacketIntegrityRules
{
    public const float MinIntegrity = 10f;
    public const float PenaltyPerHit = 6f;

    public static DeliveryGrade CalculateGrade(float integrity)
    {
        if (integrity >= 89.5f) return DeliveryGrade.S;
        if (integrity >= 69.5f) return DeliveryGrade.A;
        if (integrity >= 39.5f) return DeliveryGrade.B;
        return DeliveryGrade.C;
    }

    public static int GetBonusBits(DeliveryGrade grade)
    {
        switch (grade)
        {
            case DeliveryGrade.S: return 150;
            case DeliveryGrade.A: return 80;
            case DeliveryGrade.B: return 30;
            default: return 0;
        }
    }

    public static string GetGradeString(DeliveryGrade grade)
    {
        switch (grade)
        {
            case DeliveryGrade.S: return "GRADE S [PRISTINE]";
            case DeliveryGrade.A: return "GRADE A [CLEAN]";
            case DeliveryGrade.B: return "GRADE B [DEGRADED]";
            default: return "GRADE C [CORRUPTED]";
        }
    }
}
