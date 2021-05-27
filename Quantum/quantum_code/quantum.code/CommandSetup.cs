using Photon.Deterministic;

namespace Quantum {
  public static class CommandSetup {
    public static DeterministicCommand[] CreateCommands(RuntimeConfig gameConfig, SimulationConfig simulationConfig) {
      return new DeterministicCommand[] {
        // pre-defined core commands
#if DEBUG
        Core.DebugCommand.CreateCommand(),
#endif

        // user commands go here
      };
    }
  }
}
