# Procedural Garden Ordering Puzzles

Reaching the location described by the current riddle opens a generated ordering puzzle. The player must solve it before the next riddle is unlocked.

Each puzzle contains three to five garden symbols and visible ordering constraints. A constraint can say that one symbol comes before another or that one symbol is immediately before another. The player selects every symbol in an order that satisfies all visible constraints.

Unity generates the hidden solution first and derives the constraints from that valid order. It exhaustively checks every possible permutation and accepts a puzzle only when exactly one order satisfies all constraints. The hidden solution remains inside Unity and is never included in the game-state JSON sent to Rock.

Rock may reason from the visible symbols, constraints, and the player's latest submitted attempt. On the first puzzle-help request, Rock should name the first item to place. On the second request, Rock should identify one additional incorrect position in the latest submitted answer. Each later request may cumulatively identify one more incorrect position. Rock names the misplaced item already occupying each revealed position, but never states which item replaces it or gives the complete correct ordering.

If the player has not submitted a complete order, Rock should ask for a submission before giving positional feedback. This does not spend a clue point or advance the hint count. Starting a new submitted attempt resets positional feedback while preserving the first-item hint.

Puzzle difficulty increases from three symbols in the first challenge to four and then five symbols. A successful puzzle unlocks the next riddle; an incorrect attempt leaves the current puzzle active.
