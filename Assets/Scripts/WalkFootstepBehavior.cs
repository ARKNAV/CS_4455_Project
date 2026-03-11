using UnityEngine;

public class WalkFootstepBehavior : StateMachineBehaviour
{
    private float lastFootstepTime = 0f;
    
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        // Trigger footstep at 25% of the animation
        if (normalizedTime > 0.2f && normalizedTime < 0.3f && lastFootstepTime < 0.25f)
        {
            FootstepSound fs = animator.GetComponent<FootstepSound>();
            if (fs != null)
            {
                fs.EmitFootstep();
                lastFootstepTime = 0.25f;
            }
        }
        
        // Trigger footstep at 75% of the animation
        if (normalizedTime > 0.7f && normalizedTime < 0.8f && lastFootstepTime < 0.75f)
        {
            FootstepSound fs = animator.GetComponent<FootstepSound>();
            if (fs != null)
            {
                fs.EmitFootstep();
                lastFootstepTime = 0.75f;
            }
        }
        
        // Reset when animation loops
        if (normalizedTime < 0.1f)
        {
            lastFootstepTime = 0f;
        }
    }
}
