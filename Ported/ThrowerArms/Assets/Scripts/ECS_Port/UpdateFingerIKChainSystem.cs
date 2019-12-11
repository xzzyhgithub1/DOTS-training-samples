﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class UpdateFingerIKChainSystem : JobComponentSystem
{
    private EntityQuery m_positionBufferQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        m_positionBufferQuery = 
            GetEntityQuery(ComponentType.ReadOnly<FingerJointPositionBuffer>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entity bufferSingleton = m_positionBufferQuery.GetSingletonEntity();
        var positions = EntityManager.GetBuffer<FingerJointPositionBuffer>(bufferSingleton);

        float time = UnityEngine.Time.time + TimeConstants.Offset;
        
        return Entities.WithName("UpdateFingerIKChain").ForEach((in ArmComponent arm, in Translation translation) =>
        {
            float grabTimer = 
                3f * arm.ReachTimer * arm.ReachTimer - 2f * arm.ReachTimer * arm.ReachTimer * arm.ReachTimer;
            
            for (int finger = 1; finger <= FingerConstants.CountPerArm; finger++)
            {
                float3 position =
                    translation.Value +
                    arm.HandRight *
                    (FingerConstants.XOffset + finger * FingerConstants.Spacing);
                float3 target = (position + arm.HandForward * (0.5f - 0.1f * grabTimer)) 
                                + (arm.HandUp * math.sin((time + finger * 0.2f) * 3f) * 0.2f * (1f - grabTimer));

                float3 rockFingerDelta = target - arm.LastIntendedRockPosition;
                float3 rockFingerPosition = arm.LastIntendedRockPosition + math.normalize(rockFingerDelta) * (arm.LastIntendedRockPosition * 0.5f + FingerConstants.Thickness);
                
                target = math.lerp(target, rockFingerPosition, grabTimer) + ((arm.HandUp * 0.3f + arm.HandForward * 0.1f + arm.HandRight * (finger - 1.5f) * 0.1f) * arm.OpenPalm);
                
                int lastIndex = 
                    (int) (translation.Value.x * FingerConstants.TotalChainCount + (finger * FingerConstants.PerFingerChainCount - 1));
                
                positions[lastIndex] = target;

                int firstIndex = lastIndex - FingerConstants.PerFingerChainCount + 1;

                for (int i = lastIndex - 1; i >= firstIndex; i--)
                {
                    positions[i] += arm.HandUp * FingerConstants.BendStrength;
                    float3 delta = positions[i].Value - positions[i + 1].Value;
                    positions[i] = positions[i + 1] + math.normalize(delta) * FingerConstants.BoneLengths[finger - 1];
                }

                positions[firstIndex] = position;

                for (int i = firstIndex + 1; i <= lastIndex; i++)
                {
                    float3 delta = positions[i].Value - positions[i - 1].Value;
                    positions[i] = positions[i - 1] + math.normalize(delta) * FingerConstants.BoneLengths[finger - 1];
                }
            }
        }).Schedule(inputDeps);
    }
}