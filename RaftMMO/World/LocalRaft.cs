using System.Linq;
using UnityEngine;

namespace RaftMMO.World
{
    public class LocalRaft
    {
        public static Bounds CalculateBounds()
        {
            var raft = ComponentManager<Raft>.Value;

            bool isFirst = true;
            Bounds bounds = new Bounds(SingletonGeneric<GameManager>.Singleton.lockedPivot.position, Vector3.zero);
            foreach (var collider in raft.GetComponentsInChildren<Collider>())
            {
                if (isFirst)
                {
                    bounds = collider.bounds;
                    isFirst = false;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return bounds;
        }
    }
}
