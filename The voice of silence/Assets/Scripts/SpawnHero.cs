using UnityEngine;
using System.Collections; // Нужно для работы Coroutine

public class SpawnHero : MonoBehaviour
{
    [SerializeField] private bool spawnOnCityOutskirts = true;
    [SerializeField] private Transform cityCenterPoint;
    [SerializeField] private Vector2 cityHalfSize = new Vector2(120f, 120f);
    [SerializeField] private float outskirtsInset = 5f;

    private void Start()
    {
        if (spawnOnCityOutskirts)
        {
            // Запускаем спавн через корутину с задержкой
            StartCoroutine(ForcedSpawn());
        }
    }

    private IEnumerator ForcedSpawn()
    {
        // Ждем конца кадра, чтобы все системы Unity загрузились
        yield return new WaitForEndOfFrame();

        CharacterController cc = GetComponent<CharacterController>();

        // 1. Временно выключаем контроллер, чтобы он не мешал телепортации
        if (cc != null) cc.enabled = false;

        // 2. Расчет позиции
        Vector3 center = cityCenterPoint != null ? cityCenterPoint.position : Vector3.zero;
        int side = Random.Range(0, 4);
        Vector3 spawnPosition = center;

        float edgeX = cityHalfSize.x - outskirtsInset;
        float edgeZ = cityHalfSize.y - outskirtsInset;

        switch (side)
        {
            case 0: spawnPosition += new Vector3(Random.Range(-edgeX, edgeX), 5f, edgeZ); break;
            case 1: spawnPosition += new Vector3(Random.Range(-edgeX, edgeX), 5f, -edgeZ); break;
            case 2: spawnPosition += new Vector3(edgeX, 5f, Random.Range(-edgeZ, edgeZ)); break;
            case 3: spawnPosition += new Vector3(-edgeX, 5f, Random.Range(-edgeZ, edgeZ)); break;
        }

        // 3. Силовая установка позиции
        transform.position = spawnPosition;

        // 4. Поворот к центру
        Vector3 lookTarget = new Vector3(center.x, transform.position.y, center.z);
        transform.LookAt(lookTarget);

        // Даем физике осознать новую позицию
        yield return null;

        // 5. Включаем обратно
        if (cc != null) cc.enabled = true;

    }
}
