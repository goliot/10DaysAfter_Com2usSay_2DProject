using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ObstacleGenerate : MonoBehaviour
{
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float _spawnRadiusMin;
    [SerializeField] private float _spawnRadiusMax;
    [SerializeField] private int count = 50;

    [ContextMenu("Spawn In Circle")]
    void SpawnInCircle()
    {
#if !UNITY_EDITOR
        return; // 런타임/빌드에서는 실행 불가(에디터 툴이니까)
#else
        // Undo는 "변경 전"에 등록해야 제대로 되돌릴 수 있음
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Spawn Obstacles");

        // 기존 자식 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

            // 에디터에서는 Prefab 연결 유지하는 Instantiate가 더 좋음(Undo도 깔끔)
            GameObject obstacle = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(obstacle, "Spawn Obstacle");

            obstacle.transform.SetParent(transform);

            float angle = Random.Range(0f, 2 * Mathf.PI);
            float t = Random.Range(0f, 1f);
            float radius = Mathf.Sqrt(t) * (_spawnRadiusMax - _spawnRadiusMin) + _spawnRadiusMin;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            obstacle.transform.position = transform.position + pos;
        }

        EditorUtility.SetDirty(gameObject);
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
