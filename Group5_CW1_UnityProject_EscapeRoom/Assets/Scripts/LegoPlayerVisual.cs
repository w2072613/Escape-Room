using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Lecturer-provided LegoWalk model and animation, shared by the two room players.
// The existing CharacterController owns movement; this child only handles visuals.
public sealed class LegoPlayerVisual : MonoBehaviour
{
    Transform player;
    Vector3 previousPosition;
    PlayableGraph graph;
    AnimationClipPlayable motion;
    float clipLength;
    public bool IsWalking { get; private set; }

    public static LegoPlayerVisual Attach(CharacterController controller, string student)
    {
        var root = new GameObject(student + " - LEGO player");
        root.transform.SetParent(controller.transform, false);
        root.transform.localPosition = controller.center - Vector3.up * (controller.height * .5f);
        var visual = root.AddComponent<LegoPlayerVisual>();
        visual.Initialize(controller.transform);
        return visual;
    }

    void Initialize(Transform owner)
    {
        player = owner;
        previousPosition = player.position;
        var prefab = Resources.Load<GameObject>("LegoWalk");
        var clip = Resources.LoadAll<AnimationClip>("LegoWalk")
            .FirstOrDefault(asset => !asset.name.StartsWith("__preview__"));
        if (prefab == null || clip == null)
        {
            Debug.LogError("Room player requires the LegoWalk model and animation.", this);
            return;
        }

        // Keep placement outside the imported animation's transform hierarchy.
        var placement = new GameObject("Model placement").transform;
        placement.SetParent(transform, false);
        var figure = Instantiate(prefab, placement, false);
        figure.name = "Animated LEGO model";
        foreach (var collider in figure.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var child in GetComponentsInChildren<Transform>()) child.gameObject.layer = 2;
        var renderers = figure.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y > .001f) placement.localScale *= 1.8f / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            placement.position += transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        var animator = figure.GetComponent<Animator>();
        if (animator == null) animator = figure.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create(name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        motion = AnimationClipPlayable.Create(graph, clip);
        clipLength = clip.length;
        AnimationPlayableOutput.Create(graph, "Player animation", animator).SetSourcePlayable(motion);
        graph.Play();
        graph.Evaluate(0);
    }

    void LateUpdate()
    {
        if (player == null || !graph.IsValid()) return;
        var displacement = player.position - previousPosition;
        previousPosition = player.position;
        displacement.y = 0;
        IsWalking = displacement.sqrMagnitude > .000001f;
        if (IsWalking)
        {
            transform.rotation = Quaternion.LookRotation(displacement, Vector3.up);
            motion.SetTime(clipLength > 0 ? (motion.GetTime() + Time.deltaTime) % clipLength : 0);
        }
        else motion.SetTime(0);
        // Evaluate explicitly so idle and pause cannot leave a stale walking pose.
        graph.Evaluate(0);
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
