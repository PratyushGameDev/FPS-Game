using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private float destroyTime;
    //[SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 randomizeOffset;
    [SerializeField] Color color;

    public TextMeshPro textMeshPro;

    // Start is called before the first frame update
    void Start()
    {
        //textMeshPro = GetComponent<TextMeshPro>();
        
        //transform.localPosition += new Vector3(Random.Range(-randomizeOffset.x, randomizeOffset.x), Random.Range(-randomizeOffset.y, randomizeOffset.y), Random.Range(-randomizeOffset.z, randomizeOffset.z));
        //Destroy(gameObject, destroyTime);
    }

    public void Initialize(float damage, Vector3 offset)
    {
        //textMeshPro.text = damage.ToString();
        //transform.localPosition += offset;
    }
}