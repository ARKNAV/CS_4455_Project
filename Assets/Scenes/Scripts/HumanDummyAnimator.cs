using UnityEngine;

public class HumanDummyAnimator : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // TEMP TEST:
        // Hold W to walk
        if (Input.GetKey(KeyCode.W))
            anim.SetFloat("Speed", 1f);
        else
            anim.SetFloat("Speed", 0f);
    }
}
