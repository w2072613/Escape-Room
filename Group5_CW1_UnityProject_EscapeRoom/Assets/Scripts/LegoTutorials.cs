using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tutorial references: 02–03 (brick controls) and 05.2 (animation buttons).
// Yaseen controls the Rigidbody brick; Zain controls the draggable brick.
public sealed class LegoTutorials : MonoBehaviour
{
    private Rigidbody physicsBrick;
    private Transform mouseBrick;
    private Camera overhead;
    private Vector3 dragOffset;
    private bool dragging;
    private PlayableGraph animationGraph;
    private Vector3 movement;
    private readonly List<AnimationClipPlayable> motions = new List<AnimationClipPlayable>();
    private float clipLength;
    private Camera roomCamera;
    private bool inEscapeRoom;
    private bool active = true;
    public static LegoTutorials Workshop { get; private set; }
    public static bool WorkshopActive => Workshop != null && Workshop.active;
    public bool PowerRestored { get; private set; }

    public static void CreateWorkshop(Camera camera)
    {
        var demo = new GameObject("LEGO power workshop").AddComponent<LegoTutorials>();
        demo.inEscapeRoom = true;
        demo.roomCamera = camera;
        Workshop = demo;
    }

    public void ShowWorkshop(bool show)
    {
        active = show;
        dragging = false;
        movement = Vector3.zero;
        physicsBrick.linearVelocity = Vector3.zero;
        foreach (Transform child in transform) child.gameObject.SetActive(show);
        if (roomCamera != null) roomCamera.enabled = !show;
    }

    private GameObject Own(GameObject obj)
    {
        obj.transform.SetParent(transform, true);
        return obj;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (SceneManager.GetActiveScene().name == "LegoTutorials")
            new GameObject("LEGO Tutorial Demos").AddComponent<LegoTutorials>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        overhead = Own(new GameObject("Workshop Camera")).AddComponent<Camera>();
        overhead.transform.SetPositionAndRotation(new Vector3(0, 12, -10), Quaternion.LookRotation(new Vector3(0, -12, 10)));
        overhead.orthographic = true;
        overhead.orthographicSize = Mathf.Max(6, 9 / overhead.aspect);

        RenderSettings.ambientLight = Color.white * .7f;
        var light = Own(new GameObject("Workshop Light")).AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        Box("Floor", new Vector3(0, -.15f, 0), new Vector3(16, .3f, 10), Color.gray);

        physicsBrick = Brick("Yaseen - Rigidbody brick", new Vector3(-4, .45f, 0), Color.red).AddComponent<Rigidbody>();
        physicsBrick.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        physicsBrick.linearDamping = 4;
        mouseBrick = Brick("Zain - mouse brick", new Vector3(4, .45f, 0), Color.blue).transform;

        var canvas = Own(new GameObject("Demo Controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            Own(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
        animationGraph = PlayableGraph.Create("LEGO animation controls");

        var model = Resources.Load<GameObject>("LegoWalk");
        var clip = Resources.LoadAll<AnimationClip>("LegoWalk").FirstOrDefault(asset => !asset.name.StartsWith("__preview__"));
        if (model == null || clip == null)
        {
            Debug.LogError("LegoWalk must be imported as a model with an animation clip.");
            return;
        }

        clipLength = clip.length;
        AddFigure(model, clip, "Yaseen", -4, canvas.transform, 25);
        AddFigure(model, clip, "Zain", 4, canvas.transform, 545);
        animationGraph.Play();
        Box("Red power pad", new Vector3(-4, .02f, -2), new Vector3(2, .04f, 1.4f), new Color(.5f, .12f, .12f));
        Box("Blue power pad", new Vector3(4, .02f, -2), new Vector3(2, .04f, 1.4f), new Color(.12f, .2f, .5f));
        // Physical boundaries keep the Rigidbody on its half of the workbench.
        Box("Left boundary", new Vector3(-7.8f, .5f, 0), new Vector3(.2f, 1, 10), Color.gray);
        Box("Divider", new Vector3(0, .5f, 0), new Vector3(.2f, 1, 10), Color.gray);
        Box("Front boundary", new Vector3(-4, .5f, -4.8f), new Vector3(8, 1, .2f), Color.gray);
        Box("Back boundary", new Vector3(-4, .5f, 4.8f), new Vector3(8, 1, .2f), Color.gray);
        if (inEscapeRoom)
        {
            transform.position = Vector3.right * 24;
            ShowWorkshop(false);
        }
    }

    private void AddFigure(GameObject model, AnimationClip clip, string student, float x, Transform canvas, float left)
    {
        var anchor = Own(new GameObject(student + " - minifigure"));
        anchor.transform.SetPositionAndRotation(new Vector3(x, 0, 2.5f), Quaternion.Euler(0, 180, 0));
        var figure = Instantiate(model, anchor.transform, false);
        var renderers = figure.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y > .001f) anchor.transform.localScale *= 1.8f / bounds.size.y;
        }
        figure.name = "Animated LEGO model";
        var animator = figure.GetComponent<Animator>();
        // Unity components use a custom null check; ?? can retain a missing component.
        if (animator == null) animator = figure.AddComponent<Animator>();
        animator.applyRootMotion = false;
        var motion = AnimationClipPlayable.Create(animationGraph, clip);
        motion.SetSpeed(0);
        motions.Add(motion);
        var output = AnimationPlayableOutput.Create(animationGraph, student, animator);
        output.SetSourcePlayable(motion);
        AddButton(canvas, student + " Stand", left, () =>
        {
            motion.SetTime(0);
            motion.SetSpeed(0);
        });
        AddButton(canvas, student + " Walk", left + 125, () =>
        {
            motion.SetTime(0);
            motion.SetSpeed(1);
        });
    }

