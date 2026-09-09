# Experiment requirements

Every approach in this experiment must use integer math only.

This includes position, translation, orientation, rotation, pivot calculations, footprint occupancy, and collision checks. Do not use floating-point geometry, trigonometric rotation, or continuous-angle sweep calculations for these rules. If fractional cell positions need representation, use an integer coordinate scale and integer operations.

The existing implementation predates this requirement and does not yet comply. Recording this requirement does not constitute an implementation fix.
