using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class FMVBranchingManager : MonoBehaviour
{
    [Serializable]
    public class ChoiceOption
    {
        public string choiceText;
        public string targetNodeId;
    }

    [Serializable]
    public class FMVNode
    {
        public string nodeId;
        public VideoClip videoClip;

        [Header("Choices")]
        public bool hasChoices;
        public float showChoicesAtSeconds = 5f;

        [Header("Linear next video")]
        public string nextNodeId;

        [Header("Choice options")]
        public ChoiceOption choice1;
        public ChoiceOption choice2;
        public ChoiceOption choice3;

        [Header("Small phone notification")]
        public bool showNotification;
        public float notificationAtSeconds = 3f;
        public float notificationDuration = 3f;
        public string notificationHeader = "Groupchat • now";

        [TextArea(2, 3)]
        public string notificationMessage = "New story available.";

        [Header("Phone overlay")]
        public bool showPhoneOverlay;
        public float phoneOverlayAtSeconds = 0.2f;
        public float phoneOverlayDuration = 3f;
        public string phoneOverlayHeader = "Groupchat";

        [TextArea(3, 6)]
        public string phoneOverlayMessage = "You opened your phone.";
    }

    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Start UI")]
    public GameObject startCanvas;
    public Button playButton;

    [Header("Choice UI")]
    public GameObject choiceCanvas;

    public Button choiceButton1;
    public Button choiceButton2;
    public Button choiceButton3;

    public TMP_Text choiceText1;
    public TMP_Text choiceText2;
    public TMP_Text choiceText3;

    [Header("Notification UI")]
    public GameObject notificationPopup;
    public CanvasGroup notificationCanvasGroup;
    public TMP_Text notificationHeaderText;
    public TMP_Text notificationMessageText;

    [Header("Phone Overlay UI")]
    public GameObject phoneOverlay;
    public TMP_Text phoneHeaderText;
    public TMP_Text phoneMessageText;

    [Header("Story")]
    public string startNodeId = "opening";
    public List<FMVNode> nodes = new List<FMVNode>();

    private Dictionary<string, FMVNode> nodeLookup = new Dictionary<string, FMVNode>();
    private FMVNode currentNode;

    private bool gameStarted = false;
    private bool choicesShown = false;
    private bool notificationShown = false;
    private bool phoneOverlayShown = false;

    private Coroutine notificationCoroutine;
    private Coroutine phoneOverlayCoroutine;

    void Awake()
    {
        BuildNodeLookup();

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();

        if (startCanvas != null)
            startCanvas.SetActive(true);

        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        if (!gameStarted) return;
        if (currentNode == null) return;
        if (videoPlayer == null) return;
        if (videoPlayer.clip == null) return;

        if (currentNode.showNotification && !notificationShown && videoPlayer.time >= currentNode.notificationAtSeconds)
        {
            notificationShown = true;

            ShowNotification(
                currentNode.notificationHeader,
                currentNode.notificationMessage,
                currentNode.notificationDuration
            );
        }

        if (currentNode.showPhoneOverlay && !phoneOverlayShown && videoPlayer.time >= currentNode.phoneOverlayAtSeconds)
        {
            phoneOverlayShown = true;

            ShowPhoneOverlay(
                currentNode.phoneOverlayHeader,
                currentNode.phoneOverlayMessage,
                currentNode.phoneOverlayDuration
            );
        }

        if (currentNode.hasChoices && !choicesShown && videoPlayer.time >= currentNode.showChoicesAtSeconds)
        {
            ShowChoices();
        }
    }

    private void BuildNodeLookup()
    {
        nodeLookup.Clear();

        foreach (FMVNode node in nodes)
        {
            if (node == null) continue;

            if (string.IsNullOrWhiteSpace(node.nodeId))
            {
                Debug.LogWarning("A node has an empty Node ID.");
                continue;
            }

            if (nodeLookup.ContainsKey(node.nodeId))
            {
                Debug.LogWarning("Duplicate Node ID found: " + node.nodeId);
                continue;
            }

            nodeLookup.Add(node.nodeId, node);
        }
    }

    public void StartGame()
    {
        gameStarted = true;

        if (startCanvas != null)
            startCanvas.SetActive(false);

        PlayNode(startNodeId);
    }

    public void PlayNode(string nodeId)
    {
        if (!nodeLookup.ContainsKey(nodeId))
        {
            Debug.LogError("Node not found: " + nodeId);
            return;
        }

        currentNode = nodeLookup[nodeId];

        choicesShown = false;
        notificationShown = false;
        phoneOverlayShown = false;

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned.");
            return;
        }

        if (currentNode.videoClip == null)
        {
            Debug.LogError("Video clip missing on node: " + nodeId);
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = currentNode.videoClip;
        videoPlayer.time = 0;
        videoPlayer.Play();

        Debug.Log("Now playing node: " + nodeId);
    }

    private void ShowChoices()
    {
        choicesShown = true;

        if (videoPlayer != null)
            videoPlayer.Pause();

        if (choiceCanvas != null)
            choiceCanvas.SetActive(true);

        SetupButton(choiceButton1, choiceText1, currentNode.choice1);
        SetupButton(choiceButton2, choiceText2, currentNode.choice2);
        SetupButton(choiceButton3, choiceText3, currentNode.choice3);
    }

    private void SetupButton(Button button, TMP_Text label, ChoiceOption choice)
    {
        if (button == null || label == null)
            return;

        button.onClick.RemoveAllListeners();

        if (choice == null || string.IsNullOrWhiteSpace(choice.targetNodeId))
        {
            button.gameObject.SetActive(false);
            return;
        }

        button.gameObject.SetActive(true);
        label.text = choice.choiceText;

        string target = choice.targetNodeId;
        button.onClick.AddListener(() => PlayNode(target));
    }

    private void HideChoices()
    {
        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);
    }

    private void ShowNotification(string header, string message, float duration)
    {
        if (notificationPopup == null)
            return;

        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);

        notificationCoroutine = StartCoroutine(NotificationRoutine(header, message, duration));
    }

    private IEnumerator NotificationRoutine(string header, string message, float duration)
    {
        notificationPopup.SetActive(true);

        if (notificationHeaderText != null)
            notificationHeaderText.text = header;

        if (notificationMessageText != null)
            notificationMessageText.text = message;

        if (notificationCanvasGroup == null)
            notificationCanvasGroup = notificationPopup.GetComponent<CanvasGroup>();

        if (notificationCanvasGroup == null)
            notificationCanvasGroup = notificationPopup.AddComponent<CanvasGroup>();

        notificationCanvasGroup.interactable = false;
        notificationCanvasGroup.blocksRaycasts = false;

        float fadeInTime = 0.25f;
        float fadeOutTime = 0.5f;

        for (float t = 0; t < fadeInTime; t += Time.deltaTime)
        {
            notificationCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            yield return null;
        }

        notificationCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
        {
            notificationCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            yield return null;
        }

        notificationCanvasGroup.alpha = 0f;
        notificationPopup.SetActive(false);
        notificationCoroutine = null;
    }

    private void HideNotificationImmediate()
    {
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }

        if (notificationPopup != null)
            notificationPopup.SetActive(false);

        if (notificationCanvasGroup != null)
            notificationCanvasGroup.alpha = 0f;
    }

    private void ShowPhoneOverlay(string header, string message, float duration)
    {
        if (phoneOverlayCoroutine != null)
            StopCoroutine(phoneOverlayCoroutine);

        phoneOverlayCoroutine = StartCoroutine(PhoneOverlayRoutine(header, message, duration));
    }

    private IEnumerator PhoneOverlayRoutine(string header, string message, float duration)
    {
        if (phoneOverlay != null)
            phoneOverlay.SetActive(true);

        if (phoneHeaderText != null)
            phoneHeaderText.text = header;

        if (phoneMessageText != null)
            phoneMessageText.text = message;

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);

            if (phoneOverlay != null)
                phoneOverlay.SetActive(false);
        }

        phoneOverlayCoroutine = null;
    }

    private void HidePhoneOverlayImmediate()
    {
        if (phoneOverlayCoroutine != null)
        {
            StopCoroutine(phoneOverlayCoroutine);
            phoneOverlayCoroutine = null;
        }

        if (phoneOverlay != null)
            phoneOverlay.SetActive(false);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (currentNode == null)
            return;

        if (currentNode.hasChoices)
            return;

        if (!string.IsNullOrWhiteSpace(currentNode.nextNodeId))
        {
            PlayNode(currentNode.nextNodeId);
        }
        else
        {
            Debug.Log("End of story path.");
        }
    }
}