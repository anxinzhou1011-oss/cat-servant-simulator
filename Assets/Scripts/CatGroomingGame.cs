using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum GroomingTool
{
    Hand,
    Brush
}

public enum CatExpression
{
    Normal,
    SlightlyAngry,
    Angry,
    SlightlySatisfying,
    Satisfying,
    Hiss,
    Sleep
}

[System.Serializable]
public class CatProfile
{
    public string catName = "Cat";
    public Sprite normalSprite;
    public Sprite slightlyAngrySprite;
    public Sprite angrySprite;
    public Sprite slightlySatisfyingSprite;
    public Sprite satisfyingSprite;
    public Sprite hissSprite;
    public Sprite sleepSprite;
    public int passMoodMin = 55;
    public int passMoodMax = 65;
    public int maxAnger = 10;
    public int angrySpriteThreshold = 6;
    public int satisfyingSpriteThreshold = 60;
}

public class CatGroomingGame : MonoBehaviour
{
    [Header("Game Values")]
    public int satisfaction;
    public int anger;
    public int passValue = 60;
    public int maxAnger = 10;
    public int maxSatisfaction = 100;
    public int perfectMin = 75;
    public int perfectMax = 90;

    [Header("Current Tool")]
    public GroomingTool currentTool = GroomingTool.Hand;
    public KeyCode switchToolKey = KeyCode.LeftShift;

    [Header("Click Detection")]
    public Camera gameplayCamera;
    public LayerMask catPartLayers = ~0;
    public float heldTouchInterval = 0.7f;

    [Header("Tool Cursors")]
    public Texture2D handCursorTexture;
    public Texture2D brushCursorTexture;
    public Vector2 handCursorHotSpot = new Vector2(32, 20);
    public Vector2 brushCursorHotSpot = new Vector2(36, 58);
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Cat Sprites")]
    public CatProfile[] catProfiles;
    public string currentCatName;
    public SpriteRenderer catSpriteRenderer;
    public Sprite normalCatSprite;
    public Sprite angryCatSprite;
    public Sprite satisfyingCatSprite;
    public Vector2 catVisualCenter = new Vector2(2.3f, -0.95f);
    public float catVisualHeight = 5.8f;
    public int angrySpriteThreshold = 6;
    public int satisfyingSpriteThreshold = 60;

    [Header("UI Text")]
    public Font uiFont;
    public Color uiTextColor = new Color(0.43f, 0.29f, 0.19f);
    public Color resultTextColor = new Color(0.62f, 0.36f, 0.16f);
    public Text satisfactionText;
    public Text angerText;
    public Text toolText;
    public Text reactionText;
    public Text resultText;
    public Button finishButton;
    public Button restartButton;

    [Header("UI Bars")]
    public Slider moodBar;
    public Slider angerBar;
    public float barFillSpeed = 80f;
    public int moodBarVisibleGames = 3;

    [Header("Result Screens")]
    public float resultScreenDuration = 1f;
    public float sleepResultDelay = 3f;
    public float hissResultDelay = 0.75f;
    public Image resultScreenImage;
    public Image openingImage;
    public float openingHoldDuration = 1f;
    public float openingFadeDuration = 0.5f;

    [Header("Cat Conditions")]
    public float impatientChance = 0.3f;
    public float relaxedChance = 0.2f;
    public float sleepyChance = 0.1f;
    public float impatientAngerInterval = 3f;
    public float sleepyDuration = 15f;
    public int sleepyPassSatisfaction = 50;

    [Header("Audio")]
    public AudioClip buttonClickSound;
    public float buttonClickVolume = 0.8f;

    [Header("Auto UI")]
    public bool createUIOnStart = true;

    private bool gameEnded;
    private Image moodFillImage;
    private Image angerFillImage;
    private float displayedSatisfaction;
    private float displayedAnger;
    private Coroutine resultScreenRoutine;
    private Coroutine sleepResultRoutine;
    private Coroutine hissResultRoutine;
    private Coroutine openingRoutine;
    private AspectRatioFitter resultScreenAspectFitter;
    private AspectRatioFitter openingAspectFitter;
    private CanvasGroup openingCanvasGroup;
    private float nextHeldTouchTime;
    private bool touchStartedOverUI;
    private Sprite slightlyAngryCatSprite;
    private Sprite slightlySatisfyingCatSprite;
    private Sprite hissCatSprite;
    private Sprite sleepCatSprite;
    private int passMoodMin;
    private int passMoodMax;
    private int currentPassMood;
    private int gamesStarted;
    private bool hasImpatientCondition;
    private bool hasRelaxedCondition;
    private bool hasSleepyCondition;
    private float nextImpatientAngerTime;
    private float sleepyConditionEndTime;
    private float pendingAngerRemainder;
    private Image impatientConditionImage;
    private Image relaxedConditionImage;
    private Image sleepyConditionImage;
    private int currentCatProfileIndex = -1;
    private CatExpression latestExpression = CatExpression.Normal;
    private AudioSource sfxAudioSource;

