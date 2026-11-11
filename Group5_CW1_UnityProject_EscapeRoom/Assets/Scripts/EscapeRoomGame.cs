// Fixed overhead camera and matching movement controls.
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EscapeRoomGame : MonoBehaviour
{
    CharacterController player;
    Camera eyes;
    Transform doorHinge;
    GameObject rug, keyObject, drawerObject, note, keypad, door;
    Material rugMaterial;
    bool rugMoved, hasKey, drawerOpen, unlocked, escaped, keypadOpen, paused;
    float verticalSpeed, doorAngle;
    string code = "", message = "Search the room. Something might be hidden under the rug.";
    float messageUntil = 8;
    GameObject target;
    string prompt;
    readonly Vector3 spawn = new Vector3(0, .05f, -3);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterScenes()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "EscapeRoom" && Object.FindFirstObjectByType<EscapeRoomGame>() == null)
            new GameObject("Escape Room Game").AddComponent<EscapeRoomGame>();
    }

    void Awake()
    {
        BuildRoom();
        var body = new GameObject("Player");
        body.transform.position = spawn;
        player = body.AddComponent<CharacterController>();
        player.height = 1.8f;
        player.radius = .3f;
        player.center = new Vector3(0, .9f, 0);
        player.stepOffset = .25f;
        var marker = Box("Player marker", Vector3.zero, new Vector3(.45f, .25f, .45f), Color.cyan);
        marker.GetComponent<Collider>().enabled = false;
        marker.layer = 2; // Ignore Raycast
        marker.transform.SetParent(body.transform, false);
        marker.transform.localPosition = Vector3.up * .3f;
        body.layer = 2;

        eyes = new GameObject("Overhead Camera").AddComponent<Camera>();
        eyes.transform.SetPositionAndRotation(new Vector3(0, 8, 1), Quaternion.Euler(90, 0, 0));
        eyes.orthographic = true;
        eyes.orthographicSize = Mathf.Max(6.5f, 4.4f / eyes.aspect);
        eyes.nearClipPlane = .05f;
        eyes.backgroundColor = new Color(.07f, .08f, .1f);
        eyes.clearFlags = CameraClearFlags.SolidColor;
        LegoTutorials.CreateWorkshop(eyes);
        ShowCursor();
    }

    void BuildRoom()
    {
        RenderSettings.ambientLight = new Color(.55f, .58f, .65f);
        var light = new GameObject("Warm ceiling light").AddComponent<Light>();
        light.type = LightType.Point;
        light.transform.position = new Vector3(0, 2.9f, 0);
        light.range = 15;
        light.intensity = 2.5f;
        light.color = new Color(1, .86f, .67f);
        Box("Floor", new Vector3(0, -.12f, 1), new Vector3(8, .24f, 12), new Color(.32f, .24f, .17f));
        var wallColor = new Color(.3f, .36f, .39f);
        Box("Left wall", new Vector3(-4, 1.7f, 0), new Vector3(.2f, 3.4f, 10), wallColor);
        Box("Right wall", new Vector3(4, 1.7f, 0), new Vector3(.2f, 3.4f, 10), wallColor);
        Box("Rear wall", new Vector3(0, 1.7f, -5), new Vector3(8, 3.4f, .2f), wallColor);
        Box("Ceiling", new Vector3(0, 3.5f, 0), new Vector3(8, .2f, 10), wallColor).SetActive(false);
        Box("Exit wall left", new Vector3(-2.5f, 1.7f, 5), new Vector3(3, 3.4f, .2f), wallColor);
        Box("Exit wall right", new Vector3(2.5f, 1.7f, 5), new Vector3(3, 3.4f, .2f), wallColor);
        Box("Door lintel", new Vector3(0, 3.1f, 5), new Vector3(2, .6f, .2f), wallColor).SetActive(false);
        Box("Corridor left", new Vector3(-1.1f, 1.5f, 6), new Vector3(.2f, 3, 2), wallColor);
        Box("Corridor right", new Vector3(1.1f, 1.5f, 6), new Vector3(.2f, 3, 2), wallColor);
        Box("Corridor end", new Vector3(0, 1.5f, 7), new Vector3(2, 3, .2f), wallColor);
        doorHinge = new GameObject("Door hinge").transform;
        doorHinge.position = new Vector3(-.95f, 0, 5);
        door = Box("Exit door", new Vector3(0, 1.35f, 5), new Vector3(1.9f, 2.7f, .14f), new Color(.27f, .13f, .07f));
        door.transform.SetParent(doorHinge, true);
        keypad = Box("Door keypad", new Vector3(1.3f, 1.4f, 4.82f), new Vector3(.4f, .55f, .18f), new Color(.08f, .16f, .2f));
        Box("Keypad screen", new Vector3(1.3f, 1.52f, 4.71f), new Vector3(.28f, .13f, .03f), Color.green);
        rug = Box("Rug", new Vector3(-1.8f, .025f, -.8f), new Vector3(2, .04f, 1.5f), new Color(.55f, .09f, .12f));
        rugMaterial = rug.GetComponent<Renderer>().material;
        keyObject = Box("Brass key", new Vector3(-1.8f, .018f, -.8f), new Vector3(.3f, .025f, .08f), new Color(1, .75f, .1f));
        keyObject.SetActive(false);
        var wood = new Color(.4f, .22f, .1f);
        Box("Desk top", new Vector3(-2.7f, 1.05f, 2.7f), new Vector3(1.8f, .15f, 1), wood);
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Box("Desk leg", new Vector3(-2.7f + x * .7f, .5f, 2.7f + z * .35f), new Vector3(.12f, 1, .12f), wood);
        drawerObject = Box("Locked drawer", new Vector3(-2.7f, .82f, 2.65f), new Vector3(1.5f, .3f, .85f), new Color(.3f, .16f, .07f));
        note = Box("Code note", new Vector3(-2.7f, .99f, 1.85f), new Vector3(.5f, .015f, .35f), Color.white);
        note.SetActive(false);
    }

    static GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().material.color = color;
        return box;
    }

    void Update()
    {
        if (LegoTutorials.WorkshopActive) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keypadOpen) keypadOpen = false;
            else paused = !paused;
            ShowCursor();
        }
        doorAngle = Mathf.MoveTowards(doorAngle, unlocked ? -100 : 0, Time.deltaTime * 85);
        doorHinge.localRotation = Quaternion.Euler(0, doorAngle, 0);
        if (escaped || paused || keypadOpen) { target = null; prompt = ""; return; }
        float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        float z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        var move = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1) * 2.7f;
        if (player.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        player.Move((move + Vector3.up * verticalSpeed) * Time.deltaTime);
        FindTarget();
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) && target != null) Interact(target);
        if (unlocked && player.transform.position.z > 6.25f)
        {
            escaped = true;
            ShowCursor();
        }
    }

    void FindTarget()
    {
        target = null;
        prompt = "";
        if (!Physics.Raycast(eyes.ScreenPointToRay(Input.mousePosition), out var hit)) return;
        Vector3 distance = hit.point - player.transform.position;
        distance.y = 0;
        if (distance.sqrMagnitude > 2.5f * 2.5f) return;

        var obj = hit.collider.gameObject;
        if (obj.name == "Desk top") obj = drawerObject;
        if (obj == rug) prompt = rugMoved ? "The rug has been moved" : "E — Lift the rug";
        else if (obj == keyObject) prompt = "E — Pick up the brass key";
        else if (obj == drawerObject) prompt = drawerOpen ? "E — Read the drawer note" : "E — Open the locked drawer";
        else if (obj == note) prompt = "E — Read the note";
        else if (obj == keypad || obj.name == "Keypad screen" || obj == door) prompt = unlocked ? "Door unlocked — walk through" : "E — Use door keypad";
        if (prompt != "") target = obj;
    }

    void Interact(GameObject obj)
    {
        if (obj == rug && !rugMoved)
        {
            rugMoved = true;
            rug.transform.position += Vector3.left * 1.25f;
            rugMaterial.color = new Color(.7f, .15f, .17f);
            keyObject.SetActive(true);
            Say("A brass key was hidden under the rug. Look at it and press E.");
        }
        else if (obj == keyObject)
        {
            hasKey = true;
            keyObject.SetActive(false);
            Say("Brass key collected. It might fit the desk drawer.");
        }
        else if (obj == drawerObject || obj == note)
        {
            if (!hasKey) { Say("The drawer is locked. Find a key."); return; }
            if (!drawerOpen)
            {
                drawerOpen = true;
                drawerObject.transform.position += Vector3.back * .7f;
                note.SetActive(true);
            }
            Say("The note reads: EXIT CODE 2148.", 12);
        }
        else if (!unlocked && (obj == keypad || obj == door || obj.name == "Keypad screen"))
        {
            keypadOpen = true;
            code = "";
            ShowCursor();
        }
    }

    void Say(string text, float seconds = 5) { message = text; messageUntil = Time.time + seconds; }
    static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void OnApplicationFocus(bool focus)
    {
        if (!focus && player != null && !escaped) { paused = true; ShowCursor(); }
    }
    void OnDisable() { ShowCursor(); }

    void SubmitCode()
    {
        if (LegoTutorials.Workshop == null || !LegoTutorials.Workshop.PowerRestored)
        {
            keypadOpen = false;
            Say("The keypad has no power. Solve the LEGO power workshop first.", 8);
            return;
        }
        if (code == "2148")
        {
            unlocked = true;
            keypadOpen = false;
            Say("Door unlocked. Walk through the doorway to escape!");
            ShowCursor();
        }
        else { code = ""; Say("Incorrect code. Try again."); }
    }

    void Restart()
    {
        SceneManager.LoadScene("EscapeRoom");
    }

    void OnGUI()
    {
        if (LegoTutorials.WorkshopActive) return;
        if (!escaped && !paused && !keypadOpen && LegoTutorials.Workshop != null &&
            GUI.Button(new Rect(Screen.width - 220, 45, 205, 32), "LEGO power workshop"))
            LegoTutorials.Workshop.ShowWorkshop(true);
        GUI.Label(new Rect(18, 72, 600, 25), "Objective: restore power, find the exit code, and escape.");
        GUI.Label(new Rect(18, 15, 600, 25), "ESCAPE ROOM   |   WASD: move   Hover nearby objects and click or press E   Esc: pause");
        GUI.Label(new Rect(18, 42, 500, 25), hasKey ? "Inventory: brass key" : "Inventory: empty");
        if (Time.time < messageUntil) GUI.Box(new Rect(20, Screen.height - 85, Screen.width - 40, 55), message);
        if (!keypadOpen && !escaped && !paused)
        {
            if (!string.IsNullOrEmpty(prompt)) GUI.Box(new Rect(Screen.width / 2f - 180, Screen.height / 2f + 35, 360, 30), prompt);
        }
        if (keypadOpen)
        {
            var r = new Rect((Screen.width - 320) / 2f, (Screen.height - 310) / 2f, 320, 310);
            GUILayout.BeginArea(r, "DOOR KEYPAD", GUI.skin.window);
            GUILayout.Space(12);
            GUILayout.Label("Enter the four-digit exit code");
            GUILayout.Label(code.PadRight(4, '_'));
            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    string digit = (row * 3 + col + 1).ToString();
                    if (GUILayout.Button(digit, GUILayout.Height(32)) && code.Length < 4) code += digit;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Height(32))) code = "";
            if (GUILayout.Button("0", GUILayout.Height(32)) && code.Length < 4) code += "0";
            if (GUILayout.Button("Enter", GUILayout.Height(32))) SubmitCode();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Close (Esc)")) { keypadOpen = false; ShowCursor(); }
            GUILayout.EndArea();
            if (Event.current.type == EventType.KeyDown)
            {
                char c = Event.current.character;
                if (c >= '0' && c <= '9' && code.Length < 4) code += c;
                else if (Event.current.keyCode == KeyCode.Backspace && code.Length > 0) code = code.Substring(0, code.Length - 1);
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) SubmitCode();
                Event.current.Use();
            }
        }
        if (paused || escaped)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 320) / 2f, (Screen.height - 180) / 2f, 320, 180), GUI.skin.box);
            GUILayout.Label(escaped ? "YOU ESCAPED!" : "PAUSED");
            if (escaped)
            {
                GUILayout.Label("Room 1 escaped — the Power Room is next.");
                if (GUILayout.Button("Continue to Power Room")) SceneManager.LoadScene("Level2_PowerRoom");
            }
            else if (GUILayout.Button("Resume")) { paused = false; ShowCursor(); }
            if (GUILayout.Button("Restart room")) Restart();
            GUILayout.EndArea();
        }
    }
}
