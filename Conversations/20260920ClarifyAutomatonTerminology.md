# Clarify Automaton Terminology

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

currently, the term automaton/automata is overloaded.
automaton has a very specific meaning for this app. it describes the memory and logic used by each unit to decide what to do each turn. automaton/automata terms shouldn't be used for anything else.
fix this

## Assistant

I’ll check the prototype for uses of “automaton” and “automata,” reserve those terms for each unit’s decision-making memory and logic, and rename the other uses.

## Assistant

The overloaded uses are in the simulation controls, status messages, unit statistics, and behavior summaries. I’ll use “simulation,” “unit statistics,” and “unit behavior” there, and make the definition explicit in the documentation. The existing automaton classes already match your intended meaning.

## Assistant

The terminology is updated, and the build and all 15 simulation checks passed. I’m committing the changes and recording this conversation separately, as required by the repository instructions.

## Assistant

Fixed throughout the prototype’s interface, messages, documentation, and test descriptions. “Automaton/automata” now refers exclusively to each unit’s decision-making memory and logic.

Build and all 15 simulation checks passed. Committed as `628e290`; conversation recorded separately.