    private static void AddButton(Transform canvas, string label, float x, UnityEngine.Events.UnityAction click)
    {
        var button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(canvas, false);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -35);
        rect.sizeDelta = new Vector2(115, 35);
        button.targetGraphic = button.GetComponent<Image>();
        button.onClick.AddListener(click);
        var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(rect, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = label;
    }

    private void Update()
    {
        if (!active) return;
        foreach (var motion in motions)
            if (clipLength > 0 && motion.GetTime() >= clipLength)
                motion.SetTime(motion.GetTime() % clipLength);
        var red = physicsBrick.transform.localPosition;
        var blue = mouseBrick.localPosition;
        if (Vector2.Distance(new Vector2(red.x, red.z), new Vector2(-4, -2)) < .55f &&
            Vector2.Distance(new Vector2(blue.x, blue.z), new Vector2(4, -2)) < .55f)
            PowerRestored = true;
        movement = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1);
        HandlePointer(Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Input.mousePosition,
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
    }

    private void HandlePointer(bool pressed, bool held, Vector3 screenPosition, bool overUI)
    {
        var ray = overhead.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.up, mouseBrick.position);
        if (pressed && !overUI && Physics.Raycast(ray, out var hit) &&
            hit.transform == mouseBrick && plane.Raycast(ray, out var start))
        {
            dragging = true;
            dragOffset = mouseBrick.position - ray.GetPoint(start);
        }
        if (!held) dragging = false;
        if (dragging && plane.Raycast(ray, out var distance))
        {
            var position = ray.GetPoint(distance) + dragOffset;
            mouseBrick.position = new Vector3(
                Mathf.Clamp(position.x, transform.position.x + .9f, transform.position.x + 7),
                mouseBrick.position.y,
                Mathf.Clamp(position.z, transform.position.z - 4, transform.position.z + 2));
        }
    }

    private void FixedUpdate()
    {
        if (active && physicsBrick != null)
            physicsBrick.AddForce(movement * 18, ForceMode.Acceleration);
    }

    private void OnGUI()
    {
        if (!active) return;
        GUI.Box(new Rect(15, 8, Screen.width - 30, 25), PowerRestored
            ? "WORKSHOP POWER RESTORED — the escape-room keypad now has power."
            : "LEGO POWER WORKSHOP — move each brick onto its matching dark pad.");
        if (inEscapeRoom && GUI.Button(new Rect(Screen.width / 2f - 110, Screen.height - 48, 220, 32), "Return to escape room"))
            ShowWorkshop(false);
        GUI.Box(new Rect(15, Screen.height - 60, 270, 38), "Yaseen: WASD moves the red brick");
        GUI.Box(new Rect(Screen.width - 285, Screen.height - 60, 270, 38), "Zain: click and drag the blue brick");
    }

    private void OnDestroy()
    {
        if (animationGraph.IsValid()) animationGraph.Destroy();
        if (Workshop == this) Workshop = null;
    }

    private GameObject Brick(string label, Vector3 position, Color color)
    {
        var body = Box(label, position, new Vector3(1.5f, .65f, .8f), color);
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 4; column++)
        {
            var stud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stud.name = "Stud";
            stud.GetComponent<Collider>().enabled = false;
            stud.GetComponent<Renderer>().material.color = color;
            stud.transform.position = position + new Vector3((column - 1.5f) * .36f, .38f, (row - .5f) * .38f);
            stud.transform.localScale = new Vector3(.24f, .05f, .24f);
            stud.transform.SetParent(body.transform, true);
        }
        return body;
    }

    private GameObject Box(string label, Vector3 position, Vector3 size, Color color)
    {
        var box = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        box.name = label;
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material.color = color;
        return box;
    }
}
