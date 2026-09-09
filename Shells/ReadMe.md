# Shells

Shells provide optional presentation and input. They may add color, layout,
animation, audio, accessibility, automation, remote play, or entirely different
controls, but may not contain simulation rules.

Unqualified **Shell** means the main 3D Godot Shell, using SimplePaint and the
required orthographic 3/4 overhead camera. **Text shell** means terminal-only
input/output. **2d shell** means the currently implemented symbolic Godot
presentation in `Godot`, which consumes the Kernel Host JSON-lines protocol.
The main 3D Shell's production model pipeline is not yet implemented.

Future Shells may include pixel-art 2D, non-pixel-art 2D, and other visual styles
of 3D presentation. Other valid Shells could be a Discord bot, a screen reader,
a web client, or an AI experiment harness. See
[shell terminology](../Documentation/Architecture.md#shell-terminology).
