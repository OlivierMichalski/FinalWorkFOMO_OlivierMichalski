using System;
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

    [Header("Story")]
    public string startNodeId = "opening";
    public List<FMVNode> nodes = new List<FMVNode>();

    private Dictionary<string, FMVNode> nodeLookup = new Dictionary<string, FMVNode>();
    private FMVNode currentNode;
    private bool choicesShown = false;
    private bool gameStarted = false;

    void Awake()
    {
        foreach (FMVNode node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.nodeId))
            {
                nodeLookup[node.nodeId] = node;
            }
        }

        HideChoices();

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

        if (currentNode.hasChoices && !choicesShown && videoPlayer.time >= currentNode.showChoicesAtSeconds)
        {
            ShowChoices();
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

        HideChoices();

        videoPlayer.Stop();
        videoPlayer.clip = currentNode.videoClip;
        videoPlayer.time = 0;
        videoPlayer.Play();

        Debug.Log("Now playing node: " + nodeId);
    }

    private void ShowChoices()
    {
        choicesShown = true;

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

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (currentNode == null) return;

        if (currentNode.hasChoices)
        {
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