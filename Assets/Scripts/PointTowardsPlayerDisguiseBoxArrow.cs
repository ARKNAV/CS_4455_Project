using System;
using UnityEngine;

public class PointTowardsPlayerDisguiseBoxArrow : MonoBehaviour
{
    DisguiseBox disguiseBox;
    public GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        disguiseBox = GetComponentInParent<DisguiseBox>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(player.transform);
        if (disguiseBox.isUsed == true)
        {
            gameObject.SetActive(false);
        }
    }
}
