using UnityEngine;

/// <summary>
/// 게임 HUD 관리자
/// 모든 하위 UI 컴포넌트를 관리하고 GameManager와 통신
/// </summary>
public class UI_HUD : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool enableChat = true;

    [Header("Sub Components")]
    [SerializeField] private UIProductionButtons productionButtons;
    [SerializeField] private UIPlayerResource playerResource;
    [SerializeField] private UIFleetInfo fleetInfo;
    [SerializeField] private UIPlayerStatus playerStatus;
    [SerializeField] private UIChat uiChat;
    [SerializeField] private UISystemMenu uiSystemMenu;
    [SerializeField] private UIGameResult uiGameResult;

    // GameManager 참조
    private GameManager gameManager;
    private int myPlayerId = -1;

    private void Awake()
    {
        // 컴포넌트 자동 찾기
        if (productionButtons == null) productionButtons = GetComponentInChildren<UIProductionButtons>();
        if (playerResource == null) playerResource = GetComponentInChildren<UIPlayerResource>();
        if (fleetInfo == null) fleetInfo = GetComponentInChildren<UIFleetInfo>();
        if (playerStatus == null) playerStatus = GetComponentInChildren<UIPlayerStatus>();
        
        // 채팅 기능이 활성화된 경우에만 찾기
        if (enableChat)
        {
            if (uiChat == null) uiChat = GetComponentInChildren<UIChat>();
        }
        else
        {
            // 비활성화된 경우 참조 제거 (혹시 할당되어 있어도 무시)
            uiChat = null;
        }

        if (uiSystemMenu == null) uiSystemMenu = GetComponentInChildren<UISystemMenu>();
        if (uiGameResult == null) uiGameResult = GetComponentInChildren<UIGameResult>();

        // 초기 상태 설정
        if (fleetInfo != null) fleetInfo.Show(false);
        if (uiGameResult != null) uiGameResult.Hide();
        
        // 채팅 UI 숨기기 (옵션이 꺼져있거나 컴포넌트가 없는 경우)
        if (!enableChat || uiChat == null)
        {
            var chatObj = GetComponentInChildren<UIChat>(true); // 비활성화된 것도 찾아서
            if (chatObj != null) chatObj.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // 생산 버튼 이벤트 연결
            if (productionButtons != null)
            {
                productionButtons.OnProductionRequested += HandleProductionRequested;
            }
        }

        // 채팅 이벤트 연결 (활성화된 경우만)
        if (enableChat && uiChat != null)
        {
            uiChat.OnChatSendRequested += HandleChatSendRequested;
        }

        // 시스템 메뉴 이벤트 연결
        if (uiSystemMenu != null)
        {
            uiSystemMenu.OnLeaveRequested += HandleLeaveRequested;
        }

        // 게임 결과 이벤트 연결
        if (uiGameResult != null)
        {
            uiGameResult.OnConfirmClicked += HandleGameResultConfirm;
        }
    }

    // ... (기존 메서드들) ...

    #region Chat & System Methods

    public void AddChatMessage(string sender, string message, System.DateTime? timestamp = null)
    {
        if (uiChat != null)
        {
            uiChat.AddMessage(sender, message, timestamp);
        }
    }

    public void UpdateConnectionStatus(bool isConnected)
    {
        if (uiSystemMenu != null)
        {
            uiSystemMenu.UpdateConnectionStatus(isConnected);
        }
    }

    public void ShowGameResult(bool isWinner, string message = "")
    {
        if (uiGameResult != null)
        {
            uiGameResult.ShowResult(isWinner, message);
        }
    }

    #endregion

    #region Event Handlers

    private void HandleChatSendRequested(string message)
    {
        if (gameManager != null && gameManager.gamePlayManager != null)
        {
            gameManager.gamePlayManager.SendChatMessage(message);
        }
    }

    private void HandleLeaveRequested()
    {
        if (gameManager != null)
        {
            gameManager.EndGame();
        }
    }

    private void HandleGameResultConfirm()
    {
        if (gameManager != null)
        {
            gameManager.EndGame();
        }
    }

    public void StartCooldown(int fleetTypeId, float duration)
    {
        if (productionButtons != null)
        {
            productionButtons.StartCooldown(fleetTypeId, duration);
        }
    }

    #endregion

    /// <summary>
    /// 게임 초기화 (GameManager에서 호출)
    /// </summary>
    public void Initialize(int playerId, string playerName)
    {
        myPlayerId = playerId;
        
        if (playerStatus != null)
        {
            playerStatus.SetPlayerName(playerName);
        }
    }

    // 선택된 함대 ID 추적
    private int currentSelectedFleetId = -1;

    /// <summary>
    /// 게임 상태 업데이트 (GameManager에서 매 틱마다 호출)
    /// </summary>
    public void UpdateGameState(GamePlayManager.GameState gameState, float gameTime)
    {
        if (gameState == null || gameState.players == null) return;

        // 1. 자원, 상태, 생산 대기열 업데이트
        foreach (var player in gameState.players)
        {
            if (player.id == myPlayerId)
            {
                // 자원 업데이트
                if (playerResource != null)
                {
                    playerResource.UpdateResources((int)player.Gas, (int)player.Mineral, player.Supply, player.Supply);
                }

                // 생산 대기열 업데이트
                if (productionButtons != null && player.productionQueue != null)
                {
                    productionButtons.UpdateProductionQueue(player.productionQueue);
                }
                break;
            }
        }

        // 2. 게임 시간 업데이트
        if (playerStatus != null)
        {
            playerStatus.UpdateGameTime(gameTime);
        }

        // 3. 선택된 함대 정보 업데이트
        if (currentSelectedFleetId != -1 && fleetInfo != null && gameManager != null && gameManager.uiGame != null)
        {
            var info = gameManager.uiGame.GetFleetInfo(currentSelectedFleetId);
            if (info != null)
            {
                bool isAlly = info.ownerId == myPlayerId;
                fleetInfo.SetFleetInfo(info, isAlly);
            }
            else
            {
                // 함대가 파괴되었거나 찾을 수 없음 -> 선택 해제
                OnFleetSelected(-1);
            }
        }
    }

    /// <summary>
    /// 함대 선택 이벤트 핸들러
    /// </summary>
    public void OnFleetSelected(int fleetId)
    {
        currentSelectedFleetId = fleetId;

        if (fleetInfo == null) return;

        if (fleetId == -1)
        {
            fleetInfo.Show(false);
            return;
        }

        // 초기 정보 설정
        if (gameManager != null && gameManager.uiGame != null)
        {
            var info = gameManager.uiGame.GetFleetInfo(fleetId);
            if (info != null)
            {
                bool isAlly = info.ownerId == myPlayerId;
                fleetInfo.SetFleetInfo(info, isAlly);
                fleetInfo.Show(true);
            }
        }
    }

    /// <summary>
    /// 함대 정보 직접 설정 (GameManager에서 호출 권장)
    /// </summary>
    public void UpdateFleetInfo(GamePlayManager.GameState.FleetInfo info, bool isAlly)
    {
        if (fleetInfo != null)
        {
            fleetInfo.SetFleetInfo(info, isAlly);
            fleetInfo.Show(true);
        }
    }

    /// <summary>
    /// 생산 요청 핸들러
    /// </summary>
    private void HandleProductionRequested(int fleetTypeId)
    {
        if (gameManager != null)
        {
            gameManager.RequestProduction(fleetTypeId);
            Debug.Log($"[UI_HUD] Requesting production of fleet type {fleetTypeId}");
        }
    }

    private void OnDestroy()
    {
        if (productionButtons != null)
        {
            productionButtons.OnProductionRequested -= HandleProductionRequested;
        }
    }
}
