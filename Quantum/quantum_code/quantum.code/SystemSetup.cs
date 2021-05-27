using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quantum {
  public static class SystemSetup {
    public static SystemBase[] CreateSystems(RuntimeConfig gameConfig, SimulationConfig simulationConfig) {
      return new SystemBase[] {
        // pre-defined core systems
        new Core.CullingSystem2D(), 
        new Core.CullingSystem3D(),
        
        new Core.PhysicsSystem2D(),
        new Core.PhysicsSystem3D(),

#if DEBUG
        Core.DebugCommand.CreateSystem(),
#endif

        //new Core.NavigationSystem(),
        new Core.EntityPrototypeSystem(),
        new Core.PlayerConnectedSystem(),

        new SceneObjAnimationSystem(),

        new CustomAnimatorSystem(),
        new CustomAnimatorStateSystem(),

        new PlayerManagerSystem(),
        new GameplaySystem(),
        // user systems go here 
      };
    }
  }
}
