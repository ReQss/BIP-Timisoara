# Small Kitchen — Unity usage

This pack is set up as isometric scene sprites rather than Tilemap tiles because the
source images contain a complete room shell and an irregularly packed prop atlas.

After Unity finishes importing:

1. Create the shell with `GameObject > Cat Cafe > Small Kitchen Room`, or drag
   `wallsnfloor.png` into the scene yourself.
2. Expand `assets.png` in the Project window to see the individually named props.
3. Give the room shell a low Sprite Renderer sorting order (for example `-100`).
4. Drag props into the scene and position them from their bottom-centre pivots.
5. Keep characters above the room shell and adjust prop sorting orders where they
   should appear in front of or behind a character.

The importer also creates one isometric Tile Palette at
`Assets/Tile Palettes/Small Kitchen/Small Kitchen Palette.prefab`. Open
`Window > 2D > Tile Palette` and select **Small Kitchen Palette** to paint props onto an
isometric Tilemap. The room shell remains a scene sprite because it is one complete room,
not a repeating tile.

All sprites use 32 pixels per unit, point filtering, no mipmaps, clamp wrapping, and
uncompressed textures. If the source PNGs are replaced, run
`Tools > Cat Cafe > Reimport Small Kitchen Sprites`.
The palette can be rebuilt with
`Tools > Cat Cafe > Create or Update Small Kitchen Tile Palette`.

The original author permits commercial and noncommercial use and requests credit as
`@sythpixie` for commercial use. See `READ ME.txt` for the original terms.
