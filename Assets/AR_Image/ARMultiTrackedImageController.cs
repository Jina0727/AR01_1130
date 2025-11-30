using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Unity 스크립트(자산 참조 1개)/참조 0개
public class ARMultiTrackedImageController : MonoBehaviour
{
    public ARTrackedImageManager arTrackedImageManager;
    public GameObject[] prefabs;

    // 생성된 오브젝트를 이미지 이름으로 관리할 수 있다.
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    // Unity 메서지/참조 0개
    private void OnEnable()
    {
        if (arTrackedImageManager != null)
        {
            arTrackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }
    }

    // Unity 메서지/참조 0개
    private void OnDisable()
    {
        if (arTrackedImageManager != null)
        {
            arTrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }
    }

    // 참조 2개
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // 새로 감지된 이미지 처리 실행
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            HandleAddedImage(trackedImage);
        }

        // 업데이트된 이미지 처리 (위치/회전 갱신)
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            HandleUpdatedImage(trackedImage);
        }

        // 제거된 이미지 처리 (removed는 KeyValuePair 반환)
        foreach (var removed in eventArgs.removed)
        {
            HandleRemovedImage(removed.Value);
        }
    }

    // 새로운 이미지가 감지되었을 때
    private void HandleAddedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        int prefabIndex = GetPrefabIndexByImageName(imageName);

        if (prefabIndex >= 0 && prefabIndex < prefabs.Length && prefabs[prefabIndex] != null)
        {
            GameObject spawnedObj = Instantiate(prefabs[prefabIndex], trackedImage.transform);
            spawnedObj.transform.localPosition = Vector3.zero;
            spawnedObj.transform.localRotation = Quaternion.identity;
            spawnedObj.SetActive(true);

            // Dictionary에 저장
            spawnedObjects[imageName] = spawnedObj;

            Debug.Log($"[AR] 이미지 '{imageName}' 감지 → Prefab[{prefabIndex}] 생성");
        }
        else
        {
            Debug.LogWarning($"[AR] 이미지 '{imageName}'에 해당하는 Prefab을 찾을 수 없습니다. Index: {prefabIndex}");
        }
    }

    private void HandleUpdatedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (!spawnedObjects.TryGetValue(imageName, out GameObject spawnedObj))
        {
            // 아직 생성되지 않은 경우 생성
            HandleAddedImage(trackedImage);
            return;
        }

        // 트래킹 상태에 따라 활성화/비활성화
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            spawnedObj.SetActive(true);

            // 위치/회전 업데이트
            spawnedObj.transform.SetPositionAndRotation(
                trackedImage.transform.position,
                trackedImage.transform.rotation
            );
        }
        else if (trackedImage.trackingState == TrackingState.Limited)
        {
            // Limited 상태에서는 비활성화 (선택적)
            spawnedObj.SetActive(false);
        }
    }

    // 이미지가 더 이상 화면에 안나올때의 케이스
    private void HandleRemovedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedObjects.TryGetValue(imageName, out GameObject spawnedObj))
        {
            spawnedObj.SetActive(false);
            Debug.Log($"[AR] 이미지 '{imageName}' 트래킹 종료.");
        }
    }

    private int GetPrefabIndexByImageName(string imageName)
    {
        // ReferenceImageLibrary의 이미지 순서대로 매칭(중요)
        if (arTrackedImageManager.referenceLibrary != null)
        {
            for (int i = 0; i < arTrackedImageManager.referenceLibrary.count; i++)
            {
                if (arTrackedImageManager.referenceLibrary[i].name == imageName)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public GameObject GetSpawnedObject(string imageName)
    {
        spawnedObjects.TryGetValue(imageName, out GameObject obj);
        return obj;
    }

    public void ClearAllSpawnedObjects()
    {
        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        spawnedObjects.Clear();
    }
}
