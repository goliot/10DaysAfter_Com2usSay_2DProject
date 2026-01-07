#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class ObstacleGenerate : MonoBehaviour
{
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float _spawnRadiusMin;
    [SerializeField] private float _spawnRadiusMax;
    [SerializeField] private int count = 50;

    [ContextMenu("Spawn In Circle")]
    void SpawnInCircle()
    {
        // 기존 자식 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            // Undo 되게 하려면 DestroyImmediate 대신 이걸 쓰는 게 좋아요
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(this.gameObject, "Clear Old Obstacles");
#endif

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            if (obstaclePrefab == null) continue;

            GameObject obstacle;
#if UNITY_EDITOR
            obstacle = (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab);
            Undo.RegisterCreatedObjectUndo(obstacle, "Spawn Obstacle");
#else
            obstacle = Instantiate(obstaclePrefab);
#endif
            obstacle.transform.SetParent(transform, true);

            float angle = Random.Range(0f, 2 * Mathf.PI);
            float t = Random.Range(0f, 1f);
            float radius = Mathf.Sqrt(t) * (_spawnRadiusMax - _spawnRadiusMin) + _spawnRadiusMin;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            obstacle.transform.position = transform.position + pos;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _spawnRadiusMax);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _spawnRadiusMin);
    }
}
