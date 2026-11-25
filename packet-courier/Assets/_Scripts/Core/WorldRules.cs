using UnityEngine;

public static class WorldRules
{
    public const int Count = 5;
    public static int Segments(int world) => world == 0 ? 3 : world == 1 ? 4 : world == 4 ? 1 : 5;
    public static float TTL(int world) => new float[] { 100, 150, 170, 190, 240 }[Mathf.Clamp(world, 0, 4)];
    public static string Title(int world) => new[] { "Localhost", "LAN Party", "Fiber Deep", "Satellite Relay", "Rootkit Core" }[Mathf.Clamp(world, 0, 4)];
}
