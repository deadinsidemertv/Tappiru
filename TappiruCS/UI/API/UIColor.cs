using System;
using OpenTK.Mathematics;

public struct UIColor
{
    public Color4 Value;

    public UIColor(Color4 color)
    {
        Value = color;
    }
    public float R
    {
        get => Value.R;
        set => Value.R = value;
    }

    public float G
    {
        get => Value.G;
        set => Value.G = value;
    }

    public float B
    {
        get => Value.B;
        set => Value.B = value;
    }

    public float A
    {
        get => Value.A;
        set => Value.A = value;
    }
    // ✅ UIColor → Color4
    public static implicit operator Color4(UIColor c) => c.Value;

    // ✅ Color4 → UIColor
    public static implicit operator UIColor(Color4 color)
    {
        return new UIColor(color);
    }

    // ✅ string → UIColor
    public static implicit operator UIColor(string hex)
    {
        return new UIColor(ParseHex(hex));
    }
    private static Color4 ParseHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Hex string is null or empty");

        hex = hex.Replace("#", "");

        if (hex.Length == 3)
        {
            hex = string.Concat(
                hex[0], hex[0],
                hex[1], hex[1],
                hex[2], hex[2]
            );
        }

        if (hex.Length != 6 && hex.Length != 8)
            throw new FormatException($"Invalid hex color: {hex}");

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);

        byte a = 255;
        if (hex.Length == 8)
            a = Convert.ToByte(hex.Substring(6, 2), 16);

        return new Color4(
            r / 255f,
            g / 255f,
            b / 255f,
            a / 255f
        );
    }
}