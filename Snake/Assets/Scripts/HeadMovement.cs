using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class HeadMovement : MonoBehaviour
{
    [SerializeField] private float stepLength = 1f;
    [SerializeField] public float refreshTime = 0.3f;
    [SerializeField] private LayerMask groundMask;
    public GameObject foodPrefab;
    public GameObject bodySegmentPrefab;
    private bool tailFreezer=false;
    public GameObject tail;

    public SpriteRenderer headRenderer;
    public Sprite[] headFrames;
    public float headFPS = 8f;

    public List<Transform> bodySegments = new List<Transform>();
    public TailMovement tailMovement;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private Vector2 pendingDir = Vector2.zero;
    private float timer;

    private int frameIndex = 0;
    private float animationTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!headRenderer) headRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        RotateHead();
    }

    private void OnMovement(InputValue value)
    {
    Vector2 v = value.Get<Vector2>();
    int ix = Mathf.RoundToInt(v.x);
    int iy = Mathf.RoundToInt(v.y);

    Vector2 newDir = Vector2.zero;
    if (ix != 0 && iy == 0) newDir = new Vector2(ix, 0);
    else if (iy != 0 && ix == 0) newDir = new Vector2(0, iy);

    if (newDir == Vector2.zero) return;
    if (newDir == -direction)   return;   // blokada 180

    pendingDir = newDir;                  // ostatni input wygrywa
    }


    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer < refreshTime) { AnimateHeadLoop(); return; }
        timer = 0f;

        if (pendingDir != Vector2.zero && pendingDir != -direction)
        {
            direction = pendingDir;
            RotateHead();
        }
        pendingDir = Vector2.zero;

        Vector2 headOld = rb.position;
        Vector2 desiredDelta = direction * stepLength;

        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = groundMask };
        RaycastHit2D[] hits = new RaycastHit2D[4];
        float skin = 0.01f;

        int count = rb.Cast(desiredDelta.normalized, filter, hits, desiredDelta.magnitude);
        if (count > 0)
        {
            float allowed = Mathf.Max(0, hits[0].distance - skin);
            desiredDelta = desiredDelta.normalized * allowed;
        }

        Vector2 headNew = headOld + desiredDelta;

        Vector3 prevPos = headOld;
        Vector2 lastMoveDir = direction;

        for (int i = 0; i < bodySegments.Count; i++)
        {
            Transform seg = bodySegments[i];
            Vector3 segOld = seg.position;
            Vector3 segNew = prevPos;
            seg.position = segNew;

            Vector2 inDir  = CardinalFromDelta(segNew - segOld);
            Vector2 outDir = CardinalFromDelta(((i == 0) ? (Vector3)headNew : bodySegments[i - 1].position) - segNew);

            var anim = seg.GetComponentInChildren<BodySegmentMovement>();
            if (anim) anim.SetDirections(inDir, outDir);

            if (i == bodySegments.Count - 1)
                lastMoveDir = (inDir != Vector2.zero) ? inDir : (outDir != Vector2.zero ? outDir : lastMoveDir);

            prevPos = segOld;
        }

        if (tailMovement) // && tailFreezer==false
        {
            Transform tail = tailMovement.transform;
            tail.position = prevPos;
            Vector2 tailDir = (bodySegments.Count > 0) ? lastMoveDir : direction;
            tailMovement.SetDirection(tailDir);
        }

        if(tailFreezer==false)bodySegments[bodySegments.Count-1].GetComponent<SpriteRenderer>().enabled = true;
        tailFreezer=false;
        rb.MovePosition(headNew);
        AnimateHeadLoop();
    }

    private Vector2 CardinalFromDelta(Vector3 delta)
    {
        int ix = Mathf.Clamp(Mathf.RoundToInt((float)(delta.x / stepLength)), -1, 1);
        int iy = Mathf.Clamp(Mathf.RoundToInt((float)(delta.y / stepLength)), -1, 1);
        return new Vector2(ix, iy);
    }

    private void RotateHead()
    {
        var t = headRenderer ? headRenderer.transform : transform;
        if (direction == Vector2.right)      t.localRotation = Quaternion.Euler(0, 0, 0);
        else if (direction == Vector2.left)  t.localRotation = Quaternion.Euler(0, 0, 180);
        else if (direction == Vector2.up)    t.localRotation = Quaternion.Euler(0, 0, 90);
        else if (direction == Vector2.down)  t.localRotation = Quaternion.Euler(0, 0, -90);
    }

    private void AnimateHeadLoop()
    {
        if (!headRenderer || headFrames == null || headFrames.Length == 0) return;

        animationTimer += Time.deltaTime;
        float frameTime = 1f / headFPS;

        if (animationTimer >= frameTime)
        {
            animationTimer -= frameTime;
            frameIndex = (frameIndex + 1) % headFrames.Length;
            headRenderer.sprite = headFrames[frameIndex];
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls") && (pendingDir == Vector2.zero))
        {
            Time.timeScale = 0f;
            StartCoroutine(HitEffect());
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            Destroy(collision.gameObject);
            SpawnNewFood();
            AddBodySegment();
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("SnakeBody"))
        {
            Time.timeScale = 0f;
            StartCoroutine(HitEffect());
        }
    }

    IEnumerator HitEffect()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            Color orig = sr.color;
            sr.color = Color.red;
            yield return new WaitForSecondsRealtime(0.3f);
            sr.color = orig;
        }
    }

    private void SpawnNewFood()
    {
        float x = Mathf.Round(Random.Range(-5f, 5f));
        float y = Mathf.Round(Random.Range(-5f, 5f));
        Instantiate(foodPrefab, new Vector3(0.5f+x, 0.5f+y, 0f), Quaternion.identity);
    }

    private void AddBodySegment()
    {
        tailFreezer = true;
        Vector3 newPos = bodySegments.Count > 0
            ? bodySegments[bodySegments.Count - 1].position
            : rb.position;

        GameObject seg = Instantiate(bodySegmentPrefab, newPos, Quaternion.identity);
        Debug.Log(bodySegments[bodySegments.Count-1].GetComponent<SpriteRenderer>().sprite.name);
        bodySegments.Add(seg.transform);
        seg.GetComponent<SpriteRenderer>().enabled = false;
    }
}
