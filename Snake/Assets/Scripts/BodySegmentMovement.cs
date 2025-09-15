using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;


public class BodySegmentMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public Sprite straightH;  //poziom do wstawienia w inspectorze
    public Sprite straightV; //pion generowany

    public Sprite[] straightHAnim;   
    [HideInInspector] public Sprite[] straightVAnim;   
    public float animFPS = 8f;       
    public bool animateBody = true; 
    private float animTimer;
    private int animFrame;

    public Sprite cornerLU;         
    private Sprite cornerUR, cornerRD, cornerDL;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        //generacja pionu
        if (straightH != null)
            straightV = RotateSprite90(straightH, +1);

        //generowanie animacji pionowej z poziomej
        if ((straightVAnim == null || straightVAnim.Length == 0) &&
            (straightHAnim != null && straightHAnim.Length > 0))
        {
            straightVAnim = new Sprite[straightHAnim.Length];
            for (int i = 0; i < straightHAnim.Length; i++)
            {
                var src = straightHAnim[i];
                straightVAnim[i] = (src != null) ? RotateSprite90(src, +1) : null;
            }
        }

        BuildCornerSetFromLU();
    }


    //wołane co tick z headmovementu
    public void SetDirections(Vector2 inDirection, Vector2 outDirection)
    {
        Vector2 inDir  = ClampCardinal(inDirection);
        Vector2 outDir = ClampCardinal(outDirection);
        if (!spriteRenderer) return;

        bool same     = outDir == inDir;
        bool opposite = outDir == -inDir;

        if (same || opposite)
        {
            Vector2 dir = (outDir != Vector2.zero) ? outDir : inDir;
            bool horizontal = Mathf.Abs(dir.x) != 0;

            //bez rotacji
            spriteRenderer.transform.localRotation = Quaternion.identity;
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;

            if (animateBody && animFPS > 0f)
            {
                AdvanceAnim(Time.deltaTime);
                var frames = horizontal ? straightHAnim : straightVAnim;
                if (frames != null && frames.Length > 0)
                {
                    spriteRenderer.sprite = frames[animFrame % frames.Length];
                    return;
                }
            }

            //powrot do statycznych w razie co
            spriteRenderer.sprite = horizontal ? straightH : straightV;
            return;
        }

        //sztywny zakret
        Vector2 pair = ClampCardinal(inDir + outDir); // (-1,1),(1,1),(1,-1),(-1,-1)
        float angle =
            (pair.x == -1 && pair.y ==  1) ?   0f :
            (pair.x ==  1 && pair.y ==  1) ?  90f :
            (pair.x ==  1 && pair.y == -1) ? 180f :
                                             270f;
        spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, angle);

        float cz = inDir.x * outDir.y - inDir.y * outDir.x;
        bool isCW  = cz < 0f;
        bool isCCW = cz > 0f;

        bool swapAxes = (angle == 90f || angle == 270f);

        spriteRenderer.sprite = cornerLU;
        spriteRenderer.flipX  = false;
        spriteRenderer.flipY  = false;

        if (!swapAxes)
        {
            if (isCW)  spriteRenderer.flipX = true;
            if (isCCW) spriteRenderer.flipY = true;
        }
        else
        {
            if (isCW)  spriteRenderer.flipY = true;
            if (isCCW) spriteRenderer.flipX = true;
        }
    }

    //animacje
    private void AdvanceAnim(float dt)
    {
        animTimer += dt;
        float frameTime = 1f / animFPS;
        while (animTimer >= frameTime)
        {
            animTimer -= frameTime;
            animFrame = (animFrame + 1) & 0x7FFFFFFF; //na wypadeko overflowu
        }
    }

    private static Vector2 ClampCardinal(Vector2 v) => new Vector2(
        Mathf.Clamp(Mathf.RoundToInt(v.x), -1, 1),
        Mathf.Clamp(Mathf.RoundToInt(v.y), -1, 1)
    );

    //generowanie rogow z jednego danego
    private void BuildCornerSetFromLU()
    {
        if (!cornerLU || !cornerLU.texture) return;
        cornerUR = RotateSprite90(cornerLU, +1);
        cornerRD = RotateSprite90(cornerLU, +2);
        cornerDL = RotateSprite90(cornerLU, +3);
    }

    //obrot o 90
    private static Sprite RotateSprite90(Sprite src, int nQuarterTurnsCW)
    {
        nQuarterTurnsCW = ((nQuarterTurnsCW % 4) + 4) % 4;
        if (nQuarterTurnsCW == 0) return src;

        var tex = src.texture;
        Rect r = src.rect;
        int w = (int)r.width, h = (int)r.height;

        var srcPx = tex.GetPixels((int)r.x, (int)r.y, w, h);

        int w2 = (nQuarterTurnsCW % 2 == 0) ? w : h;
        int h2 = (nQuarterTurnsCW % 2 == 0) ? h : w;
        var dstPx = new Color[w2 * h2];

        for (int y = 0; y < h; y++)
        {
            int rowOff = y * w;
            for (int x = 0; x < w; x++)
            {
                int srcIdx = rowOff + x;
                int dx, dy;

                switch (nQuarterTurnsCW)
                {
                    case 1: dx = w - 1 - y; dy = x; break;               //90
                    case 2: dx = w - 1 - x; dy = h - 1 - y; break;       //180
                    default: dx = y; dy = h - 1 - x; break;              //270
                }
                dstPx[dy * w2 + dx] = srcPx[srcIdx];
            }
        }

        var dstTex = new Texture2D(w2, h2, TextureFormat.RGBA32, false);
        dstTex.filterMode = tex.filterMode;
        dstTex.wrapMode   = tex.wrapMode;
        dstTex.SetPixels(dstPx);
        dstTex.Apply(false);

        Vector2 sz = src.rect.size;
        Vector2 np = new Vector2(src.pivot.x / sz.x, src.pivot.y / sz.y);
        Vector2 np2 = nQuarterTurnsCW switch
        {
            1 => new Vector2(1f - np.y, np.x),
            2 => new Vector2(1f - np.x, 1f - np.y),
            _ => new Vector2(np.y, 1f - np.x)
        };

        return Sprite.Create(dstTex, new Rect(0, 0, w2, h2), np2, src.pixelsPerUnit);
    }

}