    private void Start()
    {
        if (createUIOnStart)
        {
            CreateDefaultUIIfNeeded();
        }

        SetupAudio();
        SetupCatSpriteRenderer();
        gamesStarted = 1;
        SelectRandomCatProfile();
        RollCatConditions();
        UpdateUI();
        UpdateMoodBarVisibility();
        displayedSatisfaction = satisfaction;
        displayedAnger = anger;
        UpdateAnimatedBars(true);
        UpdateCatSprite();
        UpdateCursor();
        ShowReaction("Use your hand to learn what the cat likes.");
        ShowOpeningScreen();
    }

    private void Update()
    {
        UpdateAnimatedBars(false);

        if (gameEnded)
        {
            return;
        }

        UpdateCatConditions();

        if (gameEnded)
        {
            return;
        }

        if (Input.GetKeyDown(switchToolKey) || Input.GetKeyDown(KeyCode.RightShift))
        {
            SwitchTool();
        }

        if (Input.GetMouseButtonDown(0))
        {
            touchStartedOverUI = IsPointerOverUI();

            if (!touchStartedOverUI)
            {
                TryTouchPartAtMousePosition();
                nextHeldTouchTime = Time.time + GetHeldTouchInterval();
            }
        }

        if (Input.GetMouseButton(0) && !touchStartedOverUI && Time.time >= nextHeldTouchTime)
        {
            TryTouchPartAtMousePosition();
            nextHeldTouchTime = Time.time + GetHeldTouchInterval();
        }

        if (Input.GetMouseButtonUp(0))
        {
            touchStartedOverUI = false;
        }
    }

    public void SwitchTool()
    {
        currentTool = currentTool == GroomingTool.Hand ? GroomingTool.Brush : GroomingTool.Hand;

        if (currentTool == GroomingTool.Hand)
        {
            ShowReaction("Hand mode: observe the cat's habits.");
        }
        else
        {
            ShowReaction("Brush mode: groom the places the cat seems to like.");
        }

        UpdateUI();
        UpdateCatSprite();
        UpdateCursor();
    }

    public void TouchPart(CatPart part)
    {
        if (gameEnded || part == null)
        {
            return;
        }

        CatPartReaction reaction = part.GetReaction(currentTool);

        satisfaction += reaction.satisfactionChange;
        AddAnger(reaction.angerChange);

        if (satisfaction >= maxSatisfaction)
        {
            satisfaction = maxSatisfaction;
            latestExpression = CatExpression.Hiss;
            ShowReaction("The cat has had enough.");
            EndGameByHiss();
            UpdateUI();
            return;
        }
        else
        {
            ShowReaction(reaction.message);
        }

        if (reaction.angerChange <= 0 && reaction.satisfactionChange > 0)
        {
            RecordSatisfactionExpression();
        }

        if (TryStartSleepVictory())
        {
            UpdateUI();
            return;
        }

        CheckAngerLimit();

        if (gameEnded)
        {
            UpdateUI();
            return;
        }

        UpdateUI();
        UpdateCatSprite();
    }

    public void EndGrooming()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        UpdateConditionIcons();

