using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DOTSAuthoring
{
    #region Components

    public enum HerdState : byte
    {
        Idle,
        Wander,
        Flee
    }

    public struct HerdGroupCache
    {
        public Entity GroupEntity;
        public float2 SharedTarget;
        public float2 FleeOrigin;
        public float2 FleeDirection;
        public float TriggerRadius;
    }

    public struct HerdAgent : IComponentData
    {
        public int HerdId;
    }

    public struct HerdGroup : IComponentData
    {
        public int HerdId;
    }

    public struct HerdStateMachine : IComponentData
    {
        public HerdState Current;
        public HerdState Previous;
        public float TimeInState;
    }

    public struct HerdDesiredDirection : IComponentData
    {
        public float2 Value;
    }

    public struct HerdVelocity2D : IComponentData
    {
        public float2 Value;
    }

    public struct HerdSharedTarget : IComponentData
    {
        public float2 Value;
    }

    public struct HerdFleeData : IComponentData
    {
        public float2 Direction;
        public float2 Origin;
        public float TriggerRadius;
    }

    public struct HerdAgentSettings : IComponentData
    {
        public float NeighborRadius;
        public float SeparationRadius;
        public float CellSize;
        public float ReachedDistance;
        public float HerdJoinDistance;
    }

    public struct HerdSteeringWeights : IComponentData
    {
        public float Cohesion;
        public float Alignment;
        public float Separation;
        public float Target;
        public float Avoidance;
        public float Containment;
    }

    public struct HerdBehaviorTimings : IComponentData
    {
        public float IdleDuration;
        public float FleeDuration;
        public float WanderRadius;
    }

    public struct HerdRandom : IComponentData
    {
        public Random Value;
    }

    public struct HerdWorldBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
        public float2 Margin;
    }

    #endregion

    #region SpatialHashUtility

    public static class HerdSpatialHashUtility
    {
        public static int2 WorldToCell(float2 position, float cellSize)
        {
            return (int2)math.floor(position / cellSize);
        }

        public static int Hash(int2 cell)
        {
            return cell.x * 73856093 ^ cell.y * 19349663;
        }
    }

    #endregion

    #region HerdStateTransitionSystem

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HerdStateTransitionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HerdWorldBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            var scareTriggered = false;
            var scareOrigin = float2.zero;
            var playerEntity = Entity.Null;

            foreach (var (scareEvent, entity) in
                     SystemAPI.Query<RefRO<PlayerScareEvent>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                playerEntity = entity;

                if (SystemAPI.IsComponentEnabled<PlayerScareEvent>(entity))
                {
                    scareTriggered = true;
                    scareOrigin = scareEvent.ValueRO.Value;
                }

                break;
            }

            foreach (var fsm in SystemAPI.Query<RefRW<HerdStateMachine>>())
            {
                fsm.ValueRW.TimeInState += dt;
            }

            var groupCount = SystemAPI.QueryBuilder().WithAll<HerdGroup, HerdSharedTarget, HerdFleeData>().Build().CalculateEntityCount();
            
            var groupMap = new NativeParallelHashMap<int, HerdGroupCache>(math.max(1, groupCount), Allocator.Temp);

            foreach (var (group, sharedTarget, fleeData, entity) in
                     SystemAPI.Query<RefRO<HerdGroup>, RefRO<HerdSharedTarget>, RefRO<HerdFleeData>>().WithEntityAccess())
            {
                groupMap.TryAdd(group.ValueRO.HerdId, new HerdGroupCache
                {
                    GroupEntity = entity,
                    SharedTarget = sharedTarget.ValueRO.Value,
                    FleeOrigin = fleeData.ValueRO.Origin,
                    FleeDirection = fleeData.ValueRO.Direction,
                    TriggerRadius = fleeData.ValueRO.TriggerRadius
                });
            }

            var fleeLookup = SystemAPI.GetComponentLookup<HerdFleeData>();

            if (scareTriggered)
            {
                var keys = groupMap.GetKeyArray(Allocator.Temp);
                foreach (var herdId in keys)
                {
                    if (!groupMap.TryGetValue(herdId, out var groupData))
                        continue;

                    if (!fleeLookup.HasComponent(groupData.GroupEntity))
                        continue;

                    var fleeRef = fleeLookup.GetRefRW(groupData.GroupEntity);
                    fleeRef.ValueRW.Origin = scareOrigin;

                    groupData.FleeOrigin = scareOrigin;
                    groupMap[herdId]     = groupData;
                }

                keys.Dispose();

                foreach (var (agent, fsm, localToWorld) in
                         SystemAPI.Query<
                             RefRO<HerdAgent>,
                             RefRW<HerdStateMachine>,
                             RefRO<LocalToWorld>>())
                {
                    if (!groupMap.TryGetValue(agent.ValueRO.HerdId, out var groupData))
                        continue;

                    var npcPos = localToWorld.ValueRO.Position.xy;
                    var away = npcPos - groupData.FleeOrigin;
                    var distSq = math.lengthsq(away);

                    if (distSq > groupData.TriggerRadius * groupData.TriggerRadius)
                        continue;

                    if (fsm.ValueRO.Current == HerdState.Flee)
                        continue;

                    fsm.ValueRW.Previous = fsm.ValueRO.Current;
                    fsm.ValueRW.Current = HerdState.Flee;
                    fsm.ValueRW.TimeInState = 0f;
                }

                if (playerEntity != Entity.Null)
                {
                    SystemAPI.SetComponentEnabled<PlayerScareEvent>(playerEntity, false);
                }

                groupMap.Dispose();
                return;
            }

            foreach (var (agent, fsm, timings, settings, localToWorld) in
                     SystemAPI.Query<
                         RefRO<HerdAgent>,
                         RefRW<HerdStateMachine>,
                         RefRO<HerdBehaviorTimings>,
                         RefRO<HerdAgentSettings>,
                         RefRO<LocalToWorld>>())
            {
                switch (fsm.ValueRO.Current)
                {
                    case HerdState.Idle:
                        if (fsm.ValueRO.TimeInState >= timings.ValueRO.IdleDuration)
                        {
                            fsm.ValueRW.Previous = fsm.ValueRO.Current;
                            fsm.ValueRW.Current = HerdState.Wander;
                            fsm.ValueRW.TimeInState = 0f;
                        }

                        break;
                    
                    case HerdState.Wander:
                        if (!groupMap.TryGetValue(agent.ValueRO.HerdId, out var groupData))
                            break;

                        var position = localToWorld.ValueRO.Position.xy;
                        var reachedDistance = settings.ValueRO.ReachedDistance;
                        var toTarget = groupData.SharedTarget - position;

                        if (math.lengthsq(toTarget) <= reachedDistance * reachedDistance)
                        {
                            fsm.ValueRW.Previous = fsm.ValueRO.Current;
                            fsm.ValueRW.Current = HerdState.Idle;
                            fsm.ValueRW.TimeInState = 0f;
                        }

                        break;

                    case HerdState.Flee:
                        var myPos  = localToWorld.ValueRO.Position.xy;
                        var herdId = agent.ValueRO.HerdId;

                        var joinDistance = settings.ValueRO.HerdJoinDistance;   // distancia mínima al rebaño
                        var center = float2.zero;
                        var count = 0;

                        // Centro global del rebaño: todos los agentes con el mismo HerdId,
                        // sin filtrar por NeighborRadius.
                        foreach (var (otherAgent, otherTransform) in
                                 SystemAPI.Query<RefRO<HerdAgent>, RefRO<LocalToWorld>>())
                        {
                            if (otherAgent.ValueRO.HerdId != herdId)
                                continue;

                            center += otherTransform.ValueRO.Position.xy;
                            count++;
                        }

                        var isJoined = false;

                        if (count > 0)
                        {
                            center /= count;
                            var toCenter = center - myPos;
                            var distSq   = math.lengthsq(toCenter);

                            // "Estoy en el rebaño" = estoy dentro de joinDistance del centro global.
                            if (distSq <= joinDistance * joinDistance)
                            {
                                isJoined = true;
                            }
                        }

                        // Condición de salida de Flee:
                        // ha pasado el tiempo de huida Y estoy en el rebaño.
                        if (fsm.ValueRO.TimeInState >= timings.ValueRO.FleeDuration && isJoined)
                        {
                            fsm.ValueRW.Previous   = fsm.ValueRO.Current;
                            fsm.ValueRW.Current    = HerdState.Wander; // o Wander si quieres enganchar al patrón del grupo
                            fsm.ValueRW.TimeInState = 0f;
                        }

                        break;
                }
            }

            groupMap.Dispose();
        }
    }
    #endregion

    #region HerdWanderTargetSystem

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HerdStateTransitionSystem))]
    public partial struct HerdWanderTargetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HerdWorldBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<HerdWorldBounds>())
                return;

            var bounds = SystemAPI.GetSingleton<HerdWorldBounds>();

            var groupCount = SystemAPI.QueryBuilder()
                .WithAll<HerdGroup, HerdSharedTarget, HerdFleeData>()
                .Build()
                .CalculateEntityCount();

            var groupMap = new NativeParallelHashMap<int, HerdGroupCache>(
                math.max(1, groupCount),
                Allocator.Temp);

            foreach (var (group, sharedTarget, fleeData, entity) in
                     SystemAPI.Query<RefRO<HerdGroup>, RefRO<HerdSharedTarget>, RefRO<HerdFleeData>>()
                         .WithEntityAccess())
            {
                groupMap.TryAdd(group.ValueRO.HerdId, new HerdGroupCache
                {
                    GroupEntity = entity,
                    SharedTarget = sharedTarget.ValueRO.Value,
                    FleeOrigin = fleeData.ValueRO.Origin,
                    FleeDirection = fleeData.ValueRO.Direction,
                    TriggerRadius = fleeData.ValueRO.TriggerRadius
                });
            }

            var processedHerds = new NativeParallelHashSet<int>(math.max(1, groupCount), Allocator.Temp);
            var sharedTargetLookup = SystemAPI.GetComponentLookup<HerdSharedTarget>(false);

            foreach (var (agent, fsm, timings, localToWorld, rnd) in
                     SystemAPI.Query<
                             RefRO<HerdAgent>,
                             RefRO<HerdStateMachine>,
                             RefRO<HerdBehaviorTimings>,
                             RefRO<LocalToWorld>,
                             RefRW<HerdRandom>>())
            {
                if (fsm.ValueRO.Current != HerdState.Wander || fsm.ValueRO.TimeInState > 0.05f)
                    continue;

                var herdId = agent.ValueRO.HerdId;

                if (!processedHerds.Add(herdId))
                    continue;

                if (!groupMap.TryGetValue(herdId, out var groupData))
                    continue;

                var random = rnd.ValueRW.Value;

                var angle = random.NextFloat(0f, math.PI * 2f);
                var radius = random.NextFloat(0.75f, timings.ValueRO.WanderRadius);

                var origin = localToWorld.ValueRO.Position.xy;
                var candidate = origin + new float2(math.cos(angle), math.sin(angle)) * radius;

                var min = bounds.Min + bounds.Margin;
                var max = bounds.Max - bounds.Margin;

                var targetMargin = math.max(0.5f, timings.ValueRO.WanderRadius * 0.25f);

                var targetMin = min + new float2(targetMargin, targetMargin);
                var targetMax = max - new float2(targetMargin, targetMargin);

                if (targetMin.x > targetMax.x)
                {
                    targetMin.x = min.x;
                    targetMax.x = max.x;
                }

                if (targetMin.y > targetMax.y)
                {
                    targetMin.y = min.y;
                    targetMax.y = max.y;
                }

                candidate = math.clamp(candidate, targetMin, targetMax);

                if (sharedTargetLookup.HasComponent(groupData.GroupEntity))
                {
                    sharedTargetLookup.GetRefRW(groupData.GroupEntity).ValueRW.Value = candidate;
                }

                rnd.ValueRW.Value = random;
            }

            processedHerds.Dispose();
            groupMap.Dispose();
        }
    }

    #endregion

    #region HerdSteeringSystem

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HerdWanderTargetSystem))]
    public partial struct HerdSteeringSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<HerdWorldBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<HerdWorldBounds>())
                return;

            var bounds = SystemAPI.GetSingleton<HerdWorldBounds>();
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            var query = SystemAPI.QueryBuilder()
                .WithAll<
                    HerdAgent,
                    HerdStateMachine,
                    HerdAgentSettings,
                    HerdSteeringWeights,
                    HerdDesiredDirection,
                    CharacterMoveDirection,
                    LocalToWorld>()
                .WithAll<HerdBehaviorTimings>()
                .Build();

            var count = query.CalculateEntityCount();
            if (count == 0)
                return;

            var entities = query.ToEntityArray(Allocator.Temp);
            var agents = query.ToComponentDataArray<HerdAgent>(Allocator.Temp);
            var states = query.ToComponentDataArray<HerdStateMachine>(Allocator.Temp);
            var settings = query.ToComponentDataArray<HerdAgentSettings>(Allocator.Temp);
            var weights = query.ToComponentDataArray<HerdSteeringWeights>(Allocator.Temp);
            var timings = query.ToComponentDataArray<HerdBehaviorTimings>(Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            var desireDirection = query.ToComponentDataArray<HerdDesiredDirection>(Allocator.Temp);

            var groupCount = SystemAPI.QueryBuilder().WithAll<HerdGroup, HerdSharedTarget, HerdFleeData>().Build().CalculateEntityCount();
            var groupMap = new NativeParallelHashMap<int, HerdGroupCache>(math.max(1, groupCount), Allocator.Temp);

            foreach (var (group, sharedTarget, fleeData, entity) in
                     SystemAPI.Query<RefRO<HerdGroup>, RefRO<HerdSharedTarget>, RefRO<HerdFleeData>>()
                         .WithEntityAccess())
            {
                groupMap.TryAdd(group.ValueRO.HerdId, new HerdGroupCache
                {
                    GroupEntity = entity,
                    SharedTarget = sharedTarget.ValueRO.Value,
                    FleeOrigin = fleeData.ValueRO.Origin,
                    FleeDirection = fleeData.ValueRO.Direction,
                    TriggerRadius = fleeData.ValueRO.TriggerRadius
                });
            }

            var cellSize = settings[0].CellSize;
            var cellMap = new NativeParallelMultiHashMap<int, int>(count, Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                var pos = transforms[i].Position.xy;
                var cell = HerdSpatialHashUtility.WorldToCell(pos, cellSize);
                cellMap.Add(HerdSpatialHashUtility.Hash(cell), i);
            }

            var desiredLookup = SystemAPI.GetComponentLookup<HerdDesiredDirection>();
            var moveLookup = SystemAPI.GetComponentLookup<CharacterMoveDirection>();

            
            for (int i = 0; i < count; i++)
            {
                var herdId = agents[i].HerdId;
                var myPos = transforms[i].Position.xy;
                var currentState = states[i].Current;
                var mySettings = settings[i];
                var myWeights = weights[i];

                var separation = float2.zero;
                var alignment = float2.zero;
                var cohesion = float2.zero;
                var cohesionAccumulator = float2.zero;
                var avoidance = float2.zero;
                var neighborCount = 0;
                
                var targetWeight      = myWeights.Target;
                var cohesionWeight    = myWeights.Cohesion;
                var alignmentWeight   = myWeights.Alignment;
                var separationWeight  = myWeights.Separation;
                var containmentWeight = myWeights.Containment;

                var myCell = HerdSpatialHashUtility.WorldToCell(myPos, mySettings.CellSize);

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        var cell = myCell + new int2(ox, oy);
                        var hash = HerdSpatialHashUtility.Hash(cell);

                        if (!cellMap.TryGetFirstValue(hash, out int otherIndex, out var iterator))
                            continue;

                        do
                        {
                            if (otherIndex == i)
                                continue;

                            if (agents[otherIndex].HerdId != herdId)
                                continue;

                            var otherPos = transforms[otherIndex].Position.xy;
                            var toOther = otherPos - myPos;
                            var distSq = math.lengthsq(toOther);

                            if (distSq > mySettings.NeighborRadius * mySettings.NeighborRadius)
                                continue;

                            neighborCount++;
                            cohesionAccumulator += otherPos;
                            alignment += desireDirection[otherIndex].Value;

                            if (distSq < mySettings.SeparationRadius * mySettings.SeparationRadius && distSq > 0.0001f)
                            {
                                separation -= toOther / math.max(distSq, 0.0001f);
                            }
                        }
                        while (cellMap.TryGetNextValue(out otherIndex, ref iterator));
                    }
                }

                if (neighborCount > 0)
                {
                    var center = cohesionAccumulator / neighborCount;
                    cohesion = math.normalizesafe(center - myPos);
                    alignment = math.normalizesafe(alignment / neighborCount);
                    separation = math.normalizesafe(separation);
                }
                
                var herdCenter = float2.zero;
                var herdCount  = 0;

                for (int j = 0; j < count; j++)
                {
                    if (agents[j].HerdId != herdId)
                        continue;

                    herdCenter += transforms[j].Position.xy;
                    herdCount++;
                }

                if (herdCount > 0)
                {
                    herdCenter /= herdCount;
                }

                var timeInState  = states[i].TimeInState;
                var fleeDuration = timings[i].FleeDuration;

                var targetDir = float2.zero;
                var containmentMultiplier = 1f;

                if (groupMap.TryGetValue(herdId, out var groupData))
                {
                    switch (currentState)
                    {
                        case HerdState.Idle:
                            targetDir = float2.zero;
                            break;

                        case HerdState.Wander:
                            targetDir = math.normalizesafe(groupData.SharedTarget - myPos);
                            break;

                        case HerdState.Flee:
                            // Fase 1: antes de que termine FleeDuration, huye del origen del susto.
                            if (timeInState < fleeDuration)
                            {
                                
                                // Usa el origen del susto almacenado en el grupo para huir individualmente.
                                targetDir = math.normalizesafe(myPos - groupData.FleeOrigin);
                            }
                            else
                            {
                                // Fase 2: una vez ha pasado FleeDuration, la flee consiste en
                                // volver hacia el centro global del rebaño.
                                if (herdCount > 0)
                                {
                                    targetDir = math.normalizesafe(herdCenter - myPos);
                                }
                                else
                                {
                                    // Si por lo que sea no hay nadie, no hay target claro.
                                    targetDir = math.normalizesafe(myPos - groupData.FleeOrigin);
                                }
                            }
                            
                            Debug.Log(targetDir);

                            containmentMultiplier = 2f;
                            break;
                    }
                }
                
                if (currentState == HerdState.Flee)
                {
                    if (timeInState < fleeDuration)
                    {
                        // FASE 1: huida pura del susto.
                        // Sólo Target (huir de FleeOrigin) manda; cohesión y alineamiento casi anulados.
                        cohesionWeight    = 0f;
                        alignmentWeight   = 0f;
                        // separationWeight  = myWeights.Separation * 0.5f;
                        // containmentWeight = myWeights.Containment * 0.5f; // opcional para controlar bordes
                    }
                    else
                    {
                        // FASE 2: vuelta al rebaño.
                        // Se permite cohesión/alineamiento para reagrupar, Target apunta al herdCenter.
                        cohesionWeight    = myWeights.Cohesion;
                        alignmentWeight   = myWeights.Alignment;
                        // separationWeight  = myWeights.Separation;
                        // containmentWeight = myWeights.Containment;
                    }
                }

                var containment = ComputeContainmentDirection(myPos, bounds, 2f);
                
                // Solo hacemos raycast si tenemos alguna intención de movimiento (Flee/Wander)
                if (currentState != HerdState.Idle)
                {
                    // Usa la dirección principal para el raycast: targetDir si existe, si no, finalDir provisional.
                    var rayDir = targetDir;
                    if (math.lengthsq(rayDir) < 0.0001f)
                    {
                        // Si no hay targetDir (por ejemplo, Flee pero muy cerca del origen),
                        // usamos la suma de coh/coh/separation como dirección base.
                        rayDir = cohesion + separation + alignment;
                    }

                    rayDir = math.normalizesafe(rayDir);

                    if (math.lengthsq(rayDir) > 0.0001f)
                    {
                        var rayLength = mySettings.NeighborRadius; // alcance razonable
                        var start = new float3(myPos.x, myPos.y, 0f);
                        var end = start + new float3(rayDir.x, rayDir.y, 0f) * rayLength;

                        var defaultLayer = 1u << 0; // capa 0: Default

                        var rayInput = new RaycastInput
                        {
                            Start  = start,
                            End    = end,
                            Filter = new CollisionFilter
                            {
                                BelongsTo    = ~0u,        // o la capa a la que “pertenece” tu rayo
                                CollidesWith = defaultLayer,
                                GroupIndex   = 0
                            }
                        };

                        if (physicsWorld.CastRay(rayInput, out var hit))
                        {
                            // Vector de evitación: alejarnos del obstáculo según su normal.
                            var normal2D = new float2(hit.SurfaceNormal.x, hit.SurfaceNormal.y);

                            // Empuja en la dirección de la normal (hacia fuera del obstáculo),
                            // y ligeramente desplazado para rodear en vez de rebotar frontalmente.
                            avoidance = math.normalizesafe(normal2D);
                        }
                    }
                }

                var finalDir =
                    targetDir    * targetWeight +
                    cohesion     * cohesionWeight +
                    alignment    * alignmentWeight +
                    separation   * separationWeight +
                    containment  * containmentWeight * containmentMultiplier +
                    avoidance * myWeights.Avoidance;

                // En Idle no queremos que el steering genere movimiento.
                // Forzamos dirección cero para que el personaje se quede parado.
                finalDir = currentState == HerdState.Idle ? float2.zero : math.normalizesafe(finalDir);

                desiredLookup[entities[i]] = new HerdDesiredDirection
                {
                    Value = finalDir
                };

                moveLookup[entities[i]] = new CharacterMoveDirection
                {
                    Value = finalDir
                };
            }

            groupMap.Dispose();
            cellMap.Dispose();
            entities.Dispose();
            agents.Dispose();
            states.Dispose();
            settings.Dispose();
            weights.Dispose();
            timings.Dispose();
            transforms.Dispose();
            desireDirection.Dispose();
        }

        private static float2 ComputeContainmentDirection(float2 position, HerdWorldBounds bounds, float falloffDistance)
        {
            var minX = bounds.Min.x + bounds.Margin.x;
            var maxX = bounds.Max.x - bounds.Margin.x;
            var minY = bounds.Min.y + bounds.Margin.y;
            var maxY = bounds.Max.y - bounds.Margin.y;

            var dir = float2.zero;

            if (position.x < minX + falloffDistance)
                dir.x += math.saturate((minX + falloffDistance - position.x) / falloffDistance);

            if (position.x > maxX - falloffDistance)
                dir.x -= math.saturate((position.x - (maxX - falloffDistance)) / falloffDistance);

            if (position.y < minY + falloffDistance)
                dir.y += math.saturate((minY + falloffDistance - position.y) / falloffDistance);

            if (position.y > maxY - falloffDistance)
                dir.y -= math.saturate((position.y - (maxY - falloffDistance)) / falloffDistance);

            return math.normalizesafe(dir);
        }
    }
    #endregion

    #region HerdBoundsClampSystem

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterMoveSystem))]
    public partial struct HerdBoundsClampSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HerdWorldBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<HerdWorldBounds>())
                return;

            var bounds = SystemAPI.GetSingleton<HerdWorldBounds>();

            var min = bounds.Min + bounds.Margin;
            var max = bounds.Max - bounds.Margin;

            foreach (var (transform, velocity, fsm) in
                     SystemAPI.Query<
                             RefRW<LocalTransform>,
                             RefRW<PhysicsVelocity>,
                             RefRW<HerdStateMachine>>()
                         .WithAll<HerdAgent>())
            {
                var pos = transform.ValueRO.Position;

                var clampedX = math.clamp(pos.x, min.x, max.x);
                var clampedY = math.clamp(pos.y, min.y, max.y);

                const float epsilon = 0.01f;
                const float tinySpeed = 0.05f; // velocidad casi nula
                const float minBlockedTime = 0.25f; // tiempo mínimo en Wander antes de considerar bloqueo

                var linear = velocity.ValueRO.Linear;

                // ¿está fuera del área de juego?
                var outsideX = math.abs(pos.x - clampedX) > epsilon;
                var outsideY = math.abs(pos.y - clampedY) > epsilon;

                // ¿está muy cerca del borde?
                var nearXEdge = math.abs(pos.x - clampedX) <= epsilon;
                var nearYEdge = math.abs(pos.y - clampedY) <= epsilon;

                // ¿está empujando hacia fuera?
                var pushingLeft  = pos.x <= min.x + epsilon && linear.x < 0f;
                var pushingRight = pos.x >= max.x - epsilon && linear.x > 0f;
                var pushingDown  = pos.y <= min.y + epsilon && linear.y < 0f;
                var pushingUp    = pos.y >= max.y - epsilon && linear.y > 0f;

                // ¿está pegado al borde con velocidad casi nula **y** lleva un rato en Wander?
                var stalledOnXEdge = nearXEdge &&
                                     math.abs(linear.x) <= tinySpeed &&
                                     fsm.ValueRO.Current == HerdState.Wander &&
                                     fsm.ValueRO.TimeInState > minBlockedTime;

                var stalledOnYEdge = nearYEdge &&
                                     math.abs(linear.y) <= tinySpeed &&
                                     fsm.ValueRO.Current == HerdState.Wander &&
                                     fsm.ValueRO.TimeInState > minBlockedTime;

                // tratamos como “hit” tanto empujar contra el borde,
                // como estar pegado al borde sin poder avanzar
                var hitX = outsideX || pushingLeft || pushingRight || stalledOnXEdge;
                var hitY = outsideY || pushingDown || pushingUp || stalledOnYEdge;

                if (!hitX && !hitY)
                    continue;

                pos.x = clampedX;
                pos.y = clampedY;
                transform.ValueRW.Position = pos;

                if (hitX)
                    linear.x = 0f;

                if (hitY)
                    linear.y = 0f;

                velocity.ValueRW.Linear = linear;

                if (fsm.ValueRO.Current != HerdState.Wander) continue;
                
                fsm.ValueRW.Previous = fsm.ValueRO.Current;
                fsm.ValueRW.Current = HerdState.Idle;
                fsm.ValueRW.TimeInState = 0f;
            }
        }
    }

    #endregion
}