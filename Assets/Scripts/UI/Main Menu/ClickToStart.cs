using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClickToStart : MonoBehaviour
{
    public float blinkFrequency;
    public CanvasGroup buttonsGroup;
    public UnityEngine.UI.Text text;
    public float blockDuration = 3f;

    private CanvasGroup thisGroup;
    private GameObject blocker;

    private static bool inputBlockedOnce = false; // tracks if the block already happened

    void Start()
    {
        thisGroup = GetComponent<CanvasGroup>();
        CreateBlocker();

        if (!inputBlockedOnce)
        {
            StartCoroutine(BlockInputAtStart(blockDuration));
            inputBlockedOnce = true;
        }

        StartCoroutine(TextBlinkLoop());
    }

    void Update()
    {
        if (blocker.activeSelf) return;

        if (Input.anyKeyDown)
        {
            Sound.PlayLocal(new Location(), "menu/click", 0, SoundType.Menu, 1f, 100000f, false);

            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
            buttonsGroup.alpha = 1;

            Destroy(gameObject);
        }
    }

    IEnumerator TextBlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(blinkFrequency);
            Color curColor = text.color;
            curColor.a = (curColor.a == 0) ? 1 : 0;
            text.color = curColor;
        }
    }

    void CreateBlocker()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InputBlockerCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        blocker = new GameObject("BlockerPanel");
        blocker.transform.SetParent(canvas.transform, false);
        RectTransform rt = blocker.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UnityEngine.UI.Image img = blocker.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0f);
        blocker.AddComponent<CanvasGroup>().blocksRaycasts = true;
        blocker.SetActive(false);
    }

    IEnumerator BlockInputAtStart(float duration)
    {
        blocker.SetActive(true);
        yield return new WaitForSeconds(duration);
        blocker.SetActive(false);
    }
}