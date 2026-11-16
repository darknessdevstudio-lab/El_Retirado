using UnityEngine;

public class HeadBobSystem : MonoBehaviour

{
    [Range(0.001f, 0.5f)]
    public float Amount = 0.1f;

    [Range(1f, 30f)]
    public float Frequency = 10.0f;

    [Range(5f, 20f)]
    public float Smooth = 10.0f;

    Vector3 StartPos;

    void Start()
    {
        StartPos = transform.localPosition;
    }

    void Update()
    {
        CheckForHeadbobTrigger();
        StopHeadbob();
        
    }

    void CheckForHeadbobTrigger()
    {
        float inputMagnitude = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude;

        if (inputMagnitude > 0.1f)
        {
            transform.localPosition = StartHeadbob();
        }
    }

    Vector3 StartHeadbob()
    {
        Vector3 pos = Vector3.zero;

        pos.y += Mathf.Sin(Time.time * Frequency) * Amount * 2f;
        pos.x += Mathf.Cos(Time.time * Frequency / 2f) * Amount * 1.6f;

        return Vector3.Lerp(transform.localPosition, StartPos + pos, Smooth * Time.deltaTime);
    }

    void StopHeadbob()
    {
        if (transform.localPosition == StartPos) return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, StartPos, Smooth * Time.deltaTime);
    }
}

