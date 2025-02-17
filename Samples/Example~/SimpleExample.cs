using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Services.Core;
using UnityEngine;

struct UnityServicesActive : IComponentData { }

//This is an example of  simple task using the TaskSystem

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class UnityServicesInitSystem : TaskSystem
{
    //A simple tag component necessary for differentiating
    //Each TaskSystem needs a unique component type here
    private struct UnityServicesFlag : IComponentData { }

    //We should initiate the task as soon as we start running
    protected override bool AutoCreateTask => true;

    protected override ComponentType FlagType => ComponentType.ReadWrite<UnityServicesFlag>();

    //We do not wait on any other components to be present before starting the task
    protected override ComponentType[] RequireForUpdate => Array.Empty<ComponentType>();

    protected override Task Setup(EntityManager em, Entity entity)
    {
        //note that we simply return the task that Unity Services provides
        //We are not messing with creating a new Component that contains a Task and assigning it to the entity

        //If InitializeAsync() returned something more complex like a Task<ResultInfo> we would need to create a new Component to house that result
        //See the other example in this package
        return UnityServices.InitializeAsync();
    }

    protected override void OnTaskComplete(EntityManager em, Entity entity, Task result)
    {
        Debug.Log("Unity Services initialized successfully");

        //Mark that unity services is active so the other systems can awaken
        em.AddComponentData(em.CreateEntity(), new UnityServicesActive());
    }

    protected override void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception)
    {
        Debug.LogError($"Unity Services initialization failed: {exception}");
    }

    protected override void OnSystemUpdate() { }

}
