using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class BuildingView : MonoBehaviour
    {
        [SerializeField] private List<GameObject> buildingPrefabs;

        private const float CellSize = 1f;
        private Dictionary<string, GameObject> _buildingObjects = new Dictionary<string, GameObject>();

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingRemovedEvent>(OnBuildingRemoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingRemovedEvent>(OnBuildingRemoved);
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            Vector3 worldPos = GridToWorld(evt.Position);
            GameObject prefab = GetBuildingPrefab(evt.BuildingId);

            GameObject buildingGO;
            if (prefab != null)
            {
                buildingGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                buildingGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                buildingGO.transform.SetParent(transform);
                buildingGO.transform.position = worldPos + Vector3.up * 0.5f;
                buildingGO.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            }

            buildingGO.name = $"Building_{evt.BuildingId}";
            _buildingObjects[evt.BuildingId] = buildingGO;
        }

        private void OnBuildingRemoved(BuildingRemovedEvent evt)
        {
            if (_buildingObjects.TryGetValue(evt.BuildingId, out var go))
            {
                Destroy(go);
                _buildingObjects.Remove(evt.BuildingId);
            }
        }

        private GameObject GetBuildingPrefab(string buildingId)
        {
            if (buildingPrefabs == null)
                return null;

            // Try to find a prefab whose name contains the building config id
            var buildingSystem = ServiceLocator.Get<BuildingSystem>();
            var buildings = buildingSystem.GetAllBuildings();
            string configId = null;

            foreach (var building in buildings)
            {
                if (building.Id == buildingId)
                {
                    configId = building.ConfigId;
                    break;
                }
            }

            if (configId == null)
                return null;

            foreach (var prefab in buildingPrefabs)
            {
                if (prefab != null && prefab.name.Contains(configId))
                {
                    return prefab;
                }
            }

            return buildingPrefabs.Count > 0 ? buildingPrefabs[0] : null;
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * CellSize + CellSize / 2f,
                0f,
                gridPos.y * CellSize + CellSize / 2f
            );
        }
    }
}
