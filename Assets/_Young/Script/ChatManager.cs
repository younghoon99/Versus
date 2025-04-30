using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class ChatManager : MonoBehaviourPunCallbacks
{
    // 싱글톤 인스턴스 (전역 접근용)
    public static ChatManager Instance { get; private set; }
    public GameObject m_Content;
    public GameObject chatTextPrefab; // 인스펙터에서 할당
    public TMP_InputField m_inputField;

    PhotonView photonview;

    string m_strUserName;
    public ScrollRect scrollRect;


    void Awake()
    {
        // 싱글톤 인스턴스 할당
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Screen.SetResolution(960, 600, false);
        PhotonNetwork.ConnectUsingSettings();
        photonview = GetComponent<PhotonView>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && m_inputField.isFocused == false)
        {
            m_inputField.ActivateInputField();
        }
    }
    public override void OnConnectedToMaster()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;

        int nRandomKey = Random.Range(0, 100);

        m_strUserName = "user" + nRandomKey;

        PhotonNetwork.LocalPlayer.NickName = m_strUserName;
        PhotonNetwork.JoinOrCreateRoom("Room1", options, null);
    }

    public override void OnJoinedRoom()
    {
        AddChatMessage("connect user : " + PhotonNetwork.LocalPlayer.NickName);
    }

    // 입력 필드에서 엔터 키를 눌렀을 때 호출
    public void OnEndEditEvent()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            string nickname = GetCharacterNickname();
            string strMessage = nickname + " : " + m_inputField.text;
            photonview.RPC("RPC_Chat", RpcTarget.All, strMessage);
            m_inputField.text = "";
        }
    }

    // 채팅 메시지를 UI에 추가하는 함수
    // 채팅 메시지를 UI에 추가하는 함수
    void AddChatMessage(string message)
    {
        GameObject goText = Instantiate(chatTextPrefab, m_Content.transform);
        goText.GetComponent<TextMeshProUGUI>().text = message;

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content.GetComponent<RectTransform>());
        //이걸로 줄바꿈 문제 해결

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // 채팅 입력 중 여부 반환 (InputField가 포커스 상태면 true)
    public bool IsChatting
    {
        get { return m_inputField.isFocused; }
    }

    // 캐릭터 ID에 따라 닉네임 반환
    string GetCharacterNickname()
    {
        string nickname = "알수없음";
        if (Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("CharacterSelection"))
        {
            int charId = (int)Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties["CharacterSelection"];
            if (charId == 0) nickname = "노랭이";
            else if (charId == 1) nickname = "파랭이";
        }
        return nickname;
    }


    // RPC로 호출되는 채팅 함수 (모든 클라이언트에서 실행)
    [PunRPC]
    void RPC_Chat(string message)
    {
        // 받은 메시지를 UI에 추가
        AddChatMessage(message);
    }
}