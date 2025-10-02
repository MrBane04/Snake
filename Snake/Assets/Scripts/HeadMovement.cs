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
    public FoodSpawner foodSpawner;  // spawner owockow

    public SpriteRenderer headRenderer;
    public Sprite[] headFrames;
    public float headFPS = 8f;
        
    public List<Transform> bodySegments = new List<Transform>();
    public TailMovement tailMovement;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private Vector2 pendingDir = Vector2.zero;
    private float timer;
    private Vector2 headNew;
    private int frameIndex = 0;
    private float animationTimer = 0f;
    

    //Do liczenia punktow
    public int score = 0;
    public ScoreUI scoreUI;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!headRenderer) headRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    private bool CheckSelfCollision(Vector2 headNew)
    {
        float tol = 0.1f * stepLength;
        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (Vector2.Distance((Vector2)bodySegments[i].position, headNew) <= tol)
                return true;
        }
        if (tailMovement != null)
        {
            if (Vector2.Distance((Vector2)tailMovement.transform.position, headNew) <= tol)
                return true;
        }

        return false;
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

        headNew = headOld + desiredDelta;

        Vector3 prevPos = headOld;
        Vector2 lastMoveDir = direction;
        
        for (int i = 0; i < bodySegments.Count; i++)
        {
            Transform seg = bodySegments[i];
            Vector3 segOld = seg.position;
            Vector3 segNew = prevPos;
            seg.position = segNew;

            Vector2 inDir = CardinalFromDelta(segNew - segOld);
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
        for(int i =0;i<bodySegments.Count;i++)
        {
            if(i==bodySegments.Count-1 && tailFreezer==true)continue;
            bodySegments[i].GetComponent<SpriteRenderer>().enabled = true;
        } 
        tailFreezer=false;
        if (CheckSelfCollision(headNew))
        {
            Time.timeScale = 0f;
            StartCoroutine(HitEffect());
            return;
        }
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

        Vector2 pos = collision.transform.position;
        float tol = 0.2f * stepLength;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls") && Vector2.Distance(pos, headNew) <= tol)
        {
            Time.timeScale = 0f;
            StartCoroutine(HitEffect());
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            Destroy(collision.gameObject);
            SpawnNewFood();
            AddBodySegment();

            score += 100;
            if (score > 1000000) score = 999999;
            scoreUI.UpdateScore(score);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("SnakeBody"))
        {
            Time.timeScale = 0f;
            StartCoroutine(HitEffect());
        }
    }

    IEnumerator HitEffect()
    {
        bodySegments[0].GetComponent<SpriteRenderer>().enabled = false;
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
        foodSpawner.SpawnFood(); 
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
