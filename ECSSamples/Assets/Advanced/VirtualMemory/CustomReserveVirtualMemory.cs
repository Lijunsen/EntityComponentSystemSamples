using System.Linq;
using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomReserveVirtualMemory : ICustomBootstrap
{
    // Using a custom bootstrap that returns false lets you set extra data (such as the total address space you want to reserve for ECS Chunks) before worlds are default initialized.
    public bool Initialize(string defaultWorldName)
    {
        var world = new World(defaultWorldName, WorldFlags.Game);
        World.DefaultGameObjectInjectionWorld = world;
        
        var systemList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default).ToList();
        if (SceneManager.GetActiveScene().name == "CommonCube" || SceneManager.GetActiveScene().name == "TransparentCube")
        {
            Debug.Log("Scene:MyScene");
            systemList.Add(typeof(AudioSystem));
            systemList.Add(typeof(SpawnerSystem));
            systemList.Add(typeof(CubeTranslationTransitionSystem));
        }
        
        if (SceneManager.GetActiveScene().name == "LightingCube")
        {
            Debug.Log("Scene:LightingCube");
            systemList.Add(typeof(AudioSystem));
            systemList.Add(typeof(SpawnerSystem));
            systemList.Add(typeof(CubeTranslationTransitionSystem));
            systemList.Add(typeof(CubeLightTransitionSystem));
        }
        
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systemList);
        ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

        // ICustomBootstrap runs for all scenes in a project, so to gate this sample to a particular scene, we have to use this hack that checks the active scene's name.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VirtualMemory")
            EntityManager.TotalChunkAddressSpaceInBytes = 1024UL * 1024UL * 16UL;

        return true;
    }
}
