This subfolder is a complete, fully self-contained project.

# Experiment requirements

Every approach in this experiment must use integer math only.

This includes position, translation, orientation, rotation, pivot calculations, footprint occupancy, and collision checks. Do not use floating-point geometry, trigonometric rotation, or continuous-angle sweep calculations for these rules. If fractional cell positions need representation, use an integer coordinate scale and integer operations.

The movement implementation uses doubled integer coordinates for centers and pivots, and integer coordinates for footprint dimensions and obstacle cells.
