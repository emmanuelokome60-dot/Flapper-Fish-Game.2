using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _maxTiltUp = 30f;
    [SerializeField] private float _maxTiltDown = -60f;


    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Stop physics from spinning the fish when it bumps into things
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsAlive) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            rb.linearVelocityY = jumpForce;
        }
    }

    private void FixedUpdate()
    {
        // Tilt the fish up when swimming up and down when sinking
        float angle = rb.linearVelocity.y * _rotationSpeed;

        // Keep the tilt between the limits so the fish never flips over
        angle = Mathf.Clamp(angle, _maxTiltDown, _maxTiltUp);

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // Called by the GameManager when the Play button restarts the game
    public void ResetPlayer(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        IsAlive = true;
    }

}
