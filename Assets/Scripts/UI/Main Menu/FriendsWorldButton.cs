using UnityEngine;
using UnityEngine.UI;

public class FriendsWorldButton : MonoBehaviour
{
    private const float DoubleClickMaxTime = 0.3f;

    public UnityEngine.UI.Text titleText;
    public UnityEngine.UI.Text descriptionText;

    private Button _button;
    private MultiplayerMenu _menuManager;
    private float _lastClickTime;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Click);
    }

    private void Start()
    {
        _menuManager = GetComponentInParent<MultiplayerMenu>();
    }

    private void Update()
    {
        titleText.text = "Friend's World";
        descriptionText.text = "Join Friend's World";
    }

    private void Click()
    {
        if (Time.time - _lastClickTime < DoubleClickMaxTime)
            _menuManager.Play();
        _lastClickTime = Time.time;
    }
}