using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
// PlayerDataManager 참조 추가
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Intro : MonoBehaviourPunCallbacks
{
    private string gameVersion = "1"; //게임버전
    public TextMeshProUGUI connectionInfoText; //네트워크 정보 표시할 텍스트
    public Button joinButton; //룸 접속 버튼
    
    [Header("캐릭터 선택 설정")]
    public GameObject characterSelectPanel;       // 캐릭터 선택 패널
    public Button character1Button;               // 첫 번째 캐릭터 버튼
    public Button character2Button;               // 두 번째 캐릭터 버튼 
    public Button confirmSelectionButton;         // 선택 확인 버튼
    public Image character1Preview;               // 첫 번째 캐릭터 미리보기
    public Image character2Preview;               // 두 번째 캐릭터 미리보기
    public Image selectedCharacterPreview;        // 선택된 캐릭터 미리보기

    [Header("디버그용 인게임 텍스트")]
    public TMPro.TextMeshProUGUI debugText;      // 인게임 실시간 디버그 텍스트
    
    private int selectedCharacterID = -1;         // 선택된 캐릭터 ID (기본값: 선택되지 않음)
    
    // 캐릭터 선택을 저장할 커스텀 프로퍼티 키
    private readonly string CHARACTER_SELECTION_PROP = "CharacterSelection";
     
    private void Start() //게임 실행과 동시에 마스터 서버 접속 시도
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion; 
        PhotonNetwork.ConnectUsingSettings();

        joinButton.interactable = false;
        connectionInfoText.text = "마스터 서버에 접속 중...";
        
        // 캐릭터 선택 패널 초기화
        InitCharacterSelectPanel();

        UpdateDebugText(); // 시작 시 디버그 텍스트 갱신
        
    }
    // 인게임 디버그 텍스트 갱신 함수
    private void UpdateDebugText()
    {
        if (debugText == null) return;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>Photon 상태:</b> {(PhotonNetwork.IsConnected ? "연결됨" : "연결 안됨")}");
        sb.AppendLine($"LocalPlayer: {PhotonNetwork.LocalPlayer.NickName} [{PhotonNetwork.LocalPlayer.ActorNumber}]");
        sb.AppendLine($"내 캐릭터 선택: {(PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CHARACTER_SELECTION_PROP, out object mySel) ? mySel.ToString() : "미선택")}");
        sb.AppendLine("<b>전체 플레이어 상태</b>");
        foreach (var player in PhotonNetwork.PlayerList)
        {
            string sel = player.CustomProperties.TryGetValue(CHARACTER_SELECTION_PROP, out object selId) ? selId.ToString() : "미선택";
            sb.AppendLine($"{player.NickName} [{player.ActorNumber}] - 캐릭터: {sel}");
        }
        debugText.text = sb.ToString();
    }

    // 캐릭터 선택 패널 초기화
    private void InitCharacterSelectPanel()
    {
        // 캐릭터 선택 패널 숨기기
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
        }
        
        // 캐릭터 1 버튼 이벤트 등록
        if (character1Button != null)
        {
            character1Button.onClick.AddListener(() => SelectCharacter(0));
        }
        
        // 캐릭터 2 버튼 이벤트 등록
        if (character2Button != null)
        {
            character2Button.onClick.AddListener(() => SelectCharacter(1));
        }
        
        // 선택 확인 버튼 이벤트 등록
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.onClick.AddListener(ConfirmCharacterSelection);
            confirmSelectionButton.interactable = false; // 초기에는 비활성화
        }

        UpdateCharacterButtonStates();
        TryEnableStartGame();
    }

    // 캐릭터 버튼 상태 동기화: 이미 선택된 캐릭터는 비활성화
    private void UpdateCharacterButtonStates()
    {
        int selected0 = -1;
        int selected1 = -1;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(CHARACTER_SELECTION_PROP, out object val))
            {
                int cid = (int)val;
                if (cid == 0) selected0 = player.ActorNumber;
                if (cid == 1) selected1 = player.ActorNumber;
            }
        }
        // 0번 캐릭터 버튼
        if (character1Button != null)
        {
            bool taken = selected0 != -1 && PhotonNetwork.LocalPlayer.ActorNumber != selected0;
            character1Button.interactable = !taken;
        }
        // 1번 캐릭터 버튼
        if (character2Button != null)
        {
            bool taken = selected1 != -1 && PhotonNetwork.LocalPlayer.ActorNumber != selected1;
            character2Button.interactable = !taken;
        }
    }

    // 모든 플레이어가 캐릭터 선택 완료 시 자동으로 5초 카운트다운 후 게임 시작
    public GameObject countdownPanel; // 인스펙터에서 할당
    public TMPro.TMP_Text countdownText; // 인스펙터에서 할당
    private bool countdownStarted = false;

    private void TryEnableStartGame()
    {
        bool allSelected = true;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey(CHARACTER_SELECTION_PROP) || (int)player.CustomProperties[CHARACTER_SELECTION_PROP] == -1)
            {
                allSelected = false;
                break;
            }
        }
        // 모든 플레이어가 선택 완료 && 마스터 && 중복 카운트다운 방지
        if (allSelected && PhotonNetwork.IsMasterClient && !countdownStarted)
        {
            countdownStarted = true;
            photonView.RPC("StartCountdown", RpcTarget.All);
        }
    }

    [PunRPC]
    private void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }

    private System.Collections.IEnumerator CountdownCoroutine()
    {
        if (countdownPanel != null) countdownPanel.SetActive(true);
        int seconds = 5;
        while (seconds > 0)
        {
            if (countdownText != null)
                countdownText.text = $"게임 시작까지 {seconds}초...";
            yield return new UnityEngine.WaitForSeconds(1f);
            seconds--;
        }
        if (countdownText != null)
            countdownText.text = "게임 시작!";
        yield return new UnityEngine.WaitForSeconds(0.5f);
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Main");
        }
    }
    
    // 캐릭터 선택 처리
    // 캐릭터 선택 처리 (중복 선택 방지)
    private void SelectCharacter(int characterID)
    {
        // 선택 시마다 디버그 텍스트 갱신
        UpdateDebugText();
        // 이미 다른 플레이어가 선택한 캐릭터인지 확인
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(CHARACTER_SELECTION_PROP, out object selId) && (int)selId == characterID && player != PhotonNetwork.LocalPlayer)
            {
                Debug.LogWarning($"이미 다른 플레이어가 선택한 캐릭터입니다: ID={characterID}");
                return; // 선택 불가
            }
        }
        selectedCharacterID = characterID;
        Debug.Log($"캐릭터 선택됨: ID={characterID}");
        // 선택된 캐릭터 강조 표시
        if (character1Button != null && character2Button != null)
        {
            character1Button.GetComponent<Image>().color = (characterID == 0) ? 
                new Color(0.8f, 0.8f, 1f) : Color.white;
            character2Button.GetComponent<Image>().color = (characterID == 1) ? 
                new Color(0.8f, 0.8f, 1f) : Color.white;
        }
        // 선택된 캐릭터 미리보기 표시
        if (selectedCharacterPreview != null)
        {
            if (characterID == 0 && character1Preview != null)
            {
                selectedCharacterPreview.sprite = character1Preview.sprite;
            }
            else if (characterID == 1 && character2Preview != null)
            {
                selectedCharacterPreview.sprite = character2Preview.sprite;
            }
            selectedCharacterPreview.gameObject.SetActive(true);
        }
        // 확인 버튼 활성화
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.interactable = true;
        }
    }
    
    // 캐릭터 선택 확인 및 룸 입장
    // 캐릭터 선택 확정(선택 정보 저장 및 UI 동기화)
    public void ConfirmCharacterSelection()
    {
        UpdateDebugText(); // 선택 확정 시 디버그 텍스트 갱신
        if (selectedCharacterID == -1) return; // 캐릭터가 선택되지 않았으면 무시
        // 선택한 캐릭터 정보를 플레이어 커스텀 프로퍼티에 저장
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
        playerProps.Add(CHARACTER_SELECTION_PROP, selectedCharacterID);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
        // PlayerPrefs에도 캐릭터 ID 저장 (씬 간 데이터 유지)
        PlayerPrefs.SetInt("SelectedCharacterID", selectedCharacterID);
        PlayerPrefs.Save();
        Debug.Log($"캐릭터 선택 저장 완료: ID={selectedCharacterID}, 커스텀 프로퍼티와 PlayerPrefs에 추가됨");
        // 캐릭터 선택 UI 유지 및 버튼/StartGame 동기화
        UpdateCharacterButtonStates();
        TryEnableStartGame();
        // 선택 후 UI를 숨기지 않고, 모든 플레이어가 선택 완료 시 마스터가 게임 시작
        connectionInfoText.text = "대기 중... 모든 플레이어가 선택하면 게임이 시작됩니다.";
    }
    
    public override void OnConnectedToMaster() //마스터 서버 접속 성공 시 호출
    {
        joinButton.interactable = true;
        connectionInfoText.text = "마스터 서버에 연결 성공";
        UpdateDebugText(); // 네트워크 연결 시 디버그 텍스트 갱신
    }
    
    public override void OnDisconnected(DisconnectCause cause) //마스터 서버 접속 실패 시 호출
    {
        joinButton.interactable = false;
        connectionInfoText.text = "오프라인 : 마스터 서버에 연결되지 않음 \n 접속 재시도 중";
        PhotonNetwork.ConnectUsingSettings();
        UpdateDebugText(); // 연결 끊김 시 디버그 텍스트 갱신
    }

    public void Connect() //룸 접속 시도
    {
        joinButton.interactable = false; //중복 접속 방지
        
        if(PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "룸 접속 중...";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            connectionInfoText.text = "오프라인 : 마스터 서버에 연결되지 않음 \n 접속 재시도 중";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    //(빈방이 없어) 랜덤 품 참가에 실패한 경우 자동 실행
    public override void OnJoinRandomFailed(short returnCode, string message) 
    {
        connectionInfoText.text = "빈 방이 없음, 새로운 방 생성..";
        PhotonNetwork.CreateRoom(null, new RoomOptions {MaxPlayers = 2});
    }
    
    public override void OnJoinedRoom() //룸에 참가완료된 경우 자동 실행 
    {
        connectionInfoText.text = "방 참가 성공...";
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
        }
        // 캐릭터 버튼 상태 동기화
        UpdateCharacterButtonStates();
        UpdateDebugText(); // 방 입장 시 디버그 텍스트 갱신
    }

    // 플레이어 커스텀 프로퍼티 변경 시 호출(Photon 콜백)
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 캐릭터 선택 관련 변경이면 버튼 상태 동기화
        if (changedProps.ContainsKey(CHARACTER_SELECTION_PROP))
        {
            UpdateCharacterButtonStates();
            TryEnableStartGame();
            UpdateDebugText(); // 상태 변경 시 디버그 텍스트 갱신
        }
    }
}
