# Automapolis sprite grammar

Every current world object has one authoritative 16×16 palette-indexed sprite in `sprites.json`. Whitespace divides rows into four-pixel groups and is ignored by the compiler.

The source uses one global palette and these rules:

- tactical top-down/three-quarter silhouettes;
- fixed upper-left light;
- hard pixel edges with no gradients, antialiasing, or freeform colors;
- opaque, full-cell terrain and transparent beings, buildings, and effects;
- no state indicators baked into sprites—selection and status remain Shell overlays.

Run the asset compiler with:

```powershell
dotnet run --project tools/Automapolis.Assets
```

It validates dimensions and palette membership, then deterministically produces individual PNG files, an atlas, and a manifest in `generated`. The PNGs are committed because the Godot Shell consumes them; `sprites.json` remains the reviewable source of truth.
