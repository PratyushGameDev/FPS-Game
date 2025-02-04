using Photon.Realtime;
using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreBoard : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform container;
    [SerializeField] GameObject scoreBoardItemPrefab;
    public GameObject blur;
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField] CanvasGroup scoreBoardTitle;
    [SerializeField] TMP_Text holdTabForScoreBoardText;

    public CanvasGroup leaveConfirmation;
    public CanvasGroup options;

    public bool isConfirmationOpen = false;

    Dictionary<Player, ScoreBoardItem> scoreBoardItems = new();

    private void Start()
    {
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            AddScoreBoardItem(player);
        }
        scoreBoardTitle.alpha = 0;
        leaveConfirmation.alpha = 0;
        options.alpha = 0;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddScoreBoardItem(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RemoveScoreBoardItem(otherPlayer);
    }

    void AddScoreBoardItem(Player player)
    {
        ScoreBoardItem item = Instantiate(scoreBoardItemPrefab, container).GetComponent<ScoreBoardItem>();
        item.Initialize(player);
        scoreBoardItems[player] = item;
    }

    void RemoveScoreBoardItem(Player player)
    {
        Destroy(scoreBoardItems[player].gameObject);
        scoreBoardItems.Remove(player);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            canvasGroup.alpha = 1;
            transform.root.GetComponent<Animator>().Play("ScoreBoardTransition", 0, 0f);
            scoreBoardTitle.alpha = 1;
            holdTabForScoreBoardText.color = new Color(holdTabForScoreBoardText.color.r, holdTabForScoreBoardText.color.g, holdTabForScoreBoardText.color.b, 0);
            blur.SetActive(true);
        }
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            canvasGroup.alpha = 0;
            transform.root.GetComponent<Animator>().Play("ScoreBoardIdleAnimation", 0, 0f);
            scoreBoardTitle.alpha = 0;
            blur.SetActive(false);

            holdTabForScoreBoardText.color = new Color(holdTabForScoreBoardText.color.r, holdTabForScoreBoardText.color.g, holdTabForScoreBoardText.color.b, 1);
        }                
    }

    public void OpenOptions() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            options.GetComponent<Animator>().Play("OptionsTransition");
            isConfirmationOpen = true;
            blur.SetActive(true);
            DisableCursor();
        }
    }

    public void OpenLeaveConfirmation()
    {
        leaveConfirmation.alpha = 1;
            leaveConfirmation.GetComponent<Animator>().Play("LeaveConfirmationTransition");
            DisableCursor();
            isConfirmationOpen = true;
            blur.SetActive(true);
    }

    public void DisableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseLeave()
    {
        isConfirmationOpen = false;
    }
}
