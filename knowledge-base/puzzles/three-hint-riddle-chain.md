# Three Hint Riddle Chain

The main puzzle loop is a short hidden-hint sequence. Red must follow up to 3 hidden hints before finding Blue.

At runtime, the game procedurally chooses 3 hint locations from the possible garden locations. The selected hint locations can be different each run.

Each hidden hint contains an easy riddle. The riddle points Red toward the next location. For example, "It reflects the sky, but it is not a mirror" points toward the pond.

Only one hint should be active at a time. When Red finds the active hint, the game reveals the next riddle and activates the next hint location.

The sequence is solved when Red finds the third hint. After the third hint, Blue can be revealed or reached as the final goal.

Rock can help Red understand the current riddle, but Rock should not directly say the exact target location unless the design later allows direct answers.

Rock should use the game state to know the current riddle, current hint index, found hint locations, possible hint locations, and remaining clue points.

Good Rock tips guide attention. For example, if the riddle points to the pond, Rock might say that the sky sometimes rests on quiet water.

Bad Rock tips reveal the answer directly. For example, Rock should avoid saying "Go to the pond" unless direct answers are allowed.