        if (anger >= maxAnger)
        {
            ShowResultScreen(false);
        }
        else if (satisfaction >= maxSatisfaction)
        {
            ShowResultScreen(false);
        }
        else if (satisfaction >= currentPassMood)
        {
            ShowResultScreen(true);
        }
        else
        {
            ShowResultScreen(false);
        }
    }

    public void RestartGame()
    {
        if (sleepResultRoutine != null)
        {
            StopCoroutine(sleepResultRoutine);
            sleepResultRoutine = null;
        }

        if (resultScreenRoutine != null)
        {
            StopCoroutine(resultScreenRoutine);
            resultScreenRoutine = null;
        }

        if (hissResultRoutine != null)
        {
            StopCoroutine(hissResultRoutine);
            hissResultRoutine = null;
        }

        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        satisfaction = 0;
        anger = 0;
        currentTool = GroomingTool.Hand;
        gameEnded = false;
        touchStartedOverUI = false;
        nextHeldTouchTime = 0f;
        gamesStarted++;
        SelectRandomCatProfile();
        RollCatConditions();
        SetResult("");
        HideResultScreen();
        ShowReaction("Use your hand to learn what the cat likes.");
        UpdateUI();
        UpdateMoodBarVisibility();
        displayedSatisfaction = satisfaction;
        displayedAnger = anger;
        UpdateAnimatedBars(true);
        UpdateCatSprite();
        UpdateCursor();
        ShowOpeningScreen();
    }

    private void EndGameByAnger()
    {
        EndGameByHiss();
    }

    private void EndGameByHiss()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        UpdateConditionIcons();
        latestExpression = CatExpression.Hiss;

        if (catSpriteRenderer != null && hissCatSprite != null)
        {
            SetCatSprite(hissCatSprite);
        }

        if (hissResultRoutine != null)
        {
            StopCoroutine(hissResultRoutine);
        }

        hissResultRoutine = StartCoroutine(ShowHissFailureRoutine());
    }

    private IEnumerator ShowHissFailureRoutine()
    {
        yield return new WaitForSeconds(hissResultDelay);

        hissResultRoutine = null;
        ShowResultScreen(false);
    }

    private void AddAnger(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        float adjustedAmount = hasRelaxedCondition ? amount * (2f / 3f) : amount;
        pendingAngerRemainder += adjustedAmount;

        int wholeAnger = Mathf.FloorToInt(pendingAngerRemainder);
        if (wholeAnger <= 0)
        {
            return;
        }

        anger += wholeAnger;
        pendingAngerRemainder -= wholeAnger;
        RecordAngerExpression();
    }

    private void CheckAngerLimit()
    {
        if (anger >= maxAnger)
        {
            anger = maxAnger;
            EndGameByAnger();
        }
    }

    private bool TryStartSleepVictory()
    {
        if (!hasSleepyCondition || Time.time > sleepyConditionEndTime || satisfaction < sleepyPassSatisfaction)
        {
            return false;
        }

        gameEnded = true;
        hasSleepyCondition = false;
        UpdateConditionIcons();

        if (catSpriteRenderer != null && sleepCatSprite != null)
        {
            latestExpression = CatExpression.Sleep;
            SetCatSprite(sleepCatSprite);
        }

        if (sleepResultRoutine != null)
        {
            StopCoroutine(sleepResultRoutine);
        }

        sleepResultRoutine = StartCoroutine(ShowSleepVictoryRoutine());
        return true;
    }

    private IEnumerator ShowSleepVictoryRoutine()
    {
        yield return new WaitForSeconds(sleepResultDelay);

        sleepResultRoutine = null;
        ShowResultScreen(true);
    }

    private void TryTouchPartAtMousePosition()
    {
        Camera cameraToUse = gameplayCamera != null ? gameplayCamera : Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        Vector2 worldPoint = cameraToUse.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint, catPartLayers);
        CatPart bestPart = null;

        foreach (Collider2D hit in hits)
        {
            CatPart part = hit.GetComponent<CatPart>();

            if (part == null)
            {
                part = hit.GetComponentInParent<CatPart>();
            }

            if (part == null)
            {
                continue;
            }

            if (bestPart == null || IsBetterHit(part, bestPart, hit, worldPoint))
            {
                bestPart = part;
            }
        }

        if (bestPart == null)
        {
            bestPart = FindClosestPartOnCat(worldPoint);
        }

        if (bestPart != null)
        {
            TouchPart(bestPart);
        }
    }

    private CatPart FindClosestPartOnCat(Vector2 worldPoint)
    {
        if (catSpriteRenderer == null)
        {
            return null;
        }

        Bounds catBounds = catSpriteRenderer.bounds;
        catBounds.Expand(0.35f);

        if (!catBounds.Contains(worldPoint))
        {
            return null;
        }

        CatPart[] parts = catSpriteRenderer.GetComponentsInChildren<CatPart>();
        CatPart closestPart = null;
        float closestDistance = float.MaxValue;

        foreach (CatPart part in parts)
        {
            Collider2D partCollider = part.GetComponent<Collider2D>();
            Vector2 partCenter = partCollider != null ? (Vector2)partCollider.bounds.center : (Vector2)part.transform.position;
            float distance = (partCenter - worldPoint).sqrMagnitude;

            if (closestPart == null || distance < closestDistance)
            {
                closestPart = part;
                closestDistance = distance;
            }
        }

        return closestPart;
    }

    private bool IsBetterHit(CatPart candidatePart, CatPart currentPart, Collider2D candidateHit, Vector2 worldPoint)
    {
        if (candidatePart.clickPriority != currentPart.clickPriority)
        {
            return candidatePart.clickPriority > currentPart.clickPriority;
        }

        float candidateDistance = ((Vector2)candidateHit.bounds.center - worldPoint).sqrMagnitude;
        Collider2D currentHit = currentPart.GetComponent<Collider2D>();

        if (currentHit == null)
        {
            currentHit = currentPart.GetComponentInChildren<Collider2D>();
        }

        if (currentHit == null)
        {
            return true;
        }

        float currentDistance = ((Vector2)currentHit.bounds.center - worldPoint).sqrMagnitude;
        return candidateDistance < currentDistance;
    }

    private float GetHeldTouchInterval()
    {
        return Mathf.Max(0.01f, heldTouchInterval);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        System.Collections.Generic.List<RaycastResult> raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            GameObject hitObject = result.gameObject;

            if (hitObject == null)
            {
                continue;
            }

            if (openingImage != null && hitObject == openingImage.gameObject && openingImage.enabled)
            {
                return true;
            }

            if (resultScreenImage != null && hitObject == resultScreenImage.gameObject && resultScreenImage.enabled)
            {
                return true;
            }

            if (hitObject.GetComponentInParent<Button>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateUI()
    {
        if (satisfactionText != null)
        {
            satisfactionText.text = "S: " + satisfaction;
            satisfactionText.enabled = ShouldShowMoodBar();
        }

        if (angerText != null)
        {
            angerText.text = "A: " + anger;
        }

        if (toolText != null)
        {
            toolText.text = "Tool: " + currentTool;
        }

        if (moodBar != null)
        {
            moodBar.minValue = 0;
            moodBar.maxValue = maxSatisfaction;
        }

        if (angerBar != null)
        {
            angerBar.minValue = 0;
            angerBar.maxValue = maxAnger;
        }
    }

    private bool ShouldShowMoodBar()
    {
        return gamesStarted <= moodBarVisibleGames;
    }

    private void UpdateMoodBarVisibility()
    {
        bool showMoodBar = ShouldShowMoodBar();

        if (moodBar != null)
        {
            moodBar.gameObject.SetActive(showMoodBar);
        }

        if (satisfactionText != null)
        {
            satisfactionText.enabled = showMoodBar;
        }
    }

    private void UpdateAnimatedBars(bool instant)
    {
        displayedSatisfaction = instant
            ? satisfaction
            : Mathf.MoveTowards(displayedSatisfaction, satisfaction, barFillSpeed * Time.deltaTime);
        displayedAnger = instant
            ? anger
            : Mathf.MoveTowards(displayedAnger, anger, barFillSpeed * Time.deltaTime);

        if (moodBar != null)
        {
            moodBar.value = displayedSatisfaction;
        }

        if (angerBar != null)
        {
            angerBar.value = displayedAnger;
        }

        if (moodFillImage != null)
        {
            moodFillImage.fillAmount = Mathf.Clamp01(displayedSatisfaction / 100f);
        }

        if (angerFillImage != null)
        {
            angerFillImage.fillAmount = maxAnger <= 0 ? 0 : Mathf.Clamp01(displayedAnger / maxAnger);
        }
    }

    private void SetupCatSpriteRenderer()
    {
        if (catSpriteRenderer == null)
        {
            catSpriteRenderer = FindSpriteRendererUsingSprite(normalCatSprite);
        }

        if (catSpriteRenderer == null)
        {
            catSpriteRenderer = FindFirstObjectByType<SpriteRenderer>();
        }

        if (normalCatSprite == null && catSpriteRenderer != null)
        {
            normalCatSprite = catSpriteRenderer.sprite;
        }
    }

    private void SelectRandomCatProfile()
    {
        EnsureCatProfiles();

        int[] availableProfileIndices = GetAvailableCatProfileIndices();

        if (availableProfileIndices.Length == 0)
        {
            return;
        }

        int selectedIndex = availableProfileIndices[Random.Range(0, availableProfileIndices.Length)];

        if (availableProfileIndices.Length > 1)
        {
            while (selectedIndex == currentCatProfileIndex)
            {
                selectedIndex = availableProfileIndices[Random.Range(0, availableProfileIndices.Length)];
            }
        }

        currentCatProfileIndex = selectedIndex;
        ApplyCatProfile(catProfiles[selectedIndex]);
    }

    private int[] GetAvailableCatProfileIndices()
    {
        int count = 0;

        for (int i = 0; i < catProfiles.Length; i++)
        {
            if (catProfiles[i] != null && catProfiles[i].normalSprite != null)
            {
                count++;
            }
        }

        int[] indices = new int[count];
        int nextIndex = 0;

        for (int i = 0; i < catProfiles.Length; i++)
        {
            if (catProfiles[i] != null && catProfiles[i].normalSprite != null)
            {
                indices[nextIndex] = i;
                nextIndex++;
            }
        }

        return indices;
    }

    private void RollCatConditions()
    {
        pendingAngerRemainder = 0f;
        hasImpatientCondition = Random.value < impatientChance;
        hasRelaxedCondition = Random.value < relaxedChance;
        hasSleepyCondition = Random.value < sleepyChance;
        nextImpatientAngerTime = Time.time + impatientAngerInterval;
        sleepyConditionEndTime = Time.time + sleepyDuration;
        UpdateConditionIcons();
    }

    private void UpdateCatConditions()
    {
        bool iconsChanged = false;

        if (hasSleepyCondition && Time.time > sleepyConditionEndTime)
        {
            hasSleepyCondition = false;
            iconsChanged = true;
        }

        if (hasImpatientCondition && Time.time >= nextImpatientAngerTime)
        {
            AddAnger(1);
            nextImpatientAngerTime += impatientAngerInterval;
            UpdateUI();
            CheckAngerLimit();

            if (!gameEnded)
            {
                UpdateCatSprite();
            }
        }

        if (iconsChanged)
        {
            UpdateConditionIcons();
        }
    }

    private void EnsureCatProfiles()
    {
        if (catProfiles != null && catProfiles.Length > 0)
        {
            return;
        }

        catProfiles = new CatProfile[]
        {
            new CatProfile
            {
                catName = "Grey White Cat",
                normalSprite = normalCatSprite,
                slightlyAngrySprite = slightlyAngryCatSprite,
                angrySprite = angryCatSprite,
                slightlySatisfyingSprite = slightlySatisfyingCatSprite,
                satisfyingSprite = satisfyingCatSprite,
                hissSprite = hissCatSprite,
                sleepSprite = sleepCatSprite,
                passMoodMin = passValue,
                passMoodMax = passValue,
                maxAnger = maxAnger,
                angrySpriteThreshold = angrySpriteThreshold,
                satisfyingSpriteThreshold = satisfyingSpriteThreshold
            }
        };
    }

    private void ApplyCatProfile(CatProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        currentCatName = profile.catName;
        normalCatSprite = profile.normalSprite;
        slightlyAngryCatSprite = profile.slightlyAngrySprite;
        angryCatSprite = profile.angrySprite;
        slightlySatisfyingCatSprite = profile.slightlySatisfyingSprite;
        satisfyingCatSprite = profile.satisfyingSprite;
        hissCatSprite = profile.hissSprite;
        sleepCatSprite = profile.sleepSprite;
        passMoodMin = Mathf.Max(0, Mathf.Min(profile.passMoodMin, profile.passMoodMax));
        passMoodMax = Mathf.Max(passMoodMin, Mathf.Max(profile.passMoodMin, profile.passMoodMax));
        currentPassMood = Random.Range(passMoodMin, passMoodMax + 1);
        passValue = currentPassMood;
        maxAnger = Mathf.Max(1, profile.maxAnger);
        angrySpriteThreshold = Mathf.Max(1, maxAnger / 2);
        satisfyingSpriteThreshold = Mathf.CeilToInt(currentPassMood * (2f / 3f));
        latestExpression = CatExpression.Normal;

        if (catSpriteRenderer != null && normalCatSprite != null)
        {
            SetCatSprite(normalCatSprite);
        }
    }

    private SpriteRenderer FindSpriteRendererUsingSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return null;
        }

        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.sprite == sprite)
            {
                return renderer;
            }
        }

        return null;
    }

    private void UpdateCatSprite()
    {
        if (catSpriteRenderer == null)
        {
            return;
        }

        if ((anger >= maxAnger || satisfaction >= maxSatisfaction) && hissCatSprite != null)
        {
            latestExpression = CatExpression.Hiss;
            SetCatSprite(hissCatSprite);
            return;
        }

        Sprite expressionSprite = GetSpriteForExpression(latestExpression);

        if (expressionSprite != null)
        {
            SetCatSprite(expressionSprite);
            return;
        }

        if (normalCatSprite != null)
        {
            SetCatSprite(normalCatSprite);
        }
    }

    private void RecordSatisfactionExpression()
    {
        if (satisfaction >= currentPassMood)
        {
            latestExpression = CatExpression.Satisfying;
            return;
        }

        if (satisfaction >= satisfyingSpriteThreshold)
        {
            latestExpression = CatExpression.SlightlySatisfying;
            return;
        }

        latestExpression = CatExpression.Normal;
    }

    private void RecordAngerExpression()
    {
        int halfAnger = Mathf.Max(1, Mathf.CeilToInt(maxAnger / 2f));

        if (anger >= maxAnger)
        {
            latestExpression = CatExpression.Hiss;
            return;
        }

        if (anger > halfAnger)
        {
            latestExpression = CatExpression.Angry;
            return;
        }

        if (anger >= halfAnger)
        {
            latestExpression = CatExpression.SlightlyAngry;
            return;
        }

        latestExpression = CatExpression.Normal;
    }

    private Sprite GetSpriteForExpression(CatExpression expression)
    {
        switch (expression)
        {
            case CatExpression.Sleep:
                return sleepCatSprite;
            case CatExpression.Hiss:
                return hissCatSprite;
            case CatExpression.Angry:
                return angryCatSprite;
            case CatExpression.SlightlyAngry:
                return slightlyAngryCatSprite;
            case CatExpression.Satisfying:
                return satisfyingCatSprite;
            case CatExpression.SlightlySatisfying:
                return slightlySatisfyingCatSprite;
            default:
                return normalCatSprite;
        }
    }

    private void SetCatSprite(Sprite sprite)
    {
        if (catSpriteRenderer == null || sprite == null)
        {
            return;
        }

        catSpriteRenderer.sprite = sprite;
        ApplyCatVisualPlacement();
    }

    private void ApplyCatVisualPlacement()
    {
        if (catSpriteRenderer == null || catSpriteRenderer.sprite == null)
        {
            return;
        }

        Transform catTransform = catSpriteRenderer.transform;
        Bounds spriteBounds = catSpriteRenderer.sprite.bounds;
        Sprite referenceSprite = normalCatSprite != null ? normalCatSprite : catSpriteRenderer.sprite;
        Bounds referenceBounds = referenceSprite.bounds;

        if (spriteBounds.size.y <= 0f || referenceBounds.size.y <= 0f)
        {
            return;
        }

        float scale = catVisualHeight / referenceBounds.size.y;
        catTransform.localScale = new Vector3(scale, scale, catTransform.localScale.z);

        Vector3 visualCenterOffset = new Vector3(spriteBounds.center.x * scale, spriteBounds.center.y * scale, 0f);
        catTransform.position = new Vector3(catVisualCenter.x, catVisualCenter.y, catTransform.position.z) - visualCenterOffset;
    }

    private void UpdateCursor()
    {
        Texture2D cursorTexture = currentTool == GroomingTool.Hand ? handCursorTexture : brushCursorTexture;
        Vector2 hotSpot = currentTool == GroomingTool.Hand ? handCursorHotSpot : brushCursorHotSpot;

        if (cursorTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            return;
        }

        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }

    private void ShowReaction(string message)
    {
        if (reactionText != null)
        {
            reactionText.text = message;
        }
    }

    private void SetupAudio()
    {
        if (buttonClickSound == null)
        {
            buttonClickSound = Resources.Load<AudioClip>("audio/button_click");
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();

            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        sfxAudioSource.playOnAwake = false;
    }

    private void PlayButtonClickSound()
    {
        SetupAudio();

        if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
    }

    private void SetResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
    }

    private void ShowResultScreen(bool success)
    {
        if (resultScreenRoutine != null)
        {
            StopCoroutine(resultScreenRoutine);
        }

        resultScreenRoutine = StartCoroutine(ShowResultScreenRoutine(success));
    }

    private IEnumerator ShowResultScreenRoutine(bool success)
    {
        Texture2D texture = Resources.Load<Texture2D>(success ? "result_screens/congrats" : "result_screens/fail");

        if (resultScreenImage != null && texture != null)
        {
            resultScreenImage.sprite = CreateSprite(texture);
            ConfigureResultScreenImage(resultScreenImage, texture);
            resultScreenImage.enabled = true;
        }

        SetResult("");
        ShowReaction("");

        yield return new WaitForSeconds(resultScreenDuration);

        resultScreenRoutine = null;
        RestartGame();
    }

    private void HideResultScreen()
    {
        if (resultScreenImage != null)
        {
            resultScreenImage.enabled = false;
        }
    }

    private void CreateDefaultUIIfNeeded()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Grooming UI Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        Transform parent = canvas.transform;
        FindDefaultUIReferences(parent);

        if (moodBar == null)
        {
            moodBar = CreateArtBar(parent, "Mood Bar", "mood_label", "mood_bar_bg", "mood_bar_fill", new Vector2(28, -24), true);
        }

        if (angerBar == null)
        {
            angerBar = CreateArtBar(parent, "Anger Bar", "anger_label", "anger_bar_bg", "anger_bar_fill", new Vector2(28, -86), false);
        }

        if (toolText == null)
        {
            toolText = CreateText(parent, "Tool Text", new Vector2(28, -232), "Tool: Hand", 18);
            toolText.rectTransform.sizeDelta = new Vector2(320, 40);
        }

        if (reactionText == null)
        {
            reactionText = CreateText(parent, "Reaction Text", new Vector2(32, -406), "", 17);
            reactionText.alignment = TextAnchor.MiddleLeft;
            reactionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            reactionText.verticalOverflow = VerticalWrapMode.Overflow;
            reactionText.rectTransform.sizeDelta = new Vector2(340, 72);
        }

        if (resultText == null)
        {
            resultText = CreateText(parent, "Result Text", new Vector2(32, -360), "", 24);
            resultText.alignment = TextAnchor.MiddleLeft;
            resultText.color = resultTextColor;
            resultText.rectTransform.sizeDelta = new Vector2(340, 40);
        }

        if (finishButton == null)
        {
            finishButton = CreateArtButton(parent, "Finish Button", "finish_button", new Vector2(392, -430), EndGrooming);
        }

        if (restartButton == null)
        {
            restartButton = CreateArtButton(parent, "Restart Button", "restart_button", new Vector2(510, -430), RestartGame);
        }

        CreateConditionIconsIfNeeded(parent);

        if (resultScreenImage == null)
        {
            resultScreenImage = CreateResultScreenImage(parent);
        }

        if (openingImage == null)
        {
            openingImage = CreateOpeningImage(parent);
        }

        ApplyDefaultUILayout();
    }

    private void FindDefaultUIReferences(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        if (moodBar == null)
        {
            Transform found = parent.Find("Mood Bar");
            moodBar = found != null ? found.GetComponent<Slider>() : null;
        }

        if (angerBar == null)
        {
            Transform found = parent.Find("Anger Bar");
            angerBar = found != null ? found.GetComponent<Slider>() : null;
        }

        if (toolText == null)
        {
            Transform found = parent.Find("Tool Text");
            toolText = found != null ? found.GetComponent<Text>() : null;
        }

        if (reactionText == null)
        {
            Transform found = parent.Find("Reaction Text");
            reactionText = found != null ? found.GetComponent<Text>() : null;
        }

        if (resultText == null)
        {
            Transform found = parent.Find("Result Text");
            resultText = found != null ? found.GetComponent<Text>() : null;
        }

        if (finishButton == null)
        {
            Transform found = parent.Find("Finish Button");
            finishButton = found != null ? found.GetComponent<Button>() : null;
        }

        if (restartButton == null)
        {
            Transform found = parent.Find("Restart Button");
            restartButton = found != null ? found.GetComponent<Button>() : null;
        }

        if (impatientConditionImage == null)
        {
            Transform found = parent.Find("Impatient Condition Icon");
            impatientConditionImage = found != null ? found.GetComponent<Image>() : null;
        }

        if (relaxedConditionImage == null)
        {
            Transform found = parent.Find("Relaxed Condition Icon");
            relaxedConditionImage = found != null ? found.GetComponent<Image>() : null;
        }

        if (sleepyConditionImage == null)
        {
            Transform found = parent.Find("Sleepy Condition Icon");
            sleepyConditionImage = found != null ? found.GetComponent<Image>() : null;
        }
    }

    private void ApplyDefaultUILayout()
    {
        LayoutTopLeft(moodBar != null ? moodBar.GetComponent<RectTransform>() : null, new Vector2(28, -24), new Vector2(340, 56));
        LayoutTopLeft(angerBar != null ? angerBar.GetComponent<RectTransform>() : null, new Vector2(28, -86), new Vector2(340, 56));

        if (toolText != null)
        {
            LayoutBottomLeft(toolText.rectTransform, new Vector2(32, 132), new Vector2(340, 40));
            toolText.alignment = TextAnchor.MiddleLeft;
        }

        if (reactionText != null)
        {
            LayoutBottomLeft(reactionText.rectTransform, new Vector2(32, 28), new Vector2(360, 86));
            reactionText.alignment = TextAnchor.MiddleLeft;
            reactionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            reactionText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (resultText != null)
        {
            LayoutBottomLeft(resultText.rectTransform, new Vector2(32, 112), new Vector2(340, 40));
            resultText.alignment = TextAnchor.MiddleLeft;
        }

        LayoutBottomRight(finishButton != null ? finishButton.GetComponent<RectTransform>() : null, new Vector2(-142, 32), new Vector2(104, 52));
        LayoutBottomRight(restartButton != null ? restartButton.GetComponent<RectTransform>() : null, new Vector2(-24, 32), new Vector2(104, 52));

        LayoutConditionIcon(impatientConditionImage, 0);
        LayoutConditionIcon(relaxedConditionImage, 1);
        LayoutConditionIcon(sleepyConditionImage, 2);
    }

    private void LayoutTopLeft(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private void LayoutBottomLeft(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private void LayoutBottomRight(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(1, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(1, 0);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private void LayoutConditionIcon(Image image, int index)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1);
        rectTransform.anchorMax = new Vector2(0.5f, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(-64 + index * 64, -24);
        rectTransform.sizeDelta = new Vector2(52, 52);
    }

    private Image CreateResultScreenImage(Transform parent)
    {
        GameObject imageObject = new GameObject("Result Screen Image");
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;
        image.enabled = false;
        ConfigureResultScreenImage(image, null);
        imageObject.transform.SetAsLastSibling();

        return image;
    }

    private Image CreateOpeningImage(Transform parent)
    {
        GameObject imageObject = new GameObject("Opening Image");
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;
        image.enabled = false;
        openingCanvasGroup = imageObject.AddComponent<CanvasGroup>();
        openingCanvasGroup.alpha = 0f;
        ConfigureOpeningImage(image, null);
        imageObject.transform.SetAsLastSibling();

        return image;
    }

    private void ShowOpeningScreen()
    {
        if (openingImage == null)
        {
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>("result_screens/opening");

        if (texture != null)
        {
            openingImage.sprite = CreateSprite(texture);
            ConfigureOpeningImage(openingImage, texture);
        }

        if (openingCanvasGroup == null || openingCanvasGroup.gameObject != openingImage.gameObject)
        {
            openingCanvasGroup = openingImage.GetComponent<CanvasGroup>();
            if (openingCanvasGroup == null)
            {
                openingCanvasGroup = openingImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
        }

        openingRoutine = StartCoroutine(ShowOpeningRoutine());
    }

    private IEnumerator ShowOpeningRoutine()
    {
        openingImage.enabled = true;
        openingImage.raycastTarget = true;
        openingImage.transform.SetAsLastSibling();
        openingCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(openingHoldDuration);

        float elapsed = 0f;

        while (elapsed < openingFadeDuration)
        {
            elapsed += Time.deltaTime;
            openingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / Mathf.Max(0.01f, openingFadeDuration));
            yield return null;
        }

        openingCanvasGroup.alpha = 0f;
        openingImage.enabled = false;
        openingImage.raycastTarget = false;
        openingRoutine = null;
    }

    private void CreateConditionIconsIfNeeded(Transform parent)
    {
        if (impatientConditionImage == null)
        {
            impatientConditionImage = CreateConditionIcon(parent, "Impatient Condition Icon", "impatient_icon", 0);
        }

        if (relaxedConditionImage == null)
        {
            relaxedConditionImage = CreateConditionIcon(parent, "Relaxed Condition Icon", "relaxed_icon", 1);
        }

        if (sleepyConditionImage == null)
        {
            sleepyConditionImage = CreateConditionIcon(parent, "Sleepy Condition Icon", "sleepy_icon", 2);
        }

        UpdateConditionIcons();
    }

    private Image CreateConditionIcon(Transform parent, string objectName, string textureName, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>("ui/" + textureName);

        if (texture == null)
        {
            return null;
        }

        GameObject iconObject = new GameObject(objectName);
        iconObject.transform.SetParent(parent, false);

        Image image = iconObject.AddComponent<Image>();
        image.sprite = CreateSprite(texture);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1);
        rectTransform.anchorMax = new Vector2(0.5f, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(-64 + index * 64, -24);
        rectTransform.sizeDelta = new Vector2(52, 52);

        return image;
    }

    private void UpdateConditionIcons()
    {
        if (impatientConditionImage != null)
        {
            impatientConditionImage.enabled = hasImpatientCondition && !gameEnded;
        }

        if (relaxedConditionImage != null)
        {
            relaxedConditionImage.enabled = hasRelaxedCondition && !gameEnded;
        }

        if (sleepyConditionImage != null)
        {
            sleepyConditionImage.enabled = hasSleepyCondition && !gameEnded;
        }
    }

    private void ConfigureResultScreenImage(Image image, Texture2D texture)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        if (resultScreenAspectFitter == null || resultScreenAspectFitter.gameObject != image.gameObject)
        {
            resultScreenAspectFitter = image.GetComponent<AspectRatioFitter>();
            if (resultScreenAspectFitter == null)
            {
                resultScreenAspectFitter = image.gameObject.AddComponent<AspectRatioFitter>();
            }
        }

        resultScreenAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        if (texture != null && texture.height > 0)
        {
            resultScreenAspectFitter.aspectRatio = (float)texture.width / texture.height;
        }
    }

    private void ConfigureOpeningImage(Image image, Texture2D texture)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        if (openingAspectFitter == null || openingAspectFitter.gameObject != image.gameObject)
        {
            openingAspectFitter = image.GetComponent<AspectRatioFitter>();
            if (openingAspectFitter == null)
            {
                openingAspectFitter = image.gameObject.AddComponent<AspectRatioFitter>();
            }
        }

        openingAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        if (texture != null && texture.height > 0)
        {
            openingAspectFitter.aspectRatio = (float)texture.width / texture.height;
        }
    }

    private Slider CreateArtBar(Transform parent, string objectName, string labelTextureName, string backgroundTextureName, string fillTextureName, Vector2 anchoredPosition, bool isMoodBar)
    {
        Texture2D labelTexture = Resources.Load<Texture2D>("ui/" + labelTextureName);
        Texture2D backgroundTexture = Resources.Load<Texture2D>("ui/" + backgroundTextureName);
        Texture2D fillTexture = Resources.Load<Texture2D>("ui/" + fillTextureName);

        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 1);
        rootRect.anchorMax = new Vector2(0, 1);
        rootRect.pivot = new Vector2(0, 1);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = new Vector2(340, 56);

        if (labelTexture != null)
        {
            Image labelImage = CreateImage(root.transform, labelTextureName + " Image", labelTexture, new Vector2(0, 2), new Vector2(54, 48), true);
            labelImage.raycastTarget = false;
        }

        Image backgroundImage = CreateImage(root.transform, "Background", backgroundTexture, new Vector2(70, 0), new Vector2(250, 30), false);
        backgroundImage.raycastTarget = false;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 1);
        fillAreaRect.anchorMax = new Vector2(0, 1);
        fillAreaRect.pivot = new Vector2(0, 1);
        fillAreaRect.anchoredPosition = new Vector2(70, 0);
        fillAreaRect.sizeDelta = new Vector2(250, 30);

        Image fillImage = CreateImage(fillArea.transform, "Fill", fillTexture, Vector2.zero, new Vector2(250, 30), false);
        fillImage.raycastTarget = false;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0;

        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        if (isMoodBar)
        {
            moodFillImage = fillImage;
        }
        else
        {
            angerFillImage = fillImage;
        }

        Slider slider = root.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.fillRect = null;
        slider.targetGraphic = null;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private Image CreateImage(Transform parent, string objectName, Texture2D texture, Vector2 anchoredPosition, Vector2 size, bool preserveAspect)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = CreateSprite(texture);
        image.preserveAspect = preserveAspect;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        return image;
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private Slider CreateBar(Transform parent, string objectName, string label, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 1);
        rootRect.anchorMax = new Vector2(0, 1);
        rootRect.pivot = new Vector2(0, 1);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = new Vector2(320, 34);

        Text labelText = CreateText(root.transform, label + " Label", new Vector2(0, 0), label, 18);
        labelText.rectTransform.sizeDelta = new Vector2(78, 34);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.13f, 0.13f, 0.13f, 0.85f);

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0.5f);
        backgroundRect.anchorMax = new Vector2(1, 0.5f);
        backgroundRect.pivot = new Vector2(0, 0.5f);
        backgroundRect.anchoredPosition = new Vector2(86, 0);
        backgroundRect.sizeDelta = new Vector2(-86, 20);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(background.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3, 3);
        fillAreaRect.offsetMax = new Vector2(-3, -3);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider slider = root.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, string text, int fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = GetUIFont();
        uiText.fontSize = fontSize;
        uiText.color = uiTextColor;
        uiText.alignment = TextAnchor.MiddleLeft;

        RectTransform rectTransform = uiText.rectTransform;
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(360, 34);

        return uiText;
    }

    private Font GetUIFont()
    {
        if (uiFont == null)
        {
            uiFont = Resources.Load<Font>("Fonts/DynaPuff");
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return uiFont;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.18f, 0.18f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(PlayButtonClickSound);
        button.onClick.AddListener(action);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(0, 1);
        buttonRect.pivot = new Vector2(0, 1);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(104, 38);

        Text buttonText = CreateText(buttonObject.transform, label + " Text", Vector2.zero, label, 18);
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonText.rectTransform.anchoredPosition = Vector2.zero;
        buttonText.rectTransform.sizeDelta = Vector2.zero;

        return button;
    }

    private Button CreateArtButton(Transform parent, string objectName, string textureName, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        Texture2D buttonTexture = Resources.Load<Texture2D>("ui/" + textureName);

        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = CreateSprite(buttonTexture);
        buttonImage.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(PlayButtonClickSound);
        button.onClick.AddListener(action);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(0, 1);
        buttonRect.pivot = new Vector2(0, 1);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(104, 52);

        return button;
    }
}
