using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerGuide 根视图 - 持有所有子视图引用。
/// 替代 PlayerGuideFlowControllerV2 的 Inspector Fields 部分。
/// </summary>
public class PlayerGuideView : MonoBehaviour
{
    [Header("Frame 1: 梦境空间生成")]
    [SerializeField] private Transform smallBubblesContainer;
    [SerializeField] private GameObject smallBubblePrefab;
    [SerializeField] private int smallBubbleCount = 6;
    [SerializeField] private Vector2 smallBubbleSizeRange = new Vector2(94f, 128f);
    [SerializeField] private float smallBubbleFloatSpeed = 20f;
    [SerializeField] private float smallBubbleFloatAmplitude = 15f;

    [Header("Frame 2: 弱引导")]
    [SerializeField] private float frame2HintDelayFirst = 3f;
    [SerializeField] private float frame2HintDelayRepeat = 5f;
    [SerializeField] private float frame2HintGlowIntensity = 1.3f;
    [SerializeField] private ParticleSystem dreamSpaceParticles;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private Sprite dreamBackgroundSpriteEarly;
    [SerializeField] private Sprite dreamBackgroundSpriteLate;
    [SerializeField] private PlayerGuideMainBubbleVisual mainBubbleVisual;
    [SerializeField] private Button mainBubbleButton;
    [SerializeField] private AudioSource dreamEchoAudioSource;

    [Header("Frame 4-5: 梦核与腓腓")]
    [SerializeField] private GameObject dreamCoreObject;
    [SerializeField] private Image dreamCoreGlow;
    [SerializeField] private GameObject feifeiCharacter;
    [SerializeField] private Animator feifeiAnimator;

    [Header("Frame 6: 对话系统")]
    [SerializeField] private Frame6DialogueConfig frame6Config;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueSpeakerText;
    [SerializeField] private TMP_Text dialogueContentText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Frame 7-8: 输入系统")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField dreamInputField;
    [SerializeField] private TMP_Text inputPlaceholderText;
    [SerializeField] private Button inputSubmitButton;
    [SerializeField] private CanvasGroup inputCanvasGroup;

    [Header("Frame 9: 塔罗系统")]
    [SerializeField] private GameObject tarotPanel;
    [SerializeField] private TarotDrawController tarotController;

    [Header("Frame 10: 结束")]
    [SerializeField] private float coreResponseDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("时长配置")]
    [SerializeField] private float frame1Duration = 2.5f;
    [SerializeField] private float frame3Duration = 2.5f;
    [SerializeField] private float frame4DissolveDuration = 2f;
    [SerializeField] private float frame4CoreStabilizeDuration = 0.5f;
    [SerializeField] private float frame5WalkDuration = 1.5f;
    [SerializeField] private float frame5PickupDuration = 1f;
    [SerializeField] private float frame5TailDuration = 1f;

    public Transform SmallBubblesContainer => smallBubblesContainer;
    public GameObject SmallBubblePrefab => smallBubblePrefab;
    public int SmallBubbleCount => smallBubbleCount;
    public Vector2 SmallBubbleSizeRange => smallBubbleSizeRange;
    public float SmallBubbleFloatSpeed => smallBubbleFloatSpeed;
    public float SmallBubbleFloatAmplitude => smallBubbleFloatAmplitude;

    public float Frame2HintDelayFirst => frame2HintDelayFirst;
    public float Frame2HintDelayRepeat => frame2HintDelayRepeat;
    public float Frame2HintGlowIntensity => frame2HintGlowIntensity;
    public ParticleSystem DreamSpaceParticles => dreamSpaceParticles;
    public CanvasGroup BackgroundCanvasGroup => backgroundCanvasGroup;
    public Sprite DreamBackgroundSpriteEarly => dreamBackgroundSpriteEarly;
    public Sprite DreamBackgroundSpriteLate => dreamBackgroundSpriteLate;
    public PlayerGuideMainBubbleVisual MainBubbleVisual => mainBubbleVisual;
    public Button MainBubbleButton => mainBubbleButton;
    public AudioSource DreamEchoAudioSource => dreamEchoAudioSource;

    public GameObject DreamCoreObject => dreamCoreObject;
    public Image DreamCoreGlow => dreamCoreGlow;
    public GameObject FeifeiCharacter => feifeiCharacter;
    public Animator FeifeiAnimator => feifeiAnimator;

    public Frame6DialogueConfig Frame6Config => frame6Config;
    public GameObject DialoguePanel => dialoguePanel;
    public TMP_Text DialogueSpeakerText => dialogueSpeakerText;
    public TMP_Text DialogueContentText => dialogueContentText;
    public CanvasGroup DialogueCanvasGroup => dialogueCanvasGroup;

    public GameObject InputPanel => inputPanel;
    public TMP_InputField DreamInputField => dreamInputField;
    public TMP_Text InputPlaceholderText => inputPlaceholderText;
    public Button InputSubmitButton => inputSubmitButton;
    public CanvasGroup InputCanvasGroup => inputCanvasGroup;

    public GameObject TarotPanel => tarotPanel;
    public TarotDrawController TarotController => tarotController;

    public float CoreResponseDuration => coreResponseDuration;
    public float FadeOutDuration => fadeOutDuration;

    public float Frame1Duration => frame1Duration > 0f ? frame1Duration : 2.5f;
    public float Frame3Duration => frame3Duration > 0f ? frame3Duration : 2.5f;
    public float Frame4DissolveDuration => frame4DissolveDuration > 0f ? frame4DissolveDuration : 2f;
    public float Frame4CoreStabilizeDuration => frame4CoreStabilizeDuration > 0f ? frame4CoreStabilizeDuration : 0.5f;
    public float Frame5WalkDuration => frame5WalkDuration > 0f ? frame5WalkDuration : 1.5f;
    public float Frame5PickupDuration => frame5PickupDuration > 0f ? frame5PickupDuration : 1f;
    public float Frame5TailDuration => frame5TailDuration > 0f ? frame5TailDuration : 1f;
}
