using UnityEngine;

public class MousePainter : MonoBehaviour
{
    public Camera cam;
    [Space] public bool mouseSingleClick;
    [Space] public Color paintColor;

    public float radius = 1;
    public float strength = 1;
    public float hardness = 1;

    void Update ()
    {
        bool click;
        click = mouseSingleClick ? Input.GetMouseButtonDown (0) : Input.GetMouseButton (0);

        if (click)
        {
            Vector3 position = Input.mousePosition;
            Ray ray = cam.ScreenPointToRay (position);
            RaycastHit hit;

            if (Physics.Raycast (ray, out hit, 100.0f))
            {
                Debug.DrawRay (ray.origin, hit.point - ray.origin, Color.red);
                transform.position = hit.point;
                Paintable p = hit.collider.GetComponent<Paintable> ();

                if (p.TryGetPointOnMesh (ray, hit.point, out var pointOnMesh))

                    if (p != null)
                    {
                        PaintManager.instance.paint (p, pointOnMesh, -ray.direction, radius, hardness, strength, paintColor);
                    }
            }
        }
    }
}