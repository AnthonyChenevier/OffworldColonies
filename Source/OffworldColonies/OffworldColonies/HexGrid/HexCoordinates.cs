using System;
using UnityEngine;

public struct HexCoordinates {
    public int X { get; }

    public int Y => -X - Z;

    public int Z { get; }

    public HexCoordinates(int x, int z) : this() {
        X = x;
        Z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z) {
        if ((z & 1) == 1 && z < 0)
            x++;
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position) {
        float x = position.x / (HexMetrics.InnerRadius * 2f);
        float y = -x;

        float offset = position.z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ == 0)
            return new HexCoordinates(iX, iZ);

        //fix position rounding error
        float dX = Mathf.Abs(x - iX);
        float dY = Mathf.Abs(y - iY);
        float dZ = Mathf.Abs(-x - y - iZ);

        if (dX > dY && dX > dZ)
            iX = -iY - iZ;
        else if (dZ > dY)
            iZ = -iX - iY;

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString() {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }
}