using HarmonyLib;
using RaftMMO.Utilities;
using RaftMMO.World;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.RaftCopyTools
{
    public class RaftCopier
    {
        private static SO_PipeColliderInfo[] pipeColliderInfos = null;
        private static PhysicMaterial defaultColliderMaterial = null;

        public static SerializableData.RaftData CreateRaftData()
        {
            var raft = ComponentManager<Raft>.Value;

            var backupRaftPosition = raft.transform.position;
            var backupRaftRotation = raft.transform.rotation;
            raft.transform.position = Vector3.zero;
            raft.transform.rotation = Quaternion.identity;

            var blockData = raft.GetComponentsInChildren<Block>().Where(IsBlockForSending).Select(block => new SerializableData.RaftBlockData(block)).ToArray();

            raft.transform.position = backupRaftPosition;
            raft.transform.rotation = backupRaftRotation;

            return new SerializableData.RaftData(blockData);
        }

        public static bool IsColliderForSending(Collider collider)
        {
            if (!collider.enabled || (collider.isTrigger && collider.tag != "Ladder"))
                return false;

            if (!IsBlockForSending(collider.GetComponentInParent<Block>()))
                return false;

            if (collider is BoxCollider boxCollider)
            {
                return boxCollider.size.x > 0f && boxCollider.size.y > 0f && boxCollider.size.z > 0f;
            }

            if (collider is SphereCollider sphereCollider)
            {
                return sphereCollider.radius > 0f;
            }

            return collider is MeshCollider;
        }

        private static bool IsBlockForSending(Block block)
        {
            if (block == null)
                return false;

            if (block.buildableItem == null)
                return false;

            Network_Player localPlayer = ComponentManager<Raft_Network>.Value.GetLocalPlayer();
            if (block == localPlayer.BlockCreator.selectedBlock)
                return false;

            var fieldInfo = typeof(BlockCreator).GetField("selectedBuildablePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            if (block == fieldInfo.GetValue(localPlayer.BlockCreator) as Block)
                return false;

            return true;
        }


        public static void RestoreRaftData(SerializableData.RaftData raftData, GameObject remoteRaft,
            Dictionary<SerializableData.RaftBlockData, GameObject> blockCache, out bool alreadyhadthethingerror)
        {
            alreadyhadthethingerror = false;
            RestoreBlocks(remoteRaft, raftData.blockData, blockCache, ref alreadyhadthethingerror);
        }

        private static void RestoreColliders(GameObject blockObject, Block blockPrefab, IEnumerable<SerializableData.RaftColliderData> colliderData)
        {
            if (defaultColliderMaterial == null)
            {
                defaultColliderMaterial = ComponentManager<Raft>.Value.GetComponentInChildren<Collider>().sharedMaterial;
            }

            foreach (var collider in colliderData)
            {
                var colliderObject = CopyCollider(collider, blockPrefab, defaultColliderMaterial);
                colliderObject.transform.SetParent(blockObject.transform, false);
            }
        }

        private static void RestorePlants(GameObject blockObject, IEnumerable<SerializableData.RaftPlantData> plantData)
        {
            foreach (var plant in plantData)
            {
                GameObject plantObject = new GameObject();

                var localPlayer = ComponentManager<Raft_Network>.Value.GetLocalPlayer();
                var plantPrefab = localPlayer.PlantManager.GetPlantByIndex(plant.plantUniqueItemIndex);
                if (plantPrefab != null)
                {
                    foreach (var meshRenderer in plantPrefab.GetComponentsInChildren<MeshRenderer>())
                    {
                        var plantMeshObject = CopyMesh(meshRenderer, TileBitmaskType.All, 0);
                        plantMeshObject.transform.position = plant.position.Vector3;
                        plantMeshObject.transform.rotation = Quaternion.Euler(plant.rotation.Vector3);
                        plantMeshObject.transform.localScale = plant.scale.Vector3;
                        plantMeshObject.transform.SetParent(plantObject.transform, true);
                    }
                }

                plantObject.transform.SetParent(blockObject.transform, false);
            }
        }

        private static void RestoreBlocks(GameObject remoteRaft, 
            SerializableData.RaftBlockData[] blockData,
            Dictionary<SerializableData.RaftBlockData, GameObject> blockCache,
            ref bool alreadyhadthethingerror)
        {
            var lights = new List<LightSingularityExternal>();

            foreach (var block in blockData)
            {
                if (block == null)
                {
                    RaftMMOLogger.LogVerbose("RaftCopier.RestoreBlocks: Received null block");
                    continue;
                }

                GameObject blockObject = new GameObject();

                try
                {
                    var blockPrefab = ItemManager.GetItemByIndex(block.itemIndex)?.settings_buildable?.GetBlockPrefab((DPS)block.dpsType);
                    if (blockPrefab != null)
                    {
                        blockPrefab.transform.position = block.position.Vector3;
                        blockPrefab.transform.rotation = Quaternion.Euler(block.rotation.Vector3);

                        var cookItems = blockPrefab.GetComponentsInChildren<CookingSlot>()
                            .Select(cookingSlot => Traverse.Create(cookingSlot).Field("itemConnections").GetValue<List<CookItemConnection>>())
                            .SelectMany(cookingConnetion => cookingConnetion)
                            .Where(cookingConnetion => cookingConnetion != null)
                            .Select(cookingConnetion => new GameObject[] { cookingConnetion.rawItem, cookingConnetion.cookedItem })
                            .SelectMany(item => item)
                            .Where(item => item != null);

                        foreach (var renderer in blockPrefab.GetComponentsInChildren<Renderer>())
                        {
                            // Skip meshes for cook items
                            if (cookItems.Where(cookItem => renderer.transform.IsChildOf(cookItem.transform)).Any())
                                continue;

                            if (renderer is MeshRenderer meshRenderer)
                            {
                                CopyMesh(meshRenderer, block.bitmaskType, block.bitmaskValue).transform.SetParent(blockObject.transform, true);
                            }
                            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                            {
                                CopySkinnedMesh(skinnedMeshRenderer).transform.SetParent(blockObject.transform, true);
                            }
                            else if (renderer is ParticleSystemRenderer particleSystemRenderer)
                            {
                                CopyParticles(particleSystemRenderer).transform.SetParent(blockObject.transform, true);
                            }
                        }

                        if (block.HasColorA() || block.HasColorB())
                        {
                            foreach (var renderer in blockObject.GetComponentsInChildren<Renderer>())
                            {
                                var matPropBlock = new MaterialPropertyBlock();
                                renderer.GetPropertyBlock(matPropBlock);

                                if (block.HasColorA())
                                {
                                    matPropBlock.SetColor("_Side1_Base", new Color(block.colorA_r, block.colorA_g, block.colorA_b, block.colorA_a));
                                    matPropBlock.SetColor("_Side1_Pattern", new Color(block.patternColorA_r, block.patternColorA_g, block.patternColorA_b, block.patternColorA_a));
                                    matPropBlock.SetFloat("_Pattern_Index1", block.patternIndexA);
                                    matPropBlock.SetFloat("_Pattern1_Masked", block.isMaskedA ? 1f : 0f);
                                    matPropBlock.SetFloat("_Pattern1_MaskFlip", block.isMaskFlippedA ? 1f : 0f);
                                }

                                if (block.HasColorB())
                                {
                                    matPropBlock.SetColor("_Side2_Base", new Color(block.colorB_r, block.colorB_g, block.colorB_b, block.colorB_a));
                                    matPropBlock.SetColor("_Side2_Pattern", new Color(block.patternColorB_r, block.patternColorB_g, block.patternColorB_b, block.patternColorB_a));
                                    matPropBlock.SetFloat("_Pattern_Index2", block.patternIndexB);
                                    matPropBlock.SetFloat("_Pattern2_Masked", block.isMaskedB ? 1f : 0f);
                                    matPropBlock.SetFloat("_Pattern2_MaskFlip", block.isMaskFlippedB ? 1f : 0f);
                                }

                                matPropBlock.SetFloat("_PaintSide", block.paintSide);
                                matPropBlock.SetFloat("_DecoPaintSelect", block.decoPaintSelect);

                                renderer.SetPropertyBlock(matPropBlock);
                            }
                        }

                        foreach (var lightSingularityExternal in blockPrefab.GetComponentsInChildren<LightSingularityExternal>())
                        {
                            CopyLight(lightSingularityExternal, lights).transform.SetParent(blockObject.transform, true);
                        }
                    }
                    else
                    {
                        RaftMMOLogger.LogVerbose("RaftCopier.RestoreBlocks: Received invalid item index: ", block.itemIndex);
                    }

                    blockObject.transform.SetParent(remoteRaft.transform, false);

                    RestorePlants(blockObject, block.plants);
                    RestoreColliders(blockObject, blockPrefab, block.colliders);
                }
                catch (System.Exception e)
                {
                    RaftMMOLogger.LogVerbose("RaftCopier.RestoreBlocks: Caught exception when restoring block ", block, ": ", e);
                }

                if (blockCache.Remove(block))
                {
                    RaftMMOLogger.LogVerbose("RaftCopier.RestoreBlocks: Error: Block already existed: ", block);
                    alreadyhadthethingerror = true;
                }

                blockCache.Add(block, blockObject);
            }

            foreach (var lightSingularityExternal in lights)
            {
                Traverse.Create(lightSingularityExternal).Field("lightManager").SetValue(LightSingularityPatch.LightSingularityManager);
                Traverse.Create(lightSingularityExternal).Field("hasBeenPlaced").SetValue(true);
                lightSingularityExternal.OnPlaceLight();
            }
        }

        private static GameObject CopyLight(LightSingularityExternal lightSingularityExternal,
            List<LightSingularityExternal> lights)
        {
            var lightObject = lightSingularityExternal.gameObject;

            var newLightObject = new GameObject();

            CopyTransform(newLightObject, lightObject);
            newLightObject.transform.position = lightSingularityExternal.lightOffsetPosition.position;

            var newLightSingularityExternal = newLightObject.AddComponent<LightSingularityExternal>();
            newLightSingularityExternal.lightRange = lightSingularityExternal.lightRange;
            newLightSingularityExternal.intensity = lightSingularityExternal.intensity;
            newLightSingularityExternal.IsSingularity = lightSingularityExternal.IsSingularity;
            newLightSingularityExternal.lightOffsetPosition = newLightObject.transform;
            newLightSingularityExternal.lightColor = lightSingularityExternal.lightColor;

            lights.Add(newLightSingularityExternal);

            return newLightObject;
        }

        private static GameObject CopyParticles(ParticleSystemRenderer particleSystemRenderer)
        {
            var particleObject = particleSystemRenderer.gameObject;

            var newParticleObject = UnityEngine.Object.Instantiate(particleObject);
            newParticleObject.transform.SetParent(null);

            ClearLayers(newParticleObject);

            CopyTransform(newParticleObject, particleObject);

            return newParticleObject;
        }

        private static GameObject CopyCollider(SerializableData.RaftColliderData colliderData, Block blockPrefab, PhysicMaterial defaultMaterial)
        {
            var newColliderObject = new GameObject();

            Collider newCollider;
            switch (colliderData.type)
            {
                case SerializableData.RaftColliderData.ColliderType.BOX:
                    newCollider = newColliderObject.AddComponent<BoxCollider>();
                    (newCollider as BoxCollider).size = colliderData.size.Vector3;
                    (newCollider as BoxCollider).center = colliderData.center.Vector3;
                    break;
                case SerializableData.RaftColliderData.ColliderType.SPHERE:
                    newCollider = newColliderObject.AddComponent<SphereCollider>();
                    (newCollider as SphereCollider).radius = colliderData.size.x;
                    (newCollider as SphereCollider).center = colliderData.center.Vector3;
                    break;
                case SerializableData.RaftColliderData.ColliderType.PIPEMESH:
                    newCollider = newColliderObject.AddComponent<MeshCollider>();
                    InitPipeMeshCollider(newCollider as MeshCollider, colliderData.bitMaskValue);
                    break;
                case SerializableData.RaftColliderData.ColliderType.MESH:
                    newCollider = newColliderObject.AddComponent<MeshCollider>();
                    InitMeshCollider(newCollider as MeshCollider, blockPrefab);
                    break;
                case SerializableData.RaftColliderData.ColliderType.INVALID:
                default:
                    return newColliderObject;
            }

            newCollider.sharedMaterial = defaultMaterial;
            newCollider.material = defaultMaterial;

            newColliderObject.transform.position = colliderData.position.Vector3;
            newColliderObject.transform.rotation = Quaternion.Euler(colliderData.rotation.Vector3);
            newColliderObject.transform.localScale = colliderData.scale.Vector3;
            newColliderObject.layer = LayerMask.NameToLayer("Obstruction");

            if (colliderData.isLadder)
            {
                newCollider.isTrigger = true;
                newCollider.tag = "Ladder";
            }

            return newColliderObject;
        }

        private static void InitMeshCollider(MeshCollider newMeshCollider, Block blockPrefab)
        {
            var meshCollider = blockPrefab.GetComponentInChildren<MeshCollider>();
            if (meshCollider != null)
            {
                newMeshCollider.sharedMesh = meshCollider.sharedMesh;
                newMeshCollider.convex = meshCollider.convex;
                newMeshCollider.cookingOptions = meshCollider.cookingOptions;
            }
        }

        private static void InitPipeMeshCollider(MeshCollider newMeshCollider, int bitmaskValue)
        {
            if (pipeColliderInfos == null)
            {
                pipeColliderInfos = UnityEngine.Resources.FindObjectsOfTypeAll<SO_PipeColliderInfo>();
            }

            if (pipeColliderInfos != null)
            {
                foreach (var pipeColliderInfo in pipeColliderInfos)
                {
                    if (pipeColliderInfo != null)
                    {
                        foreach (var colliderConnection in pipeColliderInfo.pipeColliderConnections)
                        {
                            if (colliderConnection.ContainsBitmaskValue(bitmaskValue))
                            {
                                var meshCollider = colliderConnection.colliderPrefab.GetComponentInChildren<MeshCollider>();
                                if (meshCollider != null)
                                {
                                    newMeshCollider.sharedMesh = meshCollider.sharedMesh;
                                    newMeshCollider.convex = meshCollider.convex;
                                    newMeshCollider.cookingOptions = meshCollider.cookingOptions;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static GameObject CopySkinnedMesh(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var skinnedMeshObject = skinnedMeshRenderer.gameObject.transform.parent.gameObject;

            var newSkinnedMeshObject = UnityEngine.Object.Instantiate(skinnedMeshObject);
            newSkinnedMeshObject.transform.SetParent(null);

            ClearLayers(newSkinnedMeshObject);

            CopyTransform(newSkinnedMeshObject, skinnedMeshObject);

            return newSkinnedMeshObject;
        }

        private static void ClearLayers(GameObject gameObject)
        {
            gameObject.layer = 0;
            foreach (Transform child in gameObject.transform)
            {
                ClearLayers(child.gameObject);
            }
        }

        private static GameObject CopyMesh(MeshRenderer meshRenderer, TileBitmaskType bitmaskType, int bitmaskValue)
        {
            var meshObject = meshRenderer.gameObject;
            var meshFilter = meshObject.GetComponent<MeshFilter>();

            var newMeshObject = new GameObject();

            var newMeshFilter = newMeshObject.AddComponent<MeshFilter>();
            newMeshFilter.mesh = meshFilter.mesh;
            newMeshFilter.sharedMesh = meshFilter.sharedMesh;

            var newMeshRenderer = newMeshObject.AddComponent<MeshRenderer>();

            newMeshRenderer.material = meshRenderer.material;
            newMeshRenderer.sharedMaterial = meshRenderer.sharedMaterial;

            newMeshRenderer.materials = meshRenderer.materials.ToArray();
            newMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials.ToArray();

            if (meshRenderer.HasPropertyBlock())
            {
                for (var i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                {
                    MaterialPropertyBlock properties = new MaterialPropertyBlock();
                    meshRenderer.GetPropertyBlock(properties, i);
                    newMeshRenderer.SetPropertyBlock(properties, i);
                }
            }

            CopyTransform(newMeshObject, meshObject);

            if (bitmaskType != TileBitmaskType.All && bitmaskValue > 0)
            {
                var bitmaskConnection = TileBitmaskManager.GetBitmaskConnectionFromBitmaskValue(bitmaskType, bitmaskValue);
                if (bitmaskConnection != null)
                {
                    var defaultBitmaskConnection = TileBitmaskManager.GetBitmaskConnectionFromBitmaskValue(bitmaskType, 0);
                    if (defaultBitmaskConnection != null && newMeshFilter.sharedMesh.name == (defaultBitmaskConnection.meshes[0].name + " Instance"))
                    {
                        newMeshFilter.sharedMesh = bitmaskConnection.meshes[0];
                        newMeshObject.transform.rotation = Quaternion.Euler(bitmaskConnection.eulerRotation);

                        if (bitmaskType == TileBitmaskType.Grassplot && newMeshRenderer.sharedMaterials.Length > 1)
                        {
                            var materialPropertyBlock = new MaterialPropertyBlock();
                            newMeshRenderer.GetPropertyBlock(materialPropertyBlock, 1);
                            materialPropertyBlock.SetFloat("_UVRotation", bitmaskConnection.eulerRotation.y);
                            newMeshRenderer.SetPropertyBlock(materialPropertyBlock, 1);
                        }
                    }
                }
            }

            return newMeshObject;
        }

        private static void CopyTransform(GameObject newObject, GameObject sourceObject)
        {
            newObject.transform.position = sourceObject.transform.position;
            newObject.transform.rotation = sourceObject.transform.rotation;
            newObject.transform.localScale = sourceObject.transform.lossyScale;
        }
    }
}
