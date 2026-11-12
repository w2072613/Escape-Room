// Level 4 - The Missing Books (simple first-person version)
// Drag this file into Assets in a Unity 6 project and press Play. No setup needed.
//
// How to play: find 4 books, read the note, put the books on the shelf in the right order.
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public sealed class Level4Library : MonoBehaviour
{
    // true = starts in any scene. Set to false if you add this to a bigger project.
    const bool RunInAnyScene = true;
    const string SceneName = "Level4_Library";

    // The four books. The same number is used for the name, the colour and the symbol.
    static readonly string[] BookNames = { "Circle", "Square", "Diamond", "Plus" };
    static readonly Color[] BookColors =
    {
        new Color(.2f, .4f, .8f),  // Circle  - blue
        new Color(.2f, .6f, .3f),  // Square  - green
        new Color(.7f, .2f, .2f),  // Diamond - red
        new Color(.5f, .3f, .7f)   // Plus    - purple
    };
    // The right order on the shelf (left to right): Diamond, Circle, Plus, Square.
    static readonly int[] RightOrder = { 2, 0, 3, 1 };

    CharacterController player;
    Camera eyes;
    GameObject door, note, shelf;
    readonly GameObject[] books = new GameObject[4];
    readonly bool[] found = new bool[4];
    readonly int[] slots = { -1, -1, -1, -1 };   // which book is in each shelf slot (-1 = empty)
    bool noteOpen, shelfOpen, paused, solved, escaped;
    float yaw, pitch, fallSpeed;
    string message = "Find the 4 missing books.";
    float messageUntil = 6;
    GameObject target;
    string prompt = "";

    int FoundCount
    {
        get { int n = 0; foreach (bool f in found) if (f) n++; return n; }
    }

    // ---- Start ---------------------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if ((RunInAnyScene || SceneManager.GetActiveScene().name == SceneName)
            && Object.FindAnyObjectByType<Level4Library>() == null)
            new GameObject("Level 4 Library").AddComponent<Level4Library>();
    }

    void Awake()
    {
        Debug.Log("Level 4 started.");
        foreach (var cam in Camera.allCameras) Destroy(cam.gameObject);   // remove the default camera

        BuildRoom();

        // The player, with the camera at eye height.
        var body = new GameObject("Player");
        body.transform.position = new Vector3(0, .05f, 0);
        body.layer = 2;   // "Ignore Raycast", so the player does not block clicks
        player = body.AddComponent<CharacterController>();
        player.height = 1.8f;
        player.radius = .3f;
        player.center = new Vector3(0, .9f, 0);

        eyes = new GameObject("Camera").AddComponent<Camera>();
        eyes.gameObject.AddComponent<AudioListener>();
        eyes.transform.SetParent(body.transform, false);
        eyes.transform.localPosition = new Vector3(0, 1.6f, 0);
        eyes.fieldOfView = 70;
        eyes.clearFlags = CameraClearFlags.SolidColor;
        eyes.backgroundColor = Color.black;
    }

    // ---- The room ------------------------------------------------------------------------
    void BuildRoom()
    {
        RenderSettings.ambientLight = new Color(.6f, .6f, .6f);
        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Point;
        light.transform.position = new Vector3(0, 2.7f, 0);
        light.range = 15;
        light.intensity = 2;

        var wall = new Color(.75f, .7f, .6f);
        var wood = new Color(.45f, .28f, .15f);

        // Floor, ceiling and walls. The back wall has a gap for the door.
        Box("Floor", 0, -.1f, 1, 10, .2f, 14, new Color(.4f, .3f, .2f));
        Box("Ceiling", 0, 3.1f, 0, 10, .2f, 10, wall);
        Box("Left wall", -5, 1.5f, 0, .2f, 3, 10, wall);
        Box("Right wall", 5, 1.5f, 0, .2f, 3, 10, wall);
        Box("Front wall", 0, 1.5f, -5, 10, 3, .2f, wall);
        Box("Back wall left", -3, 1.5f, 5, 4, 3, .2f, wall);
        Box("Back wall right", 3, 1.5f, 5, 4, 3, .2f, wall);
        door = Box("Door", 0, 1.5f, 5, 2, 3, .2f, new Color(.3f, .15f, .08f));

        // Furniture: just a few big blocks.
        Box("Table", -2.5f, .4f, 0, 2, .8f, 1, wood);
        Box("Crate", 3, .3f, -1.5f, 1, .6f, 1, wood);
        Box("Stool", -3.5f, .25f, 3.5f, .6f, .5f, .6f, wood);
        Box("Desk", 0, .45f, -4, 2.4f, .9f, 1, wood);
        shelf = Box("Shelf", -4.4f, .75f, -3, .8f, 1.5f, 2.4f, new Color(.25f, .13f, .06f));
        note = Box("Note", 0, .91f, -4, .6f, .02f, .4f, Color.white);

        // The books: Diamond on the table, Circle on the crate, Plus on the stool, Square on the floor.
        books[2] = MakeBook(2, new Vector3(-2.5f, .84f, 0));
        books[0] = MakeBook(0, new Vector3(3, .64f, -1.5f));
        books[3] = MakeBook(3, new Vector3(-3.5f, .54f, 3.5f));
        books[1] = MakeBook(1, new Vector3(3.5f, .04f, 3));
    }

    // A book is one block with a dark stripe (the spine) and a gold symbol on it.
    GameObject MakeBook(int kind, Vector3 position)
    {
        var book = new GameObject("Book - " + BookNames[kind]);
        book.transform.position = position;
        book.AddComponent<BoxCollider>().size = new Vector3(.5f, .08f, .35f);

        var gold = new Color(1, .85f, .3f);
        Piece(book.transform, PrimitiveType.Cube, 0, 0, 0, .5f, .08f, .35f, BookColors[kind], 0);
        Piece(book.transform, PrimitiveType.Cube, 0, .041f, 0, .4f, .01f, .28f, new Color(.1f, .1f, .1f), 0);
        if (kind == 0) Piece(book.transform, PrimitiveType.Cylinder, 0, .05f, 0, .18f, .005f, .18f, gold, 0);
        if (kind == 1) Piece(book.transform, PrimitiveType.Cube, 0, .05f, 0, .16f, .01f, .16f, gold, 0);
        if (kind == 2) Piece(book.transform, PrimitiveType.Cube, 0, .05f, 0, .15f, .01f, .15f, gold, 45);
        if (kind == 3)
        {
            Piece(book.transform, PrimitiveType.Cube, 0, .05f, 0, .22f, .01f, .06f, gold, 0);
            Piece(book.transform, PrimitiveType.Cube, 0, .05f, 0, .06f, .01f, .22f, gold, 0);
        }
        return book;
    }

    // A decoration piece with no collider.
    static void Piece(Transform parent, PrimitiveType type, float x, float y, float z, float sx, float sy, float sz, Color color, float turn)
    {
        var p = GameObject.CreatePrimitive(type);
        p.GetComponent<Collider>().enabled = false;
        p.GetComponent<Renderer>().material.color = color;
        p.transform.SetParent(parent, false);
        p.transform.localPosition = new Vector3(x, y, z);
        p.transform.localRotation = Quaternion.Euler(0, turn, 0);
        p.transform.localScale = new Vector3(sx, sy, sz);
    }

    // A coloured block at a position (x, y, z) with a size (sx, sy, sz).
    static GameObject Box(string name, float x, float y, float z, float sx, float sy, float sz, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = new Vector3(x, y, z);
        box.transform.localScale = new Vector3(sx, sy, sz);
        box.GetComponent<Renderer>().material.color = color;
        return box;
    }

    // ---- Every frame ---------------------------------------------------------------------
    void Update()
    {
        if (L4Input.Pressed(L4Input.Btn.Escape))
        {
            if (noteOpen) noteOpen = false;
            else if (shelfOpen) shelfOpen = false;
            else if (!escaped) paused = !paused;
        }

        // Lock the mouse for looking around. Free it when a menu is open.
        bool menuOpen = noteOpen || shelfOpen || paused || escaped;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;
        if (menuOpen) { target = null; prompt = ""; return; }

        // Look around with the mouse.
        Vector2 look = L4Input.MouseDelta();
        yaw += look.x * 2f;
        pitch = Mathf.Clamp(pitch - look.y * 2f, -80f, 80f);
        player.transform.rotation = Quaternion.Euler(0, yaw, 0);
        eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // Walk with WASD.
        float side = (L4Input.Held(L4Input.Btn.D) ? 1 : 0) - (L4Input.Held(L4Input.Btn.A) ? 1 : 0);
        float forward = (L4Input.Held(L4Input.Btn.W) ? 1 : 0) - (L4Input.Held(L4Input.Btn.S) ? 1 : 0);
        Vector3 move = Vector3.ClampMagnitude(player.transform.right * side + player.transform.forward * forward, 1) * 3f;
        if (player.isGrounded && fallSpeed < 0) fallSpeed = -2;
        fallSpeed += Physics.gravity.y * Time.deltaTime;
        player.Move((move + Vector3.up * fallSpeed) * Time.deltaTime);

        // Look at what is in the middle of the screen.
        target = null;
        prompt = "";
        if (Physics.Raycast(eyes.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out var hit, 2.8f))
        {
            var obj = hit.collider.gameObject;
            for (int i = 0; i < 4; i++)
                if (obj == books[i]) { target = obj; prompt = "Press E to pick up the book"; }
            if (obj == note) { target = obj; prompt = "Press E to read the note"; }
            if (obj == shelf && !solved) { target = obj; prompt = "Press E to use the shelf"; }
        }
        if (target != null && (L4Input.Pressed(L4Input.Btn.E) || L4Input.ClickPressed())) Use(target);

        // Walking through the open door ends the level.
        if (solved && player.transform.position.z > 5.5f) escaped = true;
    }

    void Use(GameObject obj)
    {
        for (int i = 0; i < 4; i++)
        {
            if (obj != books[i]) continue;
            found[i] = true;
            books[i].SetActive(false);
            Say("You found the " + BookNames[i] + " book. (" + FoundCount + " of 4)");
            return;
        }
        if (obj == note) noteOpen = true;
        if (obj == shelf)
        {
            if (FoundCount == 0) Say("You have no books yet.");
            else shelfOpen = true;
        }
    }

    // ---- The shelf -----------------------------------------------------------------------
    bool IsOnShelf(int book)
    {
        foreach (int s in slots) if (s == book) return true;
        return false;
    }

    // Each click puts the next book you found into this slot. After the last one, the slot goes empty.
    void ChangeSlot(int slot)
    {
        int next = slots[slot] + 1;
        while (next < 4 && !(found[next] && !IsOnShelf(next))) next++;
        slots[slot] = next >= 4 ? -1 : next;
    }

    void CheckShelf()
    {
        foreach (int s in slots)
            if (s < 0) { Say("Fill all 4 slots first."); return; }

        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == RightOrder[i]) continue;
            for (int s = 0; s < 4; s++) slots[s] = -1;
            shelfOpen = false;
            Say("Wrong order. Try again.");
            return;
        }
        solved = true;
        shelfOpen = false;
        door.SetActive(false);
        Say("The door is open! Walk through it.");
    }

    // ---- Screen text and buttons ---------------------------------------------------------
    void Say(string text) { message = text; messageUntil = Time.time + 5; }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(15, 10, 700, 25), "WASD = move    Mouse = look    E = use    Esc = pause");
        GUI.Label(new Rect(15, 35, 700, 25), "Goal: find the 4 books, read the note, put the books on the shelf.");
        GUI.Label(new Rect(15, 60, 700, 25), "Books found: " + FoundCount + " of 4");
        if (Time.time < messageUntil) GUI.Box(new Rect(20, Screen.height - 70, Screen.width - 40, 40), message);

        bool menuOpen = noteOpen || shelfOpen || paused || escaped;
        if (!menuOpen)
        {
            GUI.DrawTexture(new Rect(Screen.width / 2f - 2, Screen.height / 2f - 2, 4, 4), Texture2D.whiteTexture);   // dot in the middle
            if (prompt != "") GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 30, 300, 30), prompt);
        }

        if (noteOpen)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 360) / 2f, (Screen.height - 260) / 2f, 360, 260), GUI.skin.box);
            GUILayout.Label("THE NOTE");
            GUILayout.Label("Put the books on the shelf in this order, from left to right:");
            GUILayout.Label("1. Diamond");
            GUILayout.Label("2. Circle");
            GUILayout.Label("3. Plus");
            GUILayout.Label("4. Square");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(30))) noteOpen = false;
            GUILayout.EndArea();
        }

        if (shelfOpen)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 360) / 2f, (Screen.height - 330) / 2f, 360, 330), GUI.skin.box);
            GUILayout.Label("THE SHELF");
            GUILayout.Label("Click a slot to change the book in it.");
            for (int s = 0; s < 4; s++)
            {
                string label = slots[s] < 0 ? "empty" : BookNames[slots[s]];
                if (GUILayout.Button("Slot " + (s + 1) + ": " + label, GUILayout.Height(36))) ChangeSlot(s);
            }
            if (GUILayout.Button("Check", GUILayout.Height(32))) CheckShelf();
            if (GUILayout.Button("Close", GUILayout.Height(28))) shelfOpen = false;
            GUILayout.EndArea();
        }

        if (paused || escaped)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 300) / 2f, (Screen.height - 160) / 2f, 300, 160), GUI.skin.box);
            GUILayout.Label(escaped ? "YOU ESCAPED!" : "PAUSED");
            if (!escaped && GUILayout.Button("Resume", GUILayout.Height(32))) paused = false;
            if (GUILayout.Button("Play again", GUILayout.Height(32)) && SceneManager.GetActiveScene().buildIndex >= 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            GUILayout.EndArea();
        }
    }
}

