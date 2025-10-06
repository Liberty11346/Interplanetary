using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// there so many managers
    /// </summary>
    public class ButtonsManager : MonoBehaviour
    {
        // public enum ButtonType
        // {
        //     ProductScout,
        //     ProductDestroyer,
        //     ProductCruiser,
        //     ProductBattleCruiser,
        // }

        // 각 씬의 컨트롤러에 대한 참조
        // private soundCtrl soundManager;
        // private playerGameCtrl playerGameController;
        private PlayerInputManager playerInputManager;

        // ===buttons..===
        [SerializeField] private Button fleetIconQButton, fleetIconWButton, fleetIconEButton, fleetIconRButton;

        void Start()
        {
            //soundManager = soundCtrl.Instance;
            playerInputManager = PlayerInputManager.Instance;

            // playerGameController = InGameUIManager.Instance.playerGameController;

            // fleetIconQButton.onClick.AddListener(() => OnButtonClick(ButtonType.ProductScout));
            // fleetIconWButton.onClick.AddListener(() => OnButtonClick(ButtonType.ProductDestroyer));
            // fleetIconEButton.onClick.AddListener(() => OnButtonClick(ButtonType.ProductCruiser));
            // fleetIconRButton.onClick.AddListener(() => OnButtonClick(ButtonType.ProductBattleCruiser));

            fleetIconQButton.onClick.AddListener( () => playerInputManager.OnFleetProducted?.Invoke(0) );
            fleetIconWButton.onClick.AddListener( () => playerInputManager.OnFleetProducted?.Invoke(1) );
            fleetIconEButton.onClick.AddListener( () => playerInputManager.OnFleetProducted?.Invoke(2) );
            fleetIconRButton.onClick.AddListener( () => playerInputManager.OnFleetProducted?.Invoke(3) );
        }

        // public void OnButtonClick(ButtonType buttonType)
        // {
        //     // 모든 버튼 클릭 시 공통적으로 사운드를 재생합니다.
        //     if (soundManager != null)
        //     {
        //         soundManager.PlaySound("command");
        //     }

        //     switch (buttonType)
        //     {
        //         case ButtonType.ProductScout:
        //             playerGameController?.StartCoroutine(playerGameController.ProductFleet(playerGameController.playerScout));
        //             break;
        //         case ButtonType.ProductDestroyer:
        //             playerGameController?.StartCoroutine(playerGameController.ProductFleet(playerGameController.playerDestroyer));
        //             break;
        //         case ButtonType.ProductCruiser:
        //             playerGameController?.StartCoroutine(playerGameController.ProductFleet(playerGameController.playerCruiser));
        //             break;
        //         case ButtonType.ProductBattleCruiser:
        //             playerGameController?.StartCoroutine(playerGameController.ProductFleet(playerGameController.playerBattleCruiser));
        //             break;
        //     }
        // }
    }
}
