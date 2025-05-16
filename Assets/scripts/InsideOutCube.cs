using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class InsideOutMaterialSwitcher : MonoBehaviour
{
    [Tooltip("Used when scale.x > 0")]
    public Material outsideMaterial;
    [Tooltip("Used when scale.x < 0")]
    public Material insideMaterial;

    [SerializeField] public bool turnON;
    MeshRenderer _renderer;
    float _lastSign;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        // record initial sign
        _lastSign = Mathf.Sign(transform.localScale.x);
        if (!turnON) return;
        ApplyMaterial(_lastSign);
    }

    void Update()
    {
        if (!turnON) return;
        float currentSign = Mathf.Sign(transform.localScale.x);
        // only swap when we cross zero (1→-1 or -1→1)
        if (currentSign != _lastSign && currentSign != 0)
        {
            ApplyMaterial(currentSign);
            _lastSign = currentSign;
        }
    }

    void ApplyMaterial(float sign)
    {
        _renderer.material = (sign > 0) ? outsideMaterial : insideMaterial;
    }
}
