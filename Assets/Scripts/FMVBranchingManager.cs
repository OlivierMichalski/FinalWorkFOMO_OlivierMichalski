using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    public class PhoneTimedMessage
    {
        public float delay = 0f;
        public string sender = "Groupchat";

        [TextArea(2, 4)]
        public string message = "New message.";
    }

    [Serializable]
    public class EndingTextPart
    {
        [TextArea(3, 10)]
        public string text;

        public float displayDuration = 4f;
        public float fadeTime = 0.5f;

        [Header("Line by line reveal")]
        public bool revealLineByLine = false;
        public bool fadeInEachLine = true;
        public float lineDelay = 0.7f;
        public float lineFadeTime = 0.35f;

        public float pauseAfter = 0.4f;
        public bool keepOnScreen = false;
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
        public float phoneOverlayDuration = 4f;
        public string phoneOverlayHeader = "Groupchat";

        [TextArea(3, 6)]
        public string phoneOverlayMessage = "You opened your phone.";

        [Header("Timed phone messages")]
        public List<PhoneTimedMessage> timedPhoneMessages = new List<PhoneTimedMessage>();

        [Header("Ending screen")]
        public bool showEndingOnVideoEnd;
        public float endingFadeInTime = 0.8f;
        public bool showEndingButtonsOnComplete = true;
        public List<EndingTextPart> endingTextParts = new List<EndingTextPart>();
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

    [Header("Ending UI")]
    public GameObject endingOverlay;
    public CanvasGroup endingCanvasGroup;
    public TMP_Text endingText;

    [Header("Ending Buttons UI")]
    public GameObject endingButtonsPanel;
    public Button replayButton;
    public Button homeButton;

    [Header("Story")]
    public string startNodeId = "opening";
    public List<FMVNode> nodes = new List<FMVNode>();

    private Dictionary<string, FMVNode> nodeLookup = new Dictionary<string, FMVNode>();
    private FMVNode currentNode;

    private bool gameStarted = false;
    private bool choicesShown = false;
    private bool notificationShown = false;
    private bool phoneOverlayShown = false;
    private bool endingStarted = false;

    private Coroutine notificationCoroutine;
    private Coroutine phoneOverlayCoroutine;
    private Coroutine endingCoroutine;

    void Awake()
    {
        BuildNodeLookup();

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();
        HideEndingImmediate();

        if (startCanvas != null)
            startCanvas.SetActive(true);

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartGame);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayGame);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(GoToHomeScreen);
        }

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        if (!gameStarted) return;
        if (currentNode == null) return;
        if (videoPlayer == null) return;
        if (videoPlayer.clip == null) return;
        if (endingStarted) return;

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
            ShowPhoneOverlay(currentNode);
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
        BuildNodeLookup();

        gameStarted = true;

        if (startCanvas != null)
            startCanvas.SetActive(false);

        PlayNode(startNodeId);
    }

    public void ReplayGame()
    {
        BuildNodeLookup();

        gameStarted = true;

        HideEndingImmediate();
        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();

        if (startCanvas != null)
            startCanvas.SetActive(false);

        PlayNode(startNodeId);
    }

    public void GoToHomeScreen()
    {
        gameStarted = false;
        endingStarted = false;
        currentNode = null;

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();
        HideEndingImmediate();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        if (startCanvas != null)
            startCanvas.SetActive(true);
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
        endingStarted = false;

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();
        HideEndingImmediate();

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

    private void ShowPhoneOverlay(FMVNode node)
    {
        if (phoneOverlayCoroutine != null)
            StopCoroutine(phoneOverlayCoroutine);

        phoneOverlayCoroutine = StartCoroutine(PhoneOverlayRoutine(node));
    }

    private IEnumerator PhoneOverlayRoutine(FMVNode node)
    {
        if (phoneOverlay != null)
            phoneOverlay.SetActive(true);

        if (phoneHeaderText != null)
            phoneHeaderText.text = node.phoneOverlayHeader;

        if (phoneMessageText != null)
            phoneMessageText.text = "";

        List<PhoneTimedMessage> messages = new List<PhoneTimedMessage>();

        if (node.timedPhoneMessages != null)
        {
            foreach (PhoneTimedMessage msg in node.timedPhoneMessages)
            {
                if (msg != null)
                    messages.Add(msg);
            }
        }

        messages.Sort((a, b) => a.delay.CompareTo(b.delay));

        if (messages.Count == 0 && phoneMessageText != null)
        {
            phoneMessageText.text = node.phoneOverlayMessage;
        }

        float elapsed = 0f;
        int messageIndex = 0;

        while (true)
        {
            elapsed += Time.deltaTime;

            while (messageIndex < messages.Count && elapsed >= messages[messageIndex].delay)
            {
                AddPhoneMessage(messages[messageIndex]);
                messageIndex++;
            }

            if (node.phoneOverlayDuration > 0f && elapsed >= node.phoneOverlayDuration)
            {
                break;
            }

            if (node.phoneOverlayDuration <= 0f && messageIndex >= messages.Count)
            {
                phoneOverlayCoroutine = null;
                yield break;
            }

            yield return null;
        }

        if (phoneOverlay != null)
            phoneOverlay.SetActive(false);

        phoneOverlayCoroutine = null;
    }

    private void AddPhoneMessage(PhoneTimedMessage timedMessage)
    {
        if (phoneMessageText == null)
            return;

        string sender = timedMessage.sender;
        string message = timedMessage.message;

        if (string.IsNullOrWhiteSpace(sender))
            sender = "Message";

        string formattedMessage =
            "<b>" + sender + "</b>\n" +
            message + "\n\n";

        phoneMessageText.text += formattedMessage;
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

    private void StartEndingSequence(FMVNode node)
    {
        if (endingCoroutine != null)
            StopCoroutine(endingCoroutine);

        endingCoroutine = StartCoroutine(EndingRoutine(node));
    }

    private IEnumerator EndingRoutine(FMVNode node)
    {
        endingStarted = true;

        HideChoices();
        HideNotificationImmediate();
        HidePhoneOverlayImmediate();
        HideEndingButtons();

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (endingOverlay != null)
            endingOverlay.SetActive(true);

        if (endingCanvasGroup == null && endingOverlay != null)
            endingCanvasGroup = endingOverlay.GetComponent<CanvasGroup>();

        if (endingCanvasGroup == null && endingOverlay != null)
            endingCanvasGroup = endingOverlay.AddComponent<CanvasGroup>();

        if (endingCanvasGroup != null)
        {
            endingCanvasGroup.interactable = true;
            endingCanvasGroup.blocksRaycasts = true;
            endingCanvasGroup.alpha = 0f;

            for (float t = 0; t < node.endingFadeInTime; t += Time.deltaTime)
            {
                endingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / node.endingFadeInTime);
                yield return null;
            }

            endingCanvasGroup.alpha = 1f;
        }

        if (endingText != null)
        {
            endingText.richText = true;
            endingText.text = "";
            SetEndingTextAlpha(0f);
        }

        if (node.endingTextParts == null || node.endingTextParts.Count == 0)
        {
            Debug.LogWarning("Ending started, but no ending text parts were added.");
            ShowEndingButtonsIfNeeded(node);
            yield break;
        }

        foreach (EndingTextPart part in node.endingTextParts)
        {
            if (part == null) continue;

            if (endingText != null)
                endingText.text = "";

            if (part.revealLineByLine)
            {
                SetEndingTextAlpha(1f);
                yield return StartCoroutine(RevealTextLineByLine(part));
            }
            else
            {
                if (endingText != null)
                    endingText.text = part.text;

                yield return StartCoroutine(FadeEndingText(0f, 1f, part.fadeTime));
            }

            yield return new WaitForSeconds(part.displayDuration);

            if (part.keepOnScreen)
            {
                ShowEndingButtonsIfNeeded(node);
                yield break;
            }

            yield return StartCoroutine(FadeEndingText(1f, 0f, part.fadeTime));

            if (part.pauseAfter > 0f)
                yield return new WaitForSeconds(part.pauseAfter);
        }

        ShowEndingButtonsIfNeeded(node);
        endingCoroutine = null;
    }

    private IEnumerator RevealTextLineByLine(EndingTextPart part)
    {
        if (endingText == null)
            yield break;

        string[] lines = part.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        endingText.richText = true;
        endingText.text = "";

        for (int i = 0; i < lines.Length; i++)
        {
            if (part.fadeInEachLine && !string.IsNullOrWhiteSpace(lines[i]))
            {
                float fadeTime = Mathf.Max(0.01f, part.lineFadeTime);

                for (float t = 0; t < fadeTime; t += Time.deltaTime)
                {
                    float progress = t / fadeTime;
                    int alpha = Mathf.RoundToInt(Mathf.Lerp(0, 255, progress));
                    endingText.text = BuildLineFadeText(lines, i, alpha);
                    yield return null;
                }

                endingText.text = BuildLineFadeText(lines, i, 255);
            }
            else
            {
                endingText.text = BuildLineFadeText(lines, i, 255);
            }

            yield return new WaitForSeconds(part.lineDelay);
        }
    }

    private string BuildLineFadeText(string[] lines, int currentLineIndex, int currentAlpha)
    {
        StringBuilder builder = new StringBuilder();

        currentAlpha = Mathf.Clamp(currentAlpha, 0, 255);
        string currentAlphaHex = currentAlpha.ToString("X2");

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                builder.Append("\n");

            if (i < currentLineIndex)
            {
                builder.Append("<alpha=#FF>");
                builder.Append(lines[i]);
            }
            else if (i == currentLineIndex)
            {
                builder.Append("<alpha=#");
                builder.Append(currentAlphaHex);
                builder.Append(">");
                builder.Append(lines[i]);
            }
            else
            {
                builder.Append("<alpha=#00>");
                builder.Append(lines[i]);
            }
        }

        builder.Append("<alpha=#FF>");
        return builder.ToString();
    }

    private IEnumerator FadeEndingText(float from, float to, float duration)
    {
        if (endingText == null)
            yield break;

        if (duration <= 0f)
        {
            SetEndingTextAlpha(to);
            yield break;
        }

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(from, to, t / duration);
            SetEndingTextAlpha(alpha);
            yield return null;
        }

        SetEndingTextAlpha(to);
    }

    private void SetEndingTextAlpha(float alpha)
    {
        if (endingText == null)
            return;

        Color color = endingText.color;
        color.a = alpha;
        endingText.color = color;
    }

    private void ShowEndingButtonsIfNeeded(FMVNode node)
    {
        if (node != null && node.showEndingButtonsOnComplete)
            ShowEndingButtons();
    }

    private void ShowEndingButtons()
    {
        if (endingButtonsPanel != null)
            endingButtonsPanel.SetActive(true);
    }

    private void HideEndingButtons()
    {
        if (endingButtonsPanel != null)
            endingButtonsPanel.SetActive(false);
    }

    private void HideEndingImmediate()
    {
        if (endingCoroutine != null)
        {
            StopCoroutine(endingCoroutine);
            endingCoroutine = null;
        }

        HideEndingButtons();

        if (endingOverlay != null)
            endingOverlay.SetActive(false);

        if (endingCanvasGroup != null)
            endingCanvasGroup.alpha = 0f;

        if (endingText != null)
            endingText.text = "";
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (currentNode == null)
            return;

        if (currentNode.hasChoices)
            return;

        if (currentNode.showEndingOnVideoEnd)
        {
            StartEndingSequence(currentNode);
            return;
        }

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