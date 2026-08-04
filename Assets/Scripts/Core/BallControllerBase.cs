using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallControllerBase : MonoBehaviour
{
    private Rigidbody2D body;

    public Rigidbody2D Body => body;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }
}
