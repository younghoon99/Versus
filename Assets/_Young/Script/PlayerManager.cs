using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    [Header("캐릭터 설정")]
    [Tooltip("캐릭터 프리팹 목록 (직접 할당)")]
    [SerializeField] private GameObject[] characterPrefabs = new GameObject[2]; // 인스펙터에서 직접 할당
    
    [Header("캐릭터별 스폰 설정")]
    [Tooltip("Character1 전용 스폰 위치 (캐릭터 ID 0)")]
    [SerializeField] private Transform character1SpawnPoint;
    
    [Tooltip("Character2 전용 스폰 위치 (캐릭터 ID 1)")]
    [SerializeField] private Transform character2SpawnPoint;
    
    // 캐릭터 선택 프로퍼티 키 (Intro.cs와 동일한 값 사용)
    private readonly string CHARACTER_SELECTION_PROP = "CharacterSelection";
    
    // PlayerPrefs에 사용할 키
    private readonly string CHARACTER_PREFS_KEY = "SelectedCharacterID";
    
    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 플레이어 캐릭터 생성
            SpawnPlayer();
        }
        else
        {
            Debug.LogError("네트워크에 연결되어 있지 않습니다!");
            
            // 디버그 모드: 싱글 플레이 테스트용 (네트워크 연결 없을 때)
            #if UNITY_EDITOR
            SpawnLocalPlayerForTesting();
            #endif
        }
    }
    
    // 테스트용 로컬 플레이어 생성 (에디터 전용)
    private void SpawnLocalPlayerForTesting()
    {
        #if UNITY_EDITOR
        Debug.Log("테스트 모드: 로컬 플레이어 생성");
        
        // 스폰 위치 결정 (기본: 첫 번째 캐릭터 위치)
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        
        if (character1SpawnPoint != null)
        {
            spawnPosition = character1SpawnPoint.position;
            spawnRotation = character1SpawnPoint.rotation;
        }
        
        // 첫 번째 캐릭터로 로컬 오브젝트 생성 (테스트용)
        if (characterPrefabs != null && characterPrefabs.Length > 0 && characterPrefabs[0] != null)
        {
            GameObject playerGO = Instantiate(characterPrefabs[0], spawnPosition, spawnRotation);
            SetupPlayerCamera(playerGO);
        }
        else
        {
            Debug.LogError("캐릭터 프리팹이 할당되지 않았습니다. 인스펙터에서 프리팹을 할당해주세요.");
        }
        #endif
    }
    
    // 플레이어 캐릭터 생성 메서드
    private void SpawnPlayer()
    {
        // 먼저 캐릭터 프리팹이 할당되었는지 확인
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("캐릭터 프리팹이 할당되지 않았습니다. 인스펙터에서 프리팹을 할당해주세요.");
            return;
        }
        
        // 모든 커스텀 프로퍼티 출력 (디버그용)
        Debug.Log("현재 로컬 플레이어의 모든 커스텀 프로퍼티:");
        foreach (var prop in PhotonNetwork.LocalPlayer.CustomProperties)
        {
            Debug.Log($"키: {prop.Key}, 값: {prop.Value}");
        }
        
        // 두 가지 방법으로 캐릭터 ID 가져오기 시도
        int characterID = 0;
        
        // 1. PlayerPrefs에서 먼저 확인 (씬 전환에도 유지)
        if (PlayerPrefs.HasKey(CHARACTER_PREFS_KEY))
        {
            characterID = PlayerPrefs.GetInt(CHARACTER_PREFS_KEY);
            Debug.Log($"PlayerPrefs에서 캐릭터 ID 불러옴: {characterID}");
        }
        // 2. Photon 커스텀 프로퍼티에서 확인
        else if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CHARACTER_SELECTION_PROP, out object characterObj))
        {
            characterID = (int)characterObj;
            Debug.Log($"커스텀 프로퍼티에서 캐릭터 ID 불러옴: {characterID}");
            
            // 찾은 값을 PlayerPrefs에도 저장
            PlayerPrefs.SetInt(CHARACTER_PREFS_KEY, characterID);
        }
        else
        {
            Debug.LogWarning($"저장된 캐릭터 ID를 찾을 수 없음. 기본값({characterID}) 사용");
            
            // 커스텀 프로퍼티 업데이트
            try {
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props.Add(CHARACTER_SELECTION_PROP, characterID);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                Debug.Log("기본 캐릭터 ID를 프로퍼티에 설정했습니다.");
            }
            catch (System.Exception ex) {
                Debug.LogError($"프로퍼티 설정 중 오류: {ex.Message}");
            }
        }
        
        // 유효한 캐릭터 ID 확인
        if (characterID < 0 || characterID >= characterPrefabs.Length)
        {
            Debug.LogWarning("유효하지 않은 캐릭터 ID: " + characterID + ", 첫 번째 캐릭터를 사용합니다.");
            characterID = 0;
        }
        
        // 선택한 캐릭터 프리팹이 할당되었는지 확인
        if (characterPrefabs[characterID] == null)
        {
            Debug.LogError($"캐릭터 ID {characterID}에 해당하는 프리팹이 할당되지 않았습니다.");
            
            // 사용 가능한 대체 프리팹 찾기
            for (int i = 0; i < characterPrefabs.Length; i++)
            {
                if (characterPrefabs[i] != null)
                {
                    characterID = i;
                    Debug.Log($"대체 프리팹을 찾음: 인덱스 {i}");
                    break;
                }
            }
            
            // 모든 프리팹이 null인 경우
            if (characterPrefabs[characterID] == null)
            {
                Debug.LogError("사용 가능한 캐릭터 프리팹이 없습니다. 인스펙터에서 최소 하나의 프리팹을 할당해주세요.");
                return;
            }
        }
        
        // 캐릭터 ID에 따라 고정된 스폰 위치 결정
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        
        // 캐릭터 타입에 맞는 스폰 포인트 선택
        switch (characterID)
        {
            case 0: // Character1 스폰 위치
                if (character1SpawnPoint != null)
                {
                    spawnPosition = character1SpawnPoint.position;
                    spawnRotation = character1SpawnPoint.rotation;
                    Debug.Log("Character1 전용 스폰 포인트 사용");
                }
                else
                {
                    Debug.LogWarning("Character1 스폰 포인트가 할당되지 않았습니다. 기본 위치 사용");
                }
                break;
                
            case 1: // Character2 스폰 위치
                if (character2SpawnPoint != null)
                {
                    spawnPosition = character2SpawnPoint.position;
                    spawnRotation = character2SpawnPoint.rotation;
                    Debug.Log("Character2 전용 스폰 포인트 사용");
                }
                else
                {
                    Debug.LogWarning("Character2 스폰 포인트가 할당되지 않았습니다. 기본 위치 사용");
                }
                break;
                
            default:
                Debug.LogWarning($"지원되지 않는 캐릭터 ID: {characterID}, 기본 위치 사용");
                break;
        }
        
        try
        {
            // 선택한 프리팹 이름 가져오기
            string prefabName = characterPrefabs[characterID].name;
            Debug.Log($"캐릭터 생성 시도: ID={characterID}, 프리팹={prefabName}, 위치={spawnPosition}");
            
            // 반드시 PhotonNetwork.Instantiate로 생성해야 내 소유권이 생김
            GameObject playerObj = PhotonNetwork.Instantiate(prefabName, spawnPosition, spawnRotation);
            if (playerObj != null)
            {
                var pv = playerObj.GetComponent<PhotonView>();
                Debug.Log($"생성된 Player의 IsMine: {pv?.IsMine}, ViewID: {pv?.ViewID}");
                // 카메라 및 컨트롤 설정
                SetupPlayerCamera(playerObj);
            }
            else
            {
                Debug.LogError($"[에러] Player 인스턴스화 실패: {prefabName}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"캐릭터 생성 중 오류 발생: {ex.Message}");
            Debug.LogError("프리팹이 Photon 네트워크로 인스턴스화 가능한지 확인하세요.");
            Debug.LogError("프리팹이 Resources 폴더에도 있어야 하며, 이름이 같아야 합니다.");
        }
    }
    
    // 플레이어 카메라 설정
    private void SetupPlayerCamera(GameObject playerObject)
    {
        if (playerObject == null) return;
        
        // CameraSetup 컴포넌트 확인
        CameraSetup cameraSetup = playerObject.GetComponent<CameraSetup>();
        
        if (cameraSetup == null)
        {
            Debug.LogWarning("플레이어 오브젝트에 CameraSetup 컴포넌트가 없습니다!");
            // 필요한 경우 여기서 CameraSetup 컴포넌트 추가 가능
            // cameraSetup = playerObject.AddComponent<CameraSetup>();
        }
        
        // Player 컴포넌트 확인
        Player playerController = playerObject.GetComponent<Player>();
        
        if (playerController == null)
        {
            Debug.LogWarning("플레이어 오브젝트에 Player 컴포넌트가 없습니다!");
        }
        else
        {
            // 필요한 경우 Player 컴포넌트에 추가 설정
            // playerController.InitializePlayer();
        }
    }
    
    // 플레이어 속성이 변경되었을 때 호출
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 변경된 속성이 캐릭터 선택인지 확인
        if (changedProps.ContainsKey(CHARACTER_SELECTION_PROP))
        {
            Debug.Log($"플레이어 {targetPlayer.NickName}의 캐릭터 선택이 변경됨: {changedProps[CHARACTER_SELECTION_PROP]}");
        }
    }
}
