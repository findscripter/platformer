using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerGuide 根视图 - 持有所有子视图引用
/// 替代 PlayerGuideFlowControllerV2 的 Inspector Fields 部分
/// </summary>
public class PlayerGuideView : MonoBehaviour
{
    #region Dialogue View References
    [Header("Dialogue View")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueSpeakerText;
    [SerializeField] private TMP_Text dialogueContentText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private Frame6DialogueConfig frame6Config;
    #endregion

    #region Dream Input View References
    [Header("Dream Input View")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField dreamInputField;
    [SerializeField] private TMP_Text inputPlaceholderText;
    [SerializeField] private Button inputSubmitButton;
    [SerializeField] private CanvasGroup inputCanvasGroup;
    #endregion

    #region Tarot View References
    [Header("Tarot View")]
    [SerializeField] private GameObject tarotPanel;
    [SerializeField] private TarotDrawController tarotController;
    #endregion

    #region Feifei Character View References
    [Header("Feifei Character View")]
    [SerializeField] private GameObject feifeiCharacter;
    [SerializeField] private Animator feifeiAnimator;
    #endregion

    #region Dream Core View References
    [Header("Dream Core View")]
    [SerializeField] private GameObject dreamCoreObject;
    [SerializeField] private Image dreamCoreGlow;
    #endregion

    #region Small Bubbles View References
    [Header("Small Bubbles View")]
    [SerializeField] private Transform smallBubblesContainer;
    [SerializeField] private GameObject smallBubblePrefab;
    [SerializeField] private int smallBubbleCount = 6;
    [SerializeField] private Vector2 smallBubbleSizeRange = new Vector2(94f, 128f);
    [SerializeField] private float smallBubbleFloatSpeed = 20f;
    [SerializeField] private float smallBubbleFloatAmplitude = 15f;
    #endregion

    #region Main Bubble View References
    [Header("Main Bubble View")]
    [SerializeField] private PlayerGuideMainBubbleVisual mainBubbleVisual;
    [SerializeField] private Button mainBubbleButton;
    [SerializeField] private AudioSource dreamEchoAudioSource;
    [SerializeField] private float frame2HintDelayFirst = 3f;
    [SerializeField] private float frame2HintDelayRepeat = 5f;
    [SerializeField] private float frame2HintGlowIntensity = 1.3f;
    #endregion

    #region Background View References
    [Header("Background View")]
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private ParticleSystem dreamSpaceParticles;
    #endregion

    #region Duration Configuration
    [Header("Duration Configuration")]
    [SerializeField] private float frame1Duration = 3.5f;
    [SerializeField] private float frame3Duration = 2.5f;
    [SerializeField] private float frame4DissolveDuration = 2f;
    [SerializeField] private float frame4CoreStabilizeDuration = 0.5f;
    [SerializeField] private float frame5WalkDuration = 3f;
    [SerializeField] private float frame5PickupDuration = 1.5f;
    [SerializeField] private float coreResponseDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    #endregion

    #region Public Properties - Dialogue View

    public GameObject DialoguePanel => dialoguePanel;
    public TMP_Text DialogueSpeakerText => dialogueSpeakerText;
    public TMP_Text DialogueContentText => dialogueContentText;
    public CanvasGroup DialogueCanvasGroup => dialogueCanvasGroup;
    public Frame6DialogueConfig Frame6Config => frame6Config;

    #endregion

    #region Public Properties - Dream Input View

    public GameObject InputPanel => inputPanel;
    public TMP_InputField DreamInputField => dreamInputField;
    public TMP_Text InputPlaceholderText => inputPlaceholderText;
    public Button InputSubmitButton => inputSubmitButton;
    public CanvasGroup InputCanvasGroup => inputCanvasGroup;

    #endregion

    #region Public Properties - Tarot View

    public GameObject TarotPanel => tarotPanel;
    public TarotDrawController TarotController => tarotController;

    #endregion

    #region Public Properties - Feifei Character View

    public GameObject FeifeiCharacter => feifeiCharacter;
    public Animator FeifeiAnimator => feifeiAnimator;

    #endregion

    #region Public Properties - Dream Core View

    public GameObject DreamCoreObject => dreamCoreObject;
    public Image DreamCoreGlow => dreamCoreGlow;

    #endregion

    #region Public Properties - Small Bubbles View

    public Transform SmallBubblesContainer => smallBubblesContainer;
    public GameObject SmallBubblePrefab => smallBubblePrefab;
    public int SmallBubbleCount => smallBubbleCount;
    public Vector2 SmallBubbleSizeRange => smallBubbleSizeRange;
    public float SmallBubbleFloatSpeed => smallBubbleFloatSpeed;
    public float SmallBubbleFloatAmplitude => smallBubbleFloatAmplitude;

    #endregion

    #region Public Properties - Main Bubble View

    public PlayerGuideMainBubbleVisual MainBubbleVisual => mainBubbleVisual;
    public Button MainBubbleButton => mainBubbleButton;
    public AudioSource DreamEchoAudioSource => dreamEchoAudioSource;
    public float Frame2HintDelayFirst => frame2HintDelayFirst;
    public float Frame2HintDelayRepeat => frame2HintDelayRepeat;
    public float Frame2HintGlowIntensity => frame2HintGlowIntensity;

    #endregion

    #region Public Properties - Background View

    public CanvasGroup BackgroundCanvasGroup => backgroundCanvasGroup;
    public ParticleSystem DreamSpaceParticles => dreamSpaceParticles;

    #endregion

    #region Public Properties - Duration Configuration

    public float Frame1Duration => frame1Duration;
    public float Frame3Duration => frame3Duration;
    public float Frame4DissolveDuration => frame4DissolveDuration;
    public float Frame4CoreStabilizeDuration => frame4CoreStabilizeDuration;
    public float Frame5WalkDuration => frame5WalkDuration;
    public float Frame5PickupDuration => frame5PickupDuration;
    public float CoreResponseDuration => coreResponseDuration;
    public float FadeOutDuration => fadeOutDuration;

    #endregion
}
