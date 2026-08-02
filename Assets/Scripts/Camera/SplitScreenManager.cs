using UnityEngine;

public class SplitScreenManager : MonoBehaviour
{
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [SerializeField] private Camera camera1;
    [SerializeField] private Camera camera2;

    [SerializeField] private float splitDistance = 10f;
    [SerializeField] private float mergeDistance = 8f;

    [SerializeField] private float animationSpeed = 5f;

    private bool split;

    private void Start()
    {
        camera1.rect = new Rect(0,0,1,1);

        camera2.rect = new Rect(1,0,1,1);
    }

    private void Update()
    {
        float distance = Vector2.Distance(
            player1.position,
            player2.position);

        if(!split && distance > splitDistance)
            split = true;

        if(split && distance < mergeDistance)
            split = false;

        AnimateRects();
    }

    void AnimateRects()
    {
        Rect target1;
        Rect target2;

        if(split)
        {
            target1 = new Rect(0,0,0.5f,1);

            target2 = new Rect(0.5f,0,0.5f,1);
        }
        else
        {
            target1 = new Rect(0,0,1,1);

            target2 = new Rect(1,0,1,1);
        }

        camera1.rect = LerpRect(camera1.rect,target1);

        camera2.rect = LerpRect(camera2.rect,target2);
    }

    Rect LerpRect(Rect a, Rect b)
    {
        return new Rect(
            Mathf.Lerp(a.x,b.x,Time.deltaTime*animationSpeed),
            Mathf.Lerp(a.y,b.y,Time.deltaTime*animationSpeed),
            Mathf.Lerp(a.width,b.width,Time.deltaTime*animationSpeed),
            Mathf.Lerp(a.height,b.height,Time.deltaTime*animationSpeed)
        );
    }
}