// Reads the keyboard and mouse. Works with both of Unity's input systems.
static class L4Input
{
    public enum Btn { W, A, S, D, E, Escape }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static UnityEngine.InputSystem.Controls.KeyControl Get(Btn b)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;
        switch (b)
        {
            case Btn.W: return kb.wKey;
            case Btn.A: return kb.aKey;
            case Btn.S: return kb.sKey;
            case Btn.D: return kb.dKey;
            case Btn.E: return kb.eKey;
            default: return kb.escapeKey;
        }
    }
    public static bool Held(Btn b) { var k = Get(b); return k != null && k.isPressed; }
    public static bool Pressed(Btn b) { var k = Get(b); return k != null && k.wasPressedThisFrame; }
    public static bool ClickPressed() { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
    public static Vector2 MouseDelta()
    {
        return Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * .1f;
    }
#else
    static KeyCode Map(Btn b)
    {
        switch (b)
        {
            case Btn.W: return KeyCode.W;
            case Btn.A: return KeyCode.A;
            case Btn.S: return KeyCode.S;
            case Btn.D: return KeyCode.D;
            case Btn.E: return KeyCode.E;
            default: return KeyCode.Escape;
        }
    }
    public static bool Held(Btn b) { return Input.GetKey(Map(b)); }
    public static bool Pressed(Btn b) { return Input.GetKeyDown(Map(b)); }
    public static bool ClickPressed() { return Input.GetMouseButtonDown(0); }
    public static Vector2 MouseDelta() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
#endif
}
