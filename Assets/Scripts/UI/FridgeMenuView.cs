using UnityEngine;
using UnityEngine.UI;

public sealed class FridgeMenuView : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Text title;
    [SerializeField] private RectTransform drinksContainer;
    [SerializeField] private Button closeButton;

    public Canvas MenuCanvas => menuCanvas;
    public Text Title => title;
    public RectTransform DrinksContainer => drinksContainer;
    public Button CloseButton => closeButton;
}
