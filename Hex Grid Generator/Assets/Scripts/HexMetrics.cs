﻿using UnityEngine;

public static class HexMetrics
{
    #region Constants
    public const int ChunkSizeX = 5, ChunkSizeZ = 5;
    public const float ElevationStep = 3f;

    // Hexagon's anatomy
    // the edge's length (so also the distance from the center to any corner) is equal to 10
    // the outer radius is equal to 10 as well
    public const float OuterRadius = 10f;
    // the inner radius is equal to sqrt(3)/2 times the outer radius
    public const float InnerRadius = OuterRadius * 0.866025404f;
    public const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1f / OuterToInner;

    public const float SolidFactor = 0.8f;
    public const float BlendFactor = 1f - SolidFactor;

    public const int TerracesPerSlope = 2;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;
    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    public const float CellPerturbStrength = 1.75f;
    public const float NoiseScale = 0.003f; // world coordinates need to scall down to match the texture so noise can maintain its coherence
    public static float ElevationPerturbStrength = 1.5f;
    public static bool ElevationPerturbFlag = true;

    // river related stuff
    public const float StreamBedElevationOffset = -1.75f;
    public const float RiverSurfaceElevationOffset = -0.3f;

    public const float WaterFactor = 0.6f;
    public const float WaterSurfaceY = 1.8f;
    #endregion

    public static Texture2D NoiseSource;

    public static Vector3[] Corners = {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius) // seventh and first are exactly the same to prevent IndexOutOfBonds exception
    };

    /// <summary>
    /// Returns left triangle corner coordinates for the given direction.
    /// </summary>
    public static Vector3 GetLeftCorner(HexDirection direction) => Corners[(int)direction];
    /// <summary>
    /// Returns right triangle corner coordinates for the given direction.
    /// </summary>
    public static Vector3 GetRightCorner(HexDirection direction) => Corners[(int)direction + 1];

    /// <summary>
    /// Returns left triangle corner coordinates for the given direction multiplied by the SolidFactor constant.
    /// The higher the constant value the closer the corner is to the center of the hex.
    /// </summary>
    public static Vector3 GetLeftSolidCorner(HexDirection direction) => Corners[(int)direction] * SolidFactor;
    /// <summary>
    /// Returns right triangle corner coordinates for the given direction multiplied by the SolidFactor constant.
    /// The higher the constant value the closer the corner is to the center of the hex.
    /// </summary>
    public static Vector3 GetRightSolidCorner(HexDirection direction) => Corners[(int)direction + 1] * SolidFactor;

    /// <summary>
    /// Returns left triangle corner coordinates for the given direction multiplied by the WaterFactor constant.
    /// The higher the constant value the closer the corner is to the center of the hex.
    /// </summary>
    public static Vector3 GetLeftWaterCorner(HexDirection direction) => Corners[(int)direction] * WaterFactor;
    /// <summary>
    /// Returns right triangle corner coordinates for the given direction multiplied by the WaterFactor constant.
    /// The higher the constant value the closer the corner is to the center of the hex.
    /// </summary>
    public static Vector3 GetRightWaterCorner(HexDirection direction) => Corners[(int)direction + 1] * WaterFactor;

    public static Vector3 GetBridge(HexDirection direction)
        => (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;

    /// <summary>
    /// Interpolation between two values a and b is done with a third interpolator t.
    /// When t is 0, the result is a.When it is 1, the result is b.
    /// When t lies somewhere in between 0 and 1, a and b are mixed proportionally.
    /// Thus the formula for the interpolated result is (1 − t)a + tb.
    /// </summary>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        // To only adjust Y on odd steps, we can use (step + 1) / 2.
        // If we use an integer division, it will convert the sequence 1, 2, 3, 4 into 1, 1, 2, 2.
        float v = (step + 1) / 2 * VerticalTerraceStepSize;
        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// Returns the edge type based on the difference of the levels.
    /// </summary>
    public static HexEdgeType GetEdgeType(int level1, int level2)
    {
        if (level1 == level2)
            return HexEdgeType.Flat;

        // If the level difference is exactly one step, then we have a slope. It doesn't matter whether the slope goes up or down.
        int delta = level2 - level1;
        if (delta == 1 || delta == -1)
            return HexEdgeType.Slope;

        // in all other cases we have a cliff
        return HexEdgeType.Cliff;
    }

    // The samples are produced by sampling the texture using bilinear filtering, using the X and Z world coordinates as UV coordinates.
    // As our noise source is 2D, we ignore the third wold coordinate.
    public static Vector4 SampleNoise(Vector3 position)
        => NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        => (Corners[(int)direction] + Corners[(int)direction + 1]) * (0.5f * SolidFactor);

    /// <summary>
    /// Modifies the position of the point accordingly to the noise function.
    /// </summary>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * CellPerturbStrength;
        position.y += (sample.y * 2f - 1f) * CellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * CellPerturbStrength;
        return position;
    }
}