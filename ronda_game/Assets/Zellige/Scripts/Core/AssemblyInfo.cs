using System.Runtime.CompilerServices;

// Only the EditMode test assembly may use the engine's internal test-setup factory
// (RondaGame.FromSetup, GameSetup). The game's own assemblies cannot see it.
[assembly: InternalsVisibleTo("Zellige.Tests.EditMode")]
