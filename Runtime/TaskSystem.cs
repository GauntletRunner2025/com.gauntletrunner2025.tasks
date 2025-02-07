using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract partial class TaskSystem : SystemBase {

    protected abstract void OnTaskComplete(EntityManager em, Entity entity);
    protected abstract void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception);

    //REquire that derivative systems supply a ComponentType pointing to their personal Task type
    //Otherwise storing the Task in the base class (this) would not differentiate between them, eg UnityServices sign in and Auth sign in
    //BUt we still need the component type of it to query for it

    //This is defined in the derived class and is how we differentiate between different Tasks
    //They are all the same, so when they are done, we filter out ours by the presence of this type
    protected abstract ComponentType FlagType { get; }

    //an abstract array of component types that we will pass to RequireForUpdate
    protected abstract ComponentType[] RequiredForUpdate { get; }

    protected abstract bool Setup(EntityManager em, Entity entity, Task task);

    EntityQuery Query;

    sealed protected override void OnCreate() {
        Query = GetEntityQuery(typeof(Task), FlagType);

        foreach (var c in RequiredForUpdate) {
            RequireForUpdate(GetEntityQuery(RequiredForUpdate));
        }
    }

    protected sealed override void OnStartRunning() {
        var task = new Task();
        var e = EntityManager.CreateEntity();
        if (!Setup(EntityManager, e, task)) {
            //For some reason the system didn't want to
            EntityManager.DestroyEntity(e);
            return;
        }

        EntityManager.AddComponentData(e, task);
        EntityManager.AddComponent(e, FlagType);
    }

    protected class Task : IComponentData, IEnableableComponent {
        public System.Threading.Tasks.Task Value;
    }

    protected override void OnUpdate() {

        //We should separate out the logic for watching status into a different system
        //And update it wit TaskComplete flags etc
        //TODO

        //Its going to be diffcult to remember not to override OnUpdate() in the base

        if (Query.CalculateEntityCount() == 0)
            return;

        using var entities = Query.ToEntityArray(Allocator.TempJob);

        foreach (var e in entities) {

            var item = EntityManager.GetComponentData<Task>(e);
            if (!item.Value.IsCompleted)
                continue;

            if (item.Value.IsFaulted || item.Value.IsCanceled || !item.Value.IsCompletedSuccessfully) {
                OnTaskFailed(EntityManager, e, item.Value.Exception);
                EntityManager.SetComponentEnabled<Task>(e, false);
                continue;
            }

            EntityManager.RemoveComponent<Task>(e);

            OnTaskComplete(EntityManager, e);
        }
    }
}