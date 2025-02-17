using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Threading.Tasks;
using VideoKit;
using System;
using static VideoKit.MediaDevice;


//This example immediately asks for camera permissions, which is asynchronous

public partial class ExampleTask : TaskSystem
{

    //Should a task be created OnStartRunning() ? 
    //This still waits for any component types in RequireForUpdate[]
    protected override bool AutoCreateTask => true;

    class Flag : IComponentData
    {
        public Task<VideoKit.MediaDevice.PermissionStatus> Value;
    }

    //The permissions request has a custom payload type, so we need to create a new Component to house it 
    //Notice our inner class Flag contains a Task that delivers the permissions result
    protected override ComponentType FlagType => typeof(Flag);

    //All these components must be present (thought not all on the same entity) before this system starts running
    protected override ComponentType[] RequireForUpdate => new ComponentType[] { };

    //Can do things here every update like look for new situations we need to span a task for 
    protected override void OnSystemUpdate()
    {
        using EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // foreach (var (item, entity) in SystemAPI
        //     .Query<SomeComponent>()
        //     //.WithAll<>()
        //     //.WithNone<>()
        //     .WithEntityAccess())
        // {
        //     //Debug.Log($"[{this.GetType().Name}] {entity}");
        //      
        // }

        //If you create a new task in OnSystemUpdate() just remember to also but a base class Task component on the entity as well
        ecb.Playback(EntityManager);
    }

    protected override void OnTaskComplete(EntityManager em, Entity entity, Task result)
    {
        //The task is complete, not necessarily successful
        Debug.Log("Success");
    }

    protected override void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception)
    {
        Debug.LogError("Task failed: " + exception.Message);
    }

    protected override Task Setup(EntityManager em, Entity entity)
    {
        //Create an instance of the component that houses the task
        var taskComponent = new Flag
        {
            //actually instantiate and kick off the task
            Value = CameraDevice.CheckPermissions(request: true)
        };

        //The component gets added to the entity
        em.AddComponentData(entity, taskComponent);

        //Return the task the base system will monitor
        return taskComponent.Value;
    }
}
