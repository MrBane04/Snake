using UnityEngine;

public class TailMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; 
    public Sprite[] frames;               
    public float fps = 8f;               
    public float baseAngleOffset = 0f; //na wszelki offset

    private int frameIndex;
    private float timer;
    private Vector2 lastDir = Vector2.right;

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        frameIndex = 0;
        timer = 0f;
        ApplyFrame();
        ApplyRotation(lastDir);
    }

    private void Update()
    {
        if (!spriteRenderer || frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, fps);
        if (timer >= frameTime)
        {
            timer -= frameTime;
            frameIndex = (frameIndex + 1) % frames.Length;
            ApplyFrame();
        }
    }

    public void SetDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) dir = lastDir; 
        lastDir = dir;
        ApplyRotation(dir);
    }

    private void ApplyFrame()
    {
        spriteRenderer.sprite = frames[frameIndex];
    }

    private void ApplyRotation(Vector2 dir)
    {
        float z = (dir == Vector2.right) ? 0f :
                  (dir == Vector2.up)    ? 90f :
                  (dir == Vector2.left)  ? 180f : -90f;

        var t = spriteRenderer ? spriteRenderer.transform : transform;
        t.rotation = Quaternion.Euler(0, 0, z + baseAngleOffset);
    }
}
