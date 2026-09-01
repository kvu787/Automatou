# Automapolis sprite grammar

Every current terrain and force has one authoritative 16x16 palette-indexed sprite in `sprites.json`. Whitespace divides rows into four-pixel groups and is ignored by the compiler.

The source uses one global palette and these rules:

- tactical top-down/three-quarter silhouettes;
- squared gunmetal, gold, and cyan shapes for human magitech;
- branching green, purple, and red shapes for alien biology;
- fixed upper-left light and hard pixel edges;
- opaque full-sector terrain and transparent forces, structures, and organisms;
- no state indicators baked into sprites; selection and status remain Shell overlays.

Run the asset compiler with:

```powershell
dotnet run --project tools/Automapolis.Assets
```

It validates dimensions and palette membership, then deterministically produces individual PNG files, an atlas, and a manifest in `generated`. The PNGs are committed because the Godot Shell consumes them; `sprites.json` remains the reviewable source of truth.
