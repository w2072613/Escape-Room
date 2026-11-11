using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// AI-assisted implementation using the course Tutorials 02–03 and 05.2 as references.
public sealed class LegoTutorials : MonoBehaviour
{
    private Rigidbody physicsBrick;
    private Transform mouseBrick;
    private Camera overhead;
    private Vector3 dragOffset;
    private bool dragging;
    private PlayableGraph animationGraph;
    private Vector3 movement;

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
        overhead = new GameObject("Overhead Camera").AddComponent<Camera>();
        overhead.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
        overhead.orthographic = true;
        overhead.orthographicSize = Mathf.Max(5, 8 / overhead.aspect);

        RenderSettings.ambientLight = Color.white * .7f;
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        Box("Floor", new Vector3(0, -.15f, 0), new Vector3(16, .3f, 10), Color.gray);

        physicsBrick = Brick("Yaseen - Rigidbody brick", new Vector3(-4, .45f, 0), Color.red).AddComponent<Rigidbody>();
        physicsBrick.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        physicsBrick.linearDamping = 4;
        mouseBrick = Brick("Zain - mouse brick", new Vector3(4, .45f, 0), Color.blue).transform;

        var canvas = new GameObject("Demo Controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        animationGraph = PlayableGraph.Create("LEGO animation controls");

        var model = Resources.Load<GameObject>("LegoWalk");
        var clip = Resources.LoadAll<AnimationClip>("LegoWalk").FirstOrDefault(asset => !asset.name.StartsWith("__preview__"));
        if (model == null || clip == null)
        {
            Debug.LogError("LegoWalk must be imported as a model with an animation clip.");
            return;
        }

        AddFigure(model, clip, "Yaseen", -4, canvas.transform, 25);
        AddFigure(model, clip, "Zain", 4, canvas.transform, 545);
        animationGraph.Play();
    }

    private void AddFigure(GameObject model, AnimationClip clip, string student, float x, Transform canvas, float left)
    {
        var figure = Instantiate(model, new Vector3(x, 0, 3), Quaternion.identity);
        figure.name = student + " - minifigure";
        var animator = figure.GetComponent<Animator>() ?? figure.AddComponent<Animator>();
        animator.applyRootMotion = false;
        var motion = AnimationClipPlayable.Create(animationGraph, clip);
        motion.SetSpeed(0);
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
        movement = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1);
        var ray = overhead.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, mouseBrick.position);
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
            Physics.Raycast(ray, out var hit) && hit.transform == mouseBrick && plane.Raycast(ray, out var start))
        {
            dragging = true;
            dragOffset = mouseBrick.position - ray.GetPoint(start);
        }

        if (!Input.GetMouseButton(0)) dragging = false;
        if (dragging && plane.Raycast(ray, out var distance))
        {
            var position = ray.GetPoint(distance) + dragOffset;
            mouseBrick.position = new Vector3(Mathf.Clamp(position.x, .9f, 7), mouseBrick.position.y, Mathf.Clamp(position.z, -4, 2));
        }
    }

    private void FixedUpdate()
    {
        if (physicsBrick != null)
            physicsBrick.AddForce(movement * 18, ForceMode.Acceleration);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(15, Screen.height - 60, 270, 38), "Yaseen: WASD moves the red brick");
        GUI.Box(new Rect(Screen.width - 285, Screen.height - 60, 270, 38), "Zain: click and drag the blue brick");
    }

    private void OnDestroy()
    {
        if (animationGraph.IsValid()) animationGraph.Destroy();
    }

    private static GameObject Brick(string label, Vector3 position, Color color)
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

    private static GameObject Box(string label, Vector3 position, Vector3 size, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = label;
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material.color = color;
        return box;
    }
}
