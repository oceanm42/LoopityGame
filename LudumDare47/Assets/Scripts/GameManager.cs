using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private bool gameStarted = false;
    private float difficultyMaxValue;
    private float difficultyCurrentValue;
    private float pointMultiplier;
    private float currentPoints;
    private bool gameHasEnded;
    private CameraShake cameraShake;
    private bool fadingIn = false;
    private AudioManager audioManager;

    [Header("Game Settings")]
    [SerializeField]
    private float pointsToAdd;
    [SerializeField]
    private GameObject restartText;

    [Header("Loop Settings")]
    [SerializeField]
    private GameObject loopObj;
    [SerializeField]
    private SpriteRenderer[] loopPiecesSr;
    [SerializeField]
    private float loopCooldown;
    private float nextLoopTime;
    private int currentIndex;
    [SerializeField]
    private bool movingForward = true;
    [SerializeField]
    [Range(0f, .99f)]
    private float decreaseCoolDownPercentage;
    [SerializeField]
    private Color playerColor;
    [SerializeField]
    private Color defaultPieceColor;
    [SerializeField]
    private Animator loopAnimator;

    [Header("Piece Settings")]
    [SerializeField]
    private GameObject[] piecePrefabs;
    private int pieceIndex;
    private GameObject currentPiece;
    [SerializeField]
    private Color activePieceColor;
    [SerializeField]
    private GameObject hitParticles;

    [Header("UI Values")]
    [SerializeField]
    private TextMeshProUGUI loopDelayText;
    [SerializeField]
    private TextMeshProUGUI speedMultiplierText;
    [SerializeField]
    private Slider loopDelaySlider;
    [SerializeField]
    private Slider speedMultiplierSlider;
    [SerializeField]
    private GameObject currentPointsObj;
    [SerializeField]
    private TextMeshProUGUI currentPointsText;
    [SerializeField]
    private Animator gameSettingsAnimator;
    [SerializeField]
    private Animator currentPointsAnimator;
    [SerializeField]
    private Animator fadeAnimator;

    private void Start()
    {
        speedMultiplierSlider.value = .5f;
        loopDelaySlider.value = .5f;
        cameraShake = FindObjectOfType<CameraShake>();
        audioManager = FindObjectOfType<AudioManager>();
    }

    private void SetPieceColor(int currentPiece)
    {
        for (int i = 0; i < loopPiecesSr.Length; i++)
        {
            if (i != currentPiece)
            {
                loopPiecesSr[i].color = defaultPieceColor;
            }
            else
            {
                loopPiecesSr[i].color = playerColor;
            }
        }
    }

    private void Update()
    {
        if(gameStarted == true && gameHasEnded == false)
        {
            LoopThroughWheel();
        }
        if (Input.anyKeyDown && gameStarted == true && gameHasEnded == false)
        {
            CheckPosition();
        }

        if(gameStarted == false)
        {
            ManageUI();
        }

        if(gameHasEnded == true && Input.GetKeyDown(KeyCode.Return) && fadingIn == false)
        {
            fadingIn = true;
            fadeAnimator.Play("FadeOut");
            StartCoroutine(LoadSceneDelay());
        }
    }

    private IEnumerator LoadSceneDelay()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartGame()
    {
        audioManager.Play("Select");
        loopCooldown = loopDelaySlider.value;
        decreaseCoolDownPercentage = speedMultiplierSlider.value;
        difficultyMaxValue = loopDelaySlider.maxValue + speedMultiplierSlider.maxValue;
        difficultyCurrentValue = loopDelaySlider.value + speedMultiplierSlider.value;
        pointMultiplier = difficultyCurrentValue / difficultyMaxValue;
        loopObj.SetActive(true);
        gameStarted = true;
        gameSettingsAnimator.SetTrigger("StartGame");
        currentPointsObj.SetActive(true);
        CreateNewPiece();
    }

    private void LoopThroughWheel()
    {
        if (Time.time > nextLoopTime)
        {
            nextLoopTime = Time.time + loopCooldown;
            if (movingForward == true)
            {
                if (currentIndex < loopPiecesSr.Length - 1)
                {
                    currentIndex++;
                    SetPieceColor(currentIndex);
                }
                else
                {
                    currentIndex = 0;
                    SetPieceColor(currentIndex);
                }
            }
            else
            {
                if (currentIndex > 0)
                {
                    currentIndex--;
                    SetPieceColor(currentIndex);
                }
                else
                {
                    currentIndex = loopPiecesSr.Length - 1;
                    SetPieceColor(currentIndex);
                }
            }
            audioManager.Play("Tick");
        }
    }

    private void ManageUI()
    {
        loopDelayText.text = loopDelaySlider.value.ToString("0.00");
        float multiplierValue = speedMultiplierSlider.value * 100;
        speedMultiplierText.text =  multiplierValue.ToString("0") + "%";
    }

    private void CheckPosition()
    {
        if (currentIndex == pieceIndex)
        {
            GameObject particle = Instantiate(hitParticles, currentPiece.transform.position, Quaternion.identity);
            ParticleSystem system = particle.GetComponent<ParticleSystem>();
            system.textureSheetAnimation.SetSprite(0, currentPiece.GetComponent<SpriteRenderer>().sprite);
            CreateNewPiece();
            ChangeDirection();
            if (loopCooldown > .1)
            {
                loopCooldown -= loopCooldown *= decreaseCoolDownPercentage;
            }
            IncreasePoints();
            loopAnimator.Play("ExpandLoop");
            currentPointsAnimator.Play("ExplandText");
            StartCoroutine(cameraShake.Shake(.05f, .5f));
            audioManager.Play("Hit");
        }
        else
        {
            EndGame();
        }
    }

    private void CreateNewPiece()
    {
        if (currentPiece != null)
        {
            Destroy(currentPiece);
        }
        pieceIndex = Random.Range(0, piecePrefabs.Length);
        currentPiece = Instantiate(piecePrefabs[pieceIndex], Vector3.zero, Quaternion.identity);
        SpriteRenderer cSr = currentPiece.GetComponent<SpriteRenderer>();
        cSr.color = activePieceColor;
        cSr.sortingOrder = 2;
    }

    private void ChangeDirection()
    {
        movingForward = !movingForward;
    }

    private void IncreasePoints()
    {
        currentPoints += pointsToAdd + (pointsToAdd * pointMultiplier);
        currentPointsText.text = currentPoints.ToString("0");
    }

    private void EndGame()
    {
        audioManager.Play("Loss");
        gameHasEnded = true;
        restartText.SetActive(true);
        StartCoroutine(cameraShake.Shake(.05f, .5f));
    }
}
