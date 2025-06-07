using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CardController : MonoBehaviour
{
    [SerializeField] Cards cardPrefab;
    [SerializeField] Sprite[] sprites;
    
    [Header("Game Mechanics")]
    public Transform gridTransform;
    public GridLayoutGroup gridLayout;
    public Text completedText;
    public Text scoreText;
    public Button nextLevelButton;
    public Image blackoutPanel;

    [Header("Sound Effects")]
    public AudioClip flipSound;
    public AudioClip matchSound;
    public AudioClip mismatchSound;
    public Text attemptsText;
    public Text timerText;
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;

    private AudioSource audioSource;
    private List<Sprite> spritePairs;
    private Cards firstSelected;
    private Cards secondSelected;
    private bool isChecking = false;
    private int score = 0;
    private int consecutiveMatches = 0;
    private int consecutiveMismatches = 0;
    private int matchCounts = 0;
    private int totalCards = 0;
    private List<Cards> createdCards = new List<Cards>();
    private int attempts = 0;
    private float timeLeft;
    private bool timerRunning = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        completedText?.gameObject.SetActive(false);
        nextLevelButton?.gameObject.SetActive(false);
        blackoutPanel.color = new Color(0, 0, 0, 0);
        nextLevelButton.onClick.AddListener(NextLevel);
        restartButton.onClick.AddListener(RestartLevel);
        SetupNewBoard();
    }

    void SetupNewBoard()
    {
        foreach (Transform child in gridTransform)
            Destroy(child.gameObject);

        firstSelected = null;
        secondSelected = null;
        isChecking = false;
        score = 0;
        matchCounts = 0;
        consecutiveMatches = 0;
        consecutiveMismatches = 0;

        completedText?.gameObject.SetActive(false);
        nextLevelButton?.gameObject.SetActive(false);

        attempts = 0;
        UpdateAttempts();

        timeLeft = CalculateTimerDuration();
        timerRunning = true;
        UpdateTimer();

        gameOverPanel?.SetActive(false);
        UpdateScore();

        CreateGrid();
        CreateCards();
    }

    float CalculateTimerDuration()
    {
        return 60f;
    }

    void CreateGrid()
    {
        spritePairs = new List<Sprite>();
        int[] possibleRows = { 2, 3 };
        int rows = possibleRows[Random.Range(0, possibleRows.Length)];
        int maxCols = 7;

        List<int> validCols = new List<int>();
        for (int cols = 2; cols <= maxCols; cols++)
        {
            if ((rows * cols) % 2 == 0) validCols.Add(cols);
        }

        int chosenCols = validCols[Random.Range(0, validCols.Count)];
        totalCards = rows * chosenCols;

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = chosenCols;

        List<Sprite> availableSprites = new List<Sprite>(sprites);
        while (spritePairs.Count < totalCards)
        {
            if (availableSprites.Count == 0) availableSprites = new List<Sprite>(sprites);
            Sprite randomSprite = availableSprites[Random.Range(0, availableSprites.Count)];
            availableSprites.Remove(randomSprite);
            spritePairs.Add(randomSprite);
            spritePairs.Add(randomSprite);
        }

        ShuffleSprites(spritePairs);
    }

    void CreateCards()
    {
        createdCards.Clear();
        foreach (Sprite sprite in spritePairs)
        {
            Cards card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(sprite);
            card.controller = this;
            createdCards.Add(card); 
        }
    }

    public void PlayFlipSound()
    {
        if (flipSound) audioSource.PlayOneShot(flipSound);
    }

    public void SetSelected(Cards card)
    {
        if (isChecking || card.isSelected || card.isMatched) return;

        card.Show();

        if (firstSelected == null)
        {
            firstSelected = card;
        }
        else if (secondSelected == null)
        {
            secondSelected = card;
            attempts++;
            UpdateAttempts();
            StartCoroutine(CheckMatching());
        }
    }

    void UpdateAttempts()
    {
        if (attemptsText != null)
            attemptsText.text = "Attempts: " + attempts;
    }

    IEnumerator CheckMatching()
    {
        isChecking = true;
        yield return new WaitForSeconds(0.5f);

        if (firstSelected.iconSprite == secondSelected.iconSprite)
        {
            audioSource.PlayOneShot(matchSound);
            firstSelected.SetMatched(true);
            secondSelected.SetMatched(true);
            score = (consecutiveMatches == 0) ? score + 1 : score * 2;
            consecutiveMatches++;
            consecutiveMismatches = 0;
            matchCounts++;

            if (matchCounts >= totalCards / 2)
            {
                timerRunning = false;
                StartCoroutine(PlayCompletionAnimation());
            }
        }
        else
        {
            audioSource.PlayOneShot(mismatchSound);
            yield return new WaitForSeconds(0.5f);
            firstSelected.Hide();
            secondSelected.Hide();

            if (consecutiveMatches > 0)
            {
                consecutiveMatches = 0;
                consecutiveMismatches = 1;
            }
            else
            {
                consecutiveMismatches++;
                if (consecutiveMismatches >= 2 && score > 0)
                {
                    score -= 1;
                    consecutiveMismatches = 0;
                }
            }
        }

        UpdateScore();

        firstSelected = null;
        secondSelected = null;
        isChecking = false;
    }

    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    void ShuffleSprites(List<Sprite> spriteList)
    {
        for (int i = spriteList.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            Sprite temp = spriteList[i];
            spriteList[i] = spriteList[rand];
            spriteList[rand] = temp;
        }
    }

    IEnumerator PlayCompletionAnimation()
    {
        Vector3 originalScale = gridTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            gridTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        gridTransform.localScale = targetScale;
        elapsed = 0f;
        duration = 0.1f;

        while (elapsed < duration)
        {
            gridTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        gridTransform.localScale = originalScale;
        completedText?.gameObject.SetActive(true);
        StartCoroutine(BobCompletedText());
        nextLevelButton?.gameObject.SetActive(true);
    }

    IEnumerator BobCompletedText()
    {
        Vector3 baseScale = Vector3.one;
        float bobSpeed = 2f;
        float bobAmount = 0.1f;

        while (completedText.gameObject.activeSelf)
        {
            float scale = 1f + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            completedText.transform.localScale = baseScale * scale;
            yield return null;
        }
    }

    void NextLevel()
    {
        StartCoroutine(BlackoutAndReset());
    }

    IEnumerator BlackoutAndReset()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            blackoutPanel.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        blackoutPanel.color = Color.black;
        yield return new WaitForSeconds(0.2f);

        SetupNewBoard();

        elapsed = 0f;
        while (elapsed < duration)
        {
            blackoutPanel.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        blackoutPanel.color = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        if (!timerRunning) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            GameOver();
        }

        UpdateTimer();
    }

    void UpdateTimer()
    {
        if (timerText != null)
            timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);
    }

    void GameOver()
    {
        PauseGame(true);
        gameOverPanel?.SetActive(true);
    }

    public void RestartLevel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        firstSelected = null;
        secondSelected = null;
        isChecking = false;
        score = 0;
        matchCounts = 0;
        consecutiveMatches = 0;
        consecutiveMismatches = 0;
        attempts = 0;

        UpdateScore();
        UpdateAttempts();

        timeLeft = CalculateTimerDuration();
        timerRunning = true;
        UpdateTimer();

        completedText?.gameObject.SetActive(false);
        nextLevelButton?.gameObject.SetActive(false);

        foreach (Cards card in createdCards)
        {
            card.ResetCard(); 
        }
    }
    public void PauseGame(bool isPaused)
    {
        isChecking = isPaused;

        if (isPaused)
        {
            timerRunning = false;
        }
        else
        {
            if (matchCounts < totalCards / 2)
            {
                timerRunning = true;
            }
        }
    }
    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(0);
    }
}