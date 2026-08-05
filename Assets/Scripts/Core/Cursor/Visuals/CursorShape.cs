using System;
using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // The geometry of a cursor piece, as numbers rather than a picture. Exposing the silhouette
    // as knobs is what lets a variant be designed in the Inspector while the game is running —
    // arm length and thickness are the difference between "GBA bracket" and "thin modern
    // corner", and neither is worth a round trip out to an image editor to find.
    //
    // All values are fractions of the piece's own footprint, so they read the same at any scale.
    [Serializable]
    public struct CursorShape
    {
        [Tooltip("How far each bracket arm reaches along the tile edge. Short arms read as delicate ticks; long arms close toward a full frame.")]
        [Range(0.1f, 1f)] public float armLength;

        [Tooltip("How thick the bracket arms and the arrow outline are. The single biggest lever on whether the cursor reads as heavy or light.")]
        [Range(0.04f, 0.5f)] public float thickness;

        [Tooltip("Weight of the dark keyline around every shape. This is what keeps the cursor legible over bright terrain — 0 removes it entirely.")]
        [Range(0f, 0.2f)] public float outlineWeight;

        [Tooltip("Rounds the ends and corners of the shapes. 0 is hard and geometric, higher is softer.")]
        [Range(0f, 0.5f)] public float cornerRadius;

        [Tooltip("Width of the arrow at its base, as a fraction of the piece. Wide arrows read as chevrons, narrow ones as needles.")]
        [Range(0.1f, 1f)] public float arrowWidth;

        [Tooltip("How aggressively the arrow tapers to its point. Low is blunt and stubby, high is a long sharp spike.")]
        [Range(0f, 1f)] public float arrowSharpness;

        public static CursorShape Default => new()
        {
            armLength = 0.72f,
            thickness = 0.2f,
            outlineWeight = 0.07f,
            cornerRadius = 0.06f,
            arrowWidth = 0.55f,
            arrowSharpness = 0.6f,
        };

        // Slider drags change these continuously, so the renderer compares before it rebuilds a
        // texture. Approximately is enough — these are all authored, never computed.
        public bool Matches(in CursorShape other) =>
            Mathf.Approximately(armLength, other.armLength)
            && Mathf.Approximately(thickness, other.thickness)
            && Mathf.Approximately(outlineWeight, other.outlineWeight)
            && Mathf.Approximately(cornerRadius, other.cornerRadius)
            && Mathf.Approximately(arrowWidth, other.arrowWidth)
            && Mathf.Approximately(arrowSharpness, other.arrowSharpness);
    }
}
