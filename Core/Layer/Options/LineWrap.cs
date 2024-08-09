using Helion.Geometry;
using Helion.Render.Common.Renderers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Layer.Options;

internal static class LineWrap
{
    public static void Calculate(string inputText, string font, int fontSize, int maxWidth, IHudRenderContext hud, List<string> lines, StringBuilder builder,
        out int requiredHeight)
    {
        lines.Clear();
        builder.Clear();
        if (string.IsNullOrEmpty(inputText))
        {
            requiredHeight = 0;
            return;
        }

        int maxTokenHeight = 0;
        int widthCounter = 0;

        int splitStart;
        int splitEnd = 0;
        for (int i = 0; i < inputText.Length; i++)
        {
            if (inputText[i] == ' ' || i == inputText.Length - 1)
            {
                splitStart = splitEnd;
                splitEnd = i;
            }
            else
            {
                continue;
            }

            splitEnd++;
            var token = inputText.AsSpan(splitStart, splitEnd - splitStart);
            var tokenSize = hud.MeasureText(token, font, fontSize);
            maxTokenHeight = Math.Max(maxTokenHeight, tokenSize.Height);

            if (widthCounter + tokenSize.Width > maxWidth)
            {
                lines.Add(builder.ToString());
                builder.Clear();
                widthCounter = 0;
            }

            builder.Append(token);
            widthCounter += tokenSize.Width;
        }

        // Flush the last line out of the StringBuilder
        if (builder.Length > 0)
            lines.Add(builder.ToString());

        requiredHeight = lines.Count * maxTokenHeight;
    }
}
