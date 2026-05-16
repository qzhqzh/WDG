using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class ProjectileView : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;

        private const float CellSize = 1f;

        private void OnEnable()
        {
            EventBus.Subscribe<AttackEvent>(OnAttack);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AttackEvent>(OnAttack);
        }

        private void OnAttack(AttackEvent evt)
        {
            // Find attacker position from BuildingSystem
            var buildingSystem = ServiceLocator.Get<BuildingSystem>();
            var buildings = buildingSystem.GetAllBuildings();

            Vector3 startPos = Vector3.zero;
            bool found = false;

            foreach (var building in buildings)
            {
                if (building.Id == evt.AttackerId)
                {
                    startPos = GridToWorld(building.GridPosition) + Vector3.up * 0.5f;
                    found = true;
                    break;
                }
            }

            if (!found)
                return;

            Vector3 targetPos = new Vector3(
                evt.TargetPosition.x * CellSize + CellSize / 2f,
                0.3f,
                evt.TargetPosition.y * CellSize + CellSize / 2f
            );

            StartCoroutine(MoveProjectile(startPos, targetPos));
        }

        private IEnumerator MoveProjectile(Vector3 start, Vector3 end)
        {
            GameObject projectileGO;

            if (projectilePrefab != null)
            {
                projectileGO = Instantiate(projectilePrefab, start, Quaternion.identity, transform);
            }
            else
            {
                projectileGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileGO.transform.SetParent(transform);
                projectileGO.transform.position = start;
                projectileGO.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            }

            float distance = Vector3.Distance(start, end);
            float duration = distance / projectileSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                projectileGO.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            Destroy(projectileGO);
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
