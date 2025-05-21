using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 세이브 포인트 시스템을 총괄하는 매니저 클래스
/// 싱글톤 패턴으로 구현되어 어디서든 접근 가능
/// </summary>
public class SavePointManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SavePointManager Instance { get; private set; }

    [Header("이벤트")]
    public UnityEvent<int> onPlayerReachedFinish; // 플레이어가 결승점에 도달했을 때 발생하는 이벤트

    // 팀별 마지막 세이브 포인트 (팀 ID를 키로 사용)
    private Dictionary<int, SavePoint> lastSavePoints = new Dictionary<int, SavePoint>();

    // 세이브 포인트 목록 (팀 ID를 키로 사용, 세이브 포인트 타입별로 관리)
    private Dictionary<int, Dictionary<SavePoint.SavePointType, List<SavePoint>>> savePointsByTeam =
        new Dictionary<int, Dictionary<SavePoint.SavePointType, List<SavePoint>>>();

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 팀 1과 2에 대한 딕셔너리 초기화
        InitializeTeamDictionary(1);
        InitializeTeamDictionary(2);
    }

    /// <summary>
    /// 팀별 세이브 포인트 딕셔너리 초기화
    /// </summary>
    private void InitializeTeamDictionary(int teamId)
    {
        if (!savePointsByTeam.ContainsKey(teamId))
        {
            savePointsByTeam[teamId] = new Dictionary<SavePoint.SavePointType, List<SavePoint>>();

            // 모든 세이브 포인트 타입에 대한 리스트 초기화
            foreach (SavePoint.SavePointType type in System.Enum.GetValues(typeof(SavePoint.SavePointType)))
            {
                savePointsByTeam[teamId][type] = new List<SavePoint>();
            }
        }
    }

    /// <summary>
    /// 세이브 포인트 등록
    /// </summary>
    public void RegisterSavePoint(SavePoint savePoint)
    {
        int targetTeamId = savePoint.playerTeamId;

        // 팀 ID가 0인 경우(공통 세이브 포인트) 모든 팀에 등록
        if (targetTeamId == 0)
        {
            RegisterSavePointForTeam(1, savePoint);
            RegisterSavePointForTeam(2, savePoint);
        }
        else
        {
            RegisterSavePointForTeam(targetTeamId, savePoint);
        }
    }

    /// <summary>
    /// 특정 팀을 위한 세이브 포인트 등록
    /// </summary>
    private void RegisterSavePointForTeam(int teamId, SavePoint savePoint)
    {
        InitializeTeamDictionary(teamId);

        SavePoint.SavePointType type = savePoint.GetSavePointType();

        if (!savePointsByTeam[teamId][type].Contains(savePoint))
        {
            savePointsByTeam[teamId][type].Add(savePoint);
        }

        // 스폰 포인트의 경우 자동으로 초기 세이브 포인트로 설정
        if (type == SavePoint.SavePointType.SpawnPoint && !lastSavePoints.ContainsKey(teamId))
        {
            SetLastSavePoint(teamId, savePoint);
        }
    }

    /// <summary>
    /// 마지막 세이브 포인트 설정
    /// </summary>
    public void SetLastSavePoint(int teamId, SavePoint savePoint)
    {
        lastSavePoints[teamId] = savePoint;
        Debug.Log($"팀 {teamId}의 마지막 세이브 포인트가 {savePoint.GetSavePointType()}로 설정되었습니다.");
    }

    /// <summary>
    /// 플레이어를 마지막 세이브 포인트로 리스폰
    /// </summary>
    public void RespawnPlayer(GameObject player, int teamId)
    {
        if (lastSavePoints.ContainsKey(teamId))
        {
            Vector3 respawnPosition = lastSavePoints[teamId].GetRespawnPosition();
            player.transform.position = respawnPosition;

            // 플레이어의 상태 초기화 (필요한 경우)
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"팀 {teamId} 플레이어가 {lastSavePoints[teamId].GetSavePointType()}에서 리스폰되었습니다.");
        }
        else
        {
            Debug.LogWarning($"팀 {teamId}의 세이브 포인트가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 특정 타입의 세이브 포인트 가져오기
    /// </summary>
    public SavePoint GetSavePointOfType(int teamId, SavePoint.SavePointType type)
    {
        if (savePointsByTeam.ContainsKey(teamId) &&
            savePointsByTeam[teamId].ContainsKey(type) &&
            savePointsByTeam[teamId][type].Count > 0)
        {
            return savePointsByTeam[teamId][type][0];
        }

        return null;
    }

    /// <summary>
    /// 플레이어가 결승점에 도달했을 때 호출
    /// </summary>
    public void PlayerReachedFinish(int teamId)
    {
        Debug.Log($"팀 {teamId}가 결승점에 도달했습니다!");
        onPlayerReachedFinish?.Invoke(teamId);

        // 게임 종료 로직 (예: 승리 화면 표시, 게임 일시 정지 등)
    }
}