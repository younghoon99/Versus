using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이브 포인트 기능을 담당하는 클래스
/// 플레이어가 세이브 포인트를 통과하면 해당 위치를 저장하고
/// 플레이어가 사망 시 마지막 세이브 포인트에서 리스폰
/// </summary>
public class SavePoint : MonoBehaviour
{
    public enum SavePointType
    {
        SpawnPoint,    // 초기 스폰 포인트
        SavePoint1,    // 첫 번째 세이브 포인트
        SavePoint2,    // 두 번째 세이브 포인트
        SavePoint3,    // 세 번째 세이브 포인트
        FinishPoint    // 결승점
    }

    [Header("세이브 포인트 설정")]
    public SavePointType savePointType = SavePointType.SavePoint1;
    public int playerTeamId = 0; // 0: 모든 플레이어, 1: 플레이어1/팀1, 2: 플레이어2/팀2
    public Color activatedColor = Color.green;
    public Color deactivatedColor = Color.red;
    public GameObject visualEffect; // 활성화 시 보여줄 시각 효과

    [Header("테스트용")]
    public bool forceActivateOnStart = false;

    private bool isActivated = false;
    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }

        UpdateVisuals();
    }

    private void Start()
    {
        // 스폰 포인트의 경우 자동으로 활성화
        if (savePointType == SavePointType.SpawnPoint || forceActivateOnStart)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }

        // 세이브 포인트 매니저에 자신을 등록
        SavePointManager.Instance.RegisterSavePoint(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 확인
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            // 플레이어 팀 ID 확인 (0은 모든 플레이어 대상)
            if (playerTeamId == 0 || playerTeamId == player.teamId)
            {
                // 세이브 포인트 활성화 및 매니저에 알림
                Activate();
                SavePointManager.Instance.SetLastSavePoint(player.teamId, this);

                // 결승점인 경우 게임 승리 처리
                if (savePointType == SavePointType.FinishPoint)
                {
                    SavePointManager.Instance.PlayerReachedFinish(player.teamId);
                }
            }
        }
    }

    /// <summary>
    /// 세이브 포인트 활성화
    /// </summary>
    public void Activate()
    {
        isActivated = true;
        UpdateVisuals();
    }

    /// <summary>
    /// 세이브 포인트 비활성화
    /// </summary>
    public void Deactivate()
    {
        isActivated = false;
        UpdateVisuals();
    }

    /// <summary>
    /// 세이브 포인트의 시각적 상태 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        // 렌더러 색상 변경
        if (rend != null)
        {
            rend.material.color = isActivated ? activatedColor : deactivatedColor;
        }

        // 시각 효과 표시/숨김
        if (visualEffect != null)
        {
            visualEffect.SetActive(isActivated);
        }
    }

    /// <summary>
    /// 이 세이브 포인트의 위치 반환
    /// </summary>
    public Vector3 GetRespawnPosition()
    {
        return transform.position + Vector3.up; // 약간 위로 올려서 바닥에 끼지 않도록
    }

    /// <summary>
    /// 이 세이브 포인트의 타입 반환
    /// </summary>
    public SavePointType GetSavePointType()
    {
        return savePointType;
    }
}
