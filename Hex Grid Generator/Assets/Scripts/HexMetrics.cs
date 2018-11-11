﻿using UnityEngine;

public static class HexMetrics
{
    public const float ElevationStep = 5f;

    // Hexagon's anatomy
    // the edge's length (so also the distance from the center to any corner) is equal to 10
    // the outer radius is equal to 10 as well
    public const float OuterRadius = 10f;
    // the inner radius is equal to sqrt(3)/2 times the outer radius
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public const float SolidFactor = 0.75f;
    public const float BlendFactor = 1f - SolidFactor;

    public static Vector3[] Corners = {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius) // seventh and first are exactly the same to prevent IndexOutOfBonds exception
    };

    public static Vector3 GetFirstCorner(HexDirection direction) => Corners[(int)direction];
    public static Vector3 GetSecondCorner(HexDirection direction) => Corners[(int)direction + 1];

    public static Vector3 GetFirstSolidCorner(HexDirection direction) => Corners[(int)direction] * SolidFactor;
    public static Vector3 GetSecondSolidCorner(HexDirection direction) => Corners[(int)direction + 1] * SolidFactor;

    public static Vector3 GetBridge(HexDirection direction) 
        => (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
